using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresIntegrationTestFixture>
{
    public const string Name = "Postgres integration tests";
}
