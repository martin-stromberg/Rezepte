using Xunit;

namespace Rezepte.Tests.TestHelpers;

[CollectionDefinition(Name)]
public class GoogleCredentialsEnvironmentCollection : ICollectionFixture<object>
{
    public const string Name = "GoogleCredentialsEnvironment";
}
