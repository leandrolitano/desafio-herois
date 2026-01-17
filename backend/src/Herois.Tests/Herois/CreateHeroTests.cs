using Herois.Application.Herois;
using Herois.Application.Herois.Handlers;
using Herois.Tests.Common;

namespace Herois.Tests.Herois;

public class CreateHeroTests
{
    [Fact]
    public async Task Should_Create_Hero_With_Superpowers()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new CreateHeroHandler(tdb.Db);

        var cmd = new CreateHeroCommand(
            Nome: "Bruce Wayne",
            NomeHeroi: "Batman",
            DataNascimento: new DateTime(1972, 2, 19),
            Altura: 1.88,
            Peso: 95,
            SuperpoderIds: new List<int> { 1, 2 }
        );

        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(res.Success);
        Assert.Equal(201, res.StatusCode);
        Assert.NotNull(res.Data);
        Assert.Equal("Batman", res.Data!.NomeHeroi);
        Assert.Equal(2, res.Data!.Superpoderes.Count);
    }

    [Fact]
    public async Task Should_Block_Duplicate_NomeHeroi()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new CreateHeroHandler(tdb.Db);

        // Primeiro herói
        var first = await handler.Handle(new CreateHeroCommand(
            "Clark Kent",
            "Superman",
            new DateTime(1978, 6, 18),
            1.91,
            105,
            new List<int> { 3 }
        ), CancellationToken.None);

        Assert.True(first.Success);

        // Segundo com mesmo NomeHeroi
        var duplicate = await handler.Handle(new CreateHeroCommand(
            "Outro",
            "Superman",
            new DateTime(1980, 1, 1),
            1.70,
            70,
            new List<int> { 1 }
        ), CancellationToken.None);

        Assert.False(duplicate.Success);
        Assert.Equal(409, duplicate.StatusCode);
    }

    [Fact]
    public async Task Should_Reject_Empty_NomeHeroi()
    {
        await using var tdb = await SqliteTestDb.CreateAsync();
        var handler = new CreateHeroHandler(tdb.Db);

        var res = await handler.Handle(new CreateHeroCommand(
            "Sem HeroName",
            "   ",
            new DateTime(1990, 1, 1),
            1.75,
            80,
            new List<int> { 1 }
        ), CancellationToken.None);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }
}
