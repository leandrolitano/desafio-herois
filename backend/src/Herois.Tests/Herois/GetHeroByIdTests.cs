using Herois.Application.Herois;
using Herois.Application.Herois.Handlers;
using Herois.Tests.Common;

namespace Herois.Tests.Herois;

public class GetHeroByIdTests
{
    [Fact]
    public async Task Should_Return_400_When_Id_Is_Invalid()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new GetHeroByIdHandler(tdb.Db);

        var res = await handler.Handle(new GetHeroByIdQuery(0), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task Should_Return_404_When_Hero_Does_Not_Exist()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new GetHeroByIdHandler(tdb.Db);

        var res = await handler.Handle(new GetHeroByIdQuery(999), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Hero_With_Superpowers()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var create = new CreateHeroHandler(tdb.Db);

        var created = await create.Handle(new CreateHeroCommand(
            "Bruce Wayne",
            "Batman",
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 1, 3 }
        ), CancellationToken.None);

        Assert.True(created.Success);
        var id = created.Data!.Id;

        var handler = new GetHeroByIdHandler(tdb.Db);
        var res = await handler.Handle(new GetHeroByIdQuery(id), CancellationToken.None);

        Assert.True(res.Success);
        Assert.Equal(200, res.StatusCode);
        Assert.NotNull(res.Data);
        Assert.Equal("Batman", res.Data!.NomeHeroi);
        Assert.Equal(2, res.Data!.Superpoderes.Count);
    }
}
