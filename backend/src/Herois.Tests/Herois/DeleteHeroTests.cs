using Herois.Application.Herois;
using Herois.Application.Herois.Handlers;
using Herois.Tests.Common;

namespace Herois.Tests.Herois;

public class DeleteHeroTests
{
    [Fact]
    public async Task Should_Return_400_When_Id_Is_Invalid()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new DeleteHeroHandler(tdb.Db);

        var res = await handler.Handle(new DeleteHeroCommand(0), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task Should_Return_404_When_Hero_Does_Not_Exist()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new DeleteHeroHandler(tdb.Db);

        var res = await handler.Handle(new DeleteHeroCommand(999), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task Should_Delete_Hero_Successfully()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
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
        var id = created.Data!.Id;

        var delete = new DeleteHeroHandler(tdb.Db);
        var deleted = await delete.Handle(new DeleteHeroCommand(id), CancellationToken.None);

        Assert.True(deleted.Success);
        Assert.Equal(200, deleted.StatusCode);
        Assert.Equal("Herói removido com sucesso.", deleted.Message);

        // garante que não existe mais
        var get = new GetHeroByIdHandler(tdb.Db);
        var after = await get.Handle(new GetHeroByIdQuery(id), CancellationToken.None);
        Assert.False(after.Success);
        Assert.Equal(404, after.StatusCode);
    }
}
