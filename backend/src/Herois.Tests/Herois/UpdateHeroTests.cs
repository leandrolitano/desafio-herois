using Herois.Application.Herois;
using Herois.Application.Herois.Handlers;
using Herois.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace Herois.Tests.Herois;

public class UpdateHeroTests
{
    [Fact]
    public async Task Should_Return_400_When_Id_Is_Invalid()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new UpdateHeroHandler(tdb.Db);

        var res = await handler.Handle(new UpdateHeroCommand(
            0,
            "Nome",
            "Hero",
            new DateTime(1990, 1, 1),
            1.7,
            70,
            new List<int> { 1 },
            new byte[] { 1 }
        ), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task Should_Return_404_When_Hero_Does_Not_Exist()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new UpdateHeroHandler(tdb.Db);

        var res = await handler.Handle(new UpdateHeroCommand(
            999,
            "Nome",
            "Hero",
            new DateTime(1990, 1, 1),
            1.7,
            70,
            new List<int> { 1 },
            new byte[] { 1 }
        ), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task Should_Block_Duplicate_NomeHeroi_On_Update()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var create = new CreateHeroHandler(tdb.Db);

        var batman = await create.Handle(new CreateHeroCommand(
            "Bruce Wayne",
            "Batman",
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 1 }
        ), CancellationToken.None);

        var superman = await create.Handle(new CreateHeroCommand(
            "Clark Kent",
            "Superman",
            new DateTime(1978, 6, 18),
            1.91,
            105,
            new List<int> { 2 }
        ), CancellationToken.None);

        Assert.True(batman.Success);
        Assert.True(superman.Success);

        // rowversion atual do Batman
        var batmanRow = await tdb.Db.Herois.AsNoTracking().Where(h => h.Id == batman.Data!.Id).Select(h => h.RowVersion).FirstAsync();

        var update = new UpdateHeroHandler(tdb.Db);
        var res = await update.Handle(new UpdateHeroCommand(
            batman.Data!.Id,
            "Bruce Wayne",
            "Superman", // duplicado
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 1 },
            batmanRow
        ), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(409, res.StatusCode);
    }

    [Fact]
    public async Task Should_Update_Superpowers_Relationships()
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

        var row = await tdb.Db.Herois.AsNoTracking().Where(h => h.Id == id).Select(h => h.RowVersion).FirstAsync();

        var update = new UpdateHeroHandler(tdb.Db);
        var updated = await update.Handle(new UpdateHeroCommand(
            id,
            "Bruce Wayne",
            "Batman",
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 2, 3 },
            row
        ), CancellationToken.None);

        Assert.True(updated.Success);
        Assert.Equal(200, updated.StatusCode);
        Assert.NotNull(updated.Data);

        var powers = updated.Data!.Superpoderes.Select(p => p.Id).OrderBy(x => x).ToList();
        Assert.Equal(new List<int> { 2, 3 }, powers);
    }

    [Fact]
    public async Task Should_Return_409_When_RowVersion_Is_Stale()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var create = new CreateHeroHandler(tdb.Db);

        var created = await create.Handle(new CreateHeroCommand(
            "Bruce Wayne",
            "Batman",
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 1 }
        ), CancellationToken.None);

        Assert.True(created.Success);
        var id = created.Data!.Id;

        // rowversion que o "cliente" leu
        var oldRow = await tdb.Db.Herois.AsNoTracking().Where(h => h.Id == id).Select(h => h.RowVersion).FirstAsync();

        // simula outro processo alterando o registro (mudando o RowVersion diretamente no banco)
        await tdb.Db.Database.ExecuteSqlRawAsync("UPDATE Herois SET RowVersion = X'0102030405060708' WHERE Id = {0}", id);

        var update = new UpdateHeroHandler(tdb.Db);
        var res = await update.Handle(new UpdateHeroCommand(
            id,
            "Bruce Wayne",
            "Batman",
            new DateTime(1972, 2, 19),
            1.88,
            95,
            new List<int> { 1 },
            oldRow
        ), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(409, res.StatusCode);
    }
}
