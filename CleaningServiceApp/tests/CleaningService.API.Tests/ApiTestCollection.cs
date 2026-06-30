namespace CleaningService.API.Tests;

[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<PostgreSqlApiFixture>
{
    public const string Name = "PostgreSQL API";
}
