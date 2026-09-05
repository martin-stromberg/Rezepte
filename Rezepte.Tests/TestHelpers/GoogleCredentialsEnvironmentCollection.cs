using Xunit;

namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Class representing the google credentials environment collection.
/// </summary>
[CollectionDefinition(Name)]
public class GoogleCredentialsEnvironmentCollection : ICollectionFixture<object>
{
    /// <summary>
    /// The name value.
    /// </summary>
    public const string Name = "GoogleCredentialsEnvironment";
}
