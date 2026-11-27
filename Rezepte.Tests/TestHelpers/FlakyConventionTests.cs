using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Meta test enforcing the convention that the <c>Flaky</c> category is only ever
/// combined with <c>OsInterface</c> — a violation would push a retry-eligible test
/// into the blocking lane. The class is compiled into both test assemblies via a
/// <c>&lt;Compile Include&gt;</c> link and scans whichever assembly it runs in.
/// Trait values are read through <see cref="CustomAttributeData"/> because the xUnit
/// <c>TraitAttribute</c> is sealed and exposes no public trait-reader API.
/// </summary>
public sealed class FlakyConventionTests
{
    /// <summary>
    /// Every method or class carrying a <c>Category=Flaky</c> trait must also carry
    /// <c>Category=OsInterface</c> (class-level traits count for their methods).
    /// </summary>
    [Fact]
    public void FlakyMarkedTests_AlwaysCarryOsInterface()
    {
        var violations = new List<string>();
        var assembly = typeof(FlakyConventionTests).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            var typeCategories = Categories(type.CustomAttributes).ToArray();
            var typeIsFlaky = typeCategories.Contains(TestCategories.Flaky);
            var typeIsOsInterface = typeCategories.Contains(TestCategories.OsInterface);

            if (typeIsFlaky && !typeIsOsInterface)
            {
                violations.Add(type.FullName ?? type.Name);
            }

            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var methodCategories = Categories(method.CustomAttributes).ToArray();
                var isFlaky = typeIsFlaky || methodCategories.Contains(TestCategories.Flaky);
                var isOsInterface = typeIsOsInterface || methodCategories.Contains(TestCategories.OsInterface);

                if (isFlaky && !isOsInterface)
                {
                    violations.Add($"{type.FullName}.{method.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            "the Flaky category is only valid in combination with OsInterface");
    }

    private static IEnumerable<string> Categories(IEnumerable<CustomAttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeType.FullName != "Xunit.TraitAttribute"
                || attribute.ConstructorArguments.Count != 2)
            {
                continue;
            }

            var name = attribute.ConstructorArguments[0].Value as string;
            var value = attribute.ConstructorArguments[1].Value as string;
            if (name == TestCategories.Category && value is not null)
            {
                yield return value;
            }
        }
    }
}
