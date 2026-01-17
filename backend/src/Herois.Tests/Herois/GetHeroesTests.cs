using Herois.Application.Herois;
using Herois.Application.Herois.Handlers;
using Herois.Tests.Common;

namespace Herois.Tests.Herois;

public class GetHeroesTests
{
    [Fact]
    public async Task Should_Return_404_When_List_Is_Empty()
    {
        await using var tdb = await SqliteTestDb.CreateAsync(seedDefaultSuperpowers: false);
        var handler = new GetHeroesHandler(tdb.Db);

        var res = await handler.Handle(new GetHeroesQuery(), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
        Assert.Contains("Nenhum heroi", res.Message);
    }

    [Fact]
    public async Task Should_Return_List_When_There_Are_Heroes()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();

        // cria 1 herói
        var create = new CreateHeroHandler(tdb.Db);
        var created = await create.Handle(new CreateHeroCommand(
            "Bruce Wayne",
            "Batman",
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 1, 2 }
        ), CancellationToken.None);

        Assert.True(created.Success);

        var handler = new GetHeroesHandler(tdb.Db);
        var res = await handler.Handle(new GetHeroesQuery(), CancellationToken.None);

        Assert.True(res.Success);
        Assert.Equal(200, res.StatusCode);
        Assert.NotNull(res.Data);
        Assert.NotNull(res.Data!.Items);
        Assert.Single(res.Data!.Items);
        Assert.Equal("Batman", res.Data!.Items[0].NomeHeroi);
        Assert.Equal(1, res.Data!.Total);
    }

    [Fact]
    public async Task Should_Apply_Pagination_And_Search()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var create = new CreateHeroHandler(tdb.Db);

        // 3 herois
        await create.Handle(new CreateHeroCommand("A", "Alpha", new DateTime(1990, 1, 1), 1.7, 70, new List<int> { 1 }), CancellationToken.None);
        await create.Handle(new CreateHeroCommand("B", "Beta", new DateTime(1990, 1, 1), 1.7, 70, new List<int> { 1 }), CancellationToken.None);
        await create.Handle(new CreateHeroCommand("C", "Gamma", new DateTime(1990, 1, 1), 1.7, 70, new List<int> { 1 }), CancellationToken.None);

        var handler = new GetHeroesHandler(tdb.Db);

        // pageSize=2
        var page1 = await handler.Handle(new GetHeroesQuery(Page: 1, PageSize: 2), CancellationToken.None);
        Assert.True(page1.Success);
        Assert.Equal(2, page1.Data!.Items.Count);
        Assert.Equal(3, page1.Data!.Total);

        var page2 = await handler.Handle(new GetHeroesQuery(Page: 2, PageSize: 2), CancellationToken.None);
        Assert.True(page2.Success);
        Assert.Single(page2.Data!.Items);

        // search
        var search = await handler.Handle(new GetHeroesQuery(Page: 1, PageSize: 20, Search: "Beta"), CancellationToken.None);
        Assert.True(search.Success);
        Assert.Single(search.Data!.Items);
        Assert.Equal("Beta", search.Data!.Items[0].NomeHeroi);
    }
}
