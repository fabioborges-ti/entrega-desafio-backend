using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

public class PaginatedListTests
{
    [Fact(DisplayName = "Construtor preenche metadados e itens")]
    public void Constructor_PopulatesMetadataAndItems()
    {
        var items = new List<int> { 1, 2, 3 };

        var page = new PaginatedList<int>(items, count: 25, pageNumber: 2, pageSize: 10);

        page.Should().HaveCount(3);
        page.CurrentPage.Should().Be(2);
        page.PageSize.Should().Be(10);
        page.TotalCount.Should().Be(25);
        page.TotalPages.Should().Be(3);
        page.HasPrevious.Should().BeTrue();
        page.HasNext.Should().BeTrue();
    }

    [Theory(DisplayName = "HasPrevious / HasNext refletem a posição atual")]
    [InlineData(1, false, true)]
    [InlineData(2, true, true)]
    [InlineData(3, true, false)]
    public void HasPreviousNext_ReflectsPosition(int page, bool hasPrev, bool hasNext)
    {
        var p = new PaginatedList<int>(new List<int>(), count: 25, pageNumber: page, pageSize: 10);
        p.HasPrevious.Should().Be(hasPrev);
        p.HasNext.Should().Be(hasNext);
    }

    [Fact(DisplayName = "TotalPages é teto da divisão count/pageSize")]
    public void TotalPages_IsCeilingOfCountByPageSize()
    {
        new PaginatedList<int>(new List<int>(), 0, 1, 10).TotalPages.Should().Be(0);
        new PaginatedList<int>(new List<int>(), 9, 1, 10).TotalPages.Should().Be(1);
        new PaginatedList<int>(new List<int>(), 11, 1, 10).TotalPages.Should().Be(2);
        new PaginatedList<int>(new List<int>(), 20, 1, 10).TotalPages.Should().Be(2);
    }

    [Fact(DisplayName = "Construtor expõe Items da página atual via base List<T>")]
    public void Constructor_ExposesItemsViaBaseList()
    {
        var page = new PaginatedList<string>(new List<string> { "a", "b" }, count: 5, pageNumber: 1, pageSize: 2);
        page.Should().Equal("a", "b");
    }

    [Fact(DisplayName = "CreateAsync pagina IQueryable assíncrono (EF InMemory) e retorna a página correta")]
    public async Task CreateAsync_PaginatesAsyncQueryable()
    {
        await using var ctx = new TestDbContext(
            new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        for (var i = 1; i <= 25; i++)
            ctx.Items.Add(new TestItem { Id = i, Value = $"v{i}" });
        await ctx.SaveChangesAsync();

        var query = ctx.Items.OrderBy(i => i.Id);
        var page = await PaginatedList<TestItem>.CreateAsync(query, pageNumber: 2, pageSize: 10);

        page.CurrentPage.Should().Be(2);
        page.PageSize.Should().Be(10);
        page.TotalCount.Should().Be(25);
        page.TotalPages.Should().Be(3);
        page.Should().HaveCount(10);
        page.First().Id.Should().Be(11);
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestItem> Items => Set<TestItem>();
    }

    private sealed class TestItem
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}

