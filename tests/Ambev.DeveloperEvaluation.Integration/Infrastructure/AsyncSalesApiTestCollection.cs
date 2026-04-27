using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class AsyncSalesApiTestCollection : ICollectionFixture<AsyncSalesApiTestFixture>
{
    public const string Name = "Async sales API integration tests";
}
