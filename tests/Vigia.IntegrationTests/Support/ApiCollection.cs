namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Shares one API instance and one Postgres container across API tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<VigiaApiFactory>
{
    /// <summary>Collection name.</summary>
    public const string Name = "api";
}
