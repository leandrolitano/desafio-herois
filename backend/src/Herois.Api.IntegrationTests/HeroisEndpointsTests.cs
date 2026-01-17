using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Herois.Api.Contracts;
using Herois.Application.Common;
using Herois.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Herois.Api.IntegrationTests;

public class HeroisEndpointsTests
{
    private static HttpClient CreateClient(CustomWebApplicationFactory factory)
        => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static async Task<T> ReadJson<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)
               ?? throw new InvalidOperationException("Resposta invalida: " + json);
    }

    private static async Task<ProblemDetails> ReadProblem(HttpResponseMessage res)
        => await ReadJson<ProblemDetails>(res);

    [Fact]
    public async Task GetHeroes_WhenEmpty_Returns404ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var res = await client.GetAsync("/api/herois");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var problem = await ReadProblem(res);
        Assert.Equal(404, problem.Status);
        Assert.Contains("Nenhum heroi", problem.Detail);
    }

    [Fact]
    public async Task Crud_Flow_Create_Update_Delete_Works()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        // powers
        var powersRes = await client.GetAsync("/api/superpoderes");
        Assert.Equal(HttpStatusCode.OK, powersRes.StatusCode);

        var powersEnv = await ReadJson<Result<List<SuperpoderDto>>>(powersRes);
        Assert.True(powersEnv.Success);
        var powerIds = powersEnv.Data!.Take(2).Select(p => p.Id).ToArray();

        // CREATE
        var createPayload = new CreateHeroRequest(
            Nome: "Bruce Wayne",
            NomeHeroi: "Batman",
            DataNascimento: new DateTime(1975, 1, 1),
            Altura: 1.88,
            Peso: 92,
            SuperpoderIds: powerIds.ToList()
        );

        var createRes = await client.PostAsJsonAsync("/api/herois", createPayload);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = await ReadJson<Result<HeroiDto>>(createRes);
        Assert.True(created.Success);
        Assert.Equal(201, created.StatusCode);
        Assert.NotNull(created.Data);
        Assert.True(created.Data!.Id > 0);
        var id = created.Data!.Id;
        var rowVersion = created.Data!.RowVersion;

        // GET BY ID
        var getRes = await client.GetAsync($"/api/herois/{id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        var got = await ReadJson<Result<HeroiDto>>(getRes);
        Assert.True(got.Success);
        Assert.Equal("Batman", got.Data!.NomeHeroi);

        // UPDATE (troca alguns campos + poderes)
        var updatePayload = new UpdateHeroRequest(
            Nome: "Bruce Wayne",
            NomeHeroi: "Batman",
            DataNascimento: new DateTime(1975, 1, 1),
            Altura: 1.90,
            Peso: 95,
            SuperpoderIds: new List<int> { powerIds[0] },
            RowVersion: rowVersion
        );

        var updateRes = await client.PutAsJsonAsync($"/api/herois/{id}", updatePayload);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        var updated = await ReadJson<Result<HeroiDto>>(updateRes);
        Assert.True(updated.Success);
        Assert.Equal(1.90, updated.Data!.Altura);
        Assert.Single(updated.Data!.Superpoderes);

        // LIST (paged)
        var listRes = await client.GetAsync("/api/herois?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        var list = await ReadJson<Result<PagedResult<HeroiDto>>>(listRes);
        Assert.True(list.Success);
        Assert.NotNull(list.Data);
        Assert.Single(list.Data!.Items);

        // DELETE
        var delRes = await client.DeleteAsync($"/api/herois/{id}");
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);

        var del = await ReadJson<Result<string>>(delRes);
        Assert.True(del.Success);

        // LIST EMPTY AGAIN -> 404 (comportamento atual)
        var listEmptyRes = await client.GetAsync("/api/herois");
        Assert.Equal(HttpStatusCode.NotFound, listEmptyRes.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateNomeHeroi_Returns409ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var powersRes = await client.GetAsync("/api/superpoderes");
        var powersEnv = await ReadJson<Result<List<SuperpoderDto>>>(powersRes);
        var powerIds = powersEnv.Data!.Take(1).Select(p => p.Id).ToList();

        var payload = new CreateHeroRequest(
            Nome: "A",
            NomeHeroi: "Duplicado",
            DataNascimento: new DateTime(2000, 1, 1),
            Altura: 1.7,
            Peso: 70,
            SuperpoderIds: powerIds
        );

        var first = await client.PostAsJsonAsync("/api/herois", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/herois", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var problem = await ReadProblem(second);
        Assert.Equal(409, problem.Status);
        Assert.Contains("Ja existe", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithoutNomeHeroi_Returns400ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var powersRes = await client.GetAsync("/api/superpoderes");
        var powersEnv = await ReadJson<Result<List<SuperpoderDto>>>(powersRes);
        var powerIds = powersEnv.Data!.Take(1).Select(p => p.Id).ToList();

        var payload = new CreateHeroRequest(
            Nome: "Clark Kent",
            NomeHeroi: " ",
            DataNascimento: new DateTime(1980, 1, 1),
            Altura: 1.9,
            Peso: 85,
            SuperpoderIds: powerIds
        );

        var res = await client.PostAsJsonAsync("/api/herois", payload);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var problem = await ReadProblem(res);
        Assert.Equal(400, problem.Status);
        Assert.Contains("NomeHeroi", problem.Detail);
    }

    [Fact]
    public async Task GetById_WithInvalidId_Returns400ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var res = await client.GetAsync("/api/herois/0");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var problem = await ReadProblem(res);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Id", problem.Detail);
    }

    [Fact]
    public async Task Update_WithInvalidId_Returns400ValidationProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var powersRes = await client.GetAsync("/api/superpoderes");
        var powersEnv = await ReadJson<Result<List<SuperpoderDto>>>(powersRes);
        var powerIds = powersEnv.Data!.Take(1).Select(p => p.Id).ToList();

        var payload = new UpdateHeroRequest(
            Nome: "Teste",
            NomeHeroi: "Teste",
            DataNascimento: new DateTime(2000, 1, 1),
            Altura: 1.7,
            Peso: 70,
            SuperpoderIds: powerIds,
            RowVersion: "AQID" // base64 qualquer (nao usado porque id e invalido)
        );

        var res = await client.PutAsJsonAsync("/api/herois/0", payload);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var problem = await ReadJson<ValidationProblemDetails>(res);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Falha de validacao", problem.Title);
        Assert.True(problem.Errors.ContainsKey("Id"));
    }

    [Fact]
    public async Task Delete_WithInvalidId_Returns400ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var res = await client.DeleteAsync("/api/herois/0");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var problem = await ReadProblem(res);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var res = await client.DeleteAsync("/api/herois/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var problem = await ReadProblem(res);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Update_NotFound_Returns404ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var powersRes = await client.GetAsync("/api/superpoderes");
        var powersEnv = await ReadJson<Result<List<SuperpoderDto>>>(powersRes);
        var powerIds = powersEnv.Data!.Take(1).Select(p => p.Id).ToList();

        var payload = new UpdateHeroRequest(
            Nome: "Teste",
            NomeHeroi: "Teste",
            DataNascimento: new DateTime(2000, 1, 1),
            Altura: 1.7,
            Peso: 70,
            SuperpoderIds: powerIds,
            RowVersion: "AQID"
        );

        var res = await client.PutAsJsonAsync("/api/herois/999999", payload);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var problem = await ReadProblem(res);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Update_ToDuplicateNomeHeroi_Returns409ProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var powersRes = await client.GetAsync("/api/superpoderes");
        var powersEnv = await ReadJson<Result<List<SuperpoderDto>>>(powersRes);
        var powerIds = powersEnv.Data!.Take(1).Select(p => p.Id).ToList();

        // cria 2 herois
        var create1 = new CreateHeroRequest("A", "HeroA", new DateTime(2000, 1, 1), 1.7, 70, powerIds);
        var create2 = new CreateHeroRequest("B", "HeroB", new DateTime(2000, 1, 1), 1.7, 70, powerIds);

        var res1 = await client.PostAsJsonAsync("/api/herois", create1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        var env1 = await ReadJson<Result<HeroiDto>>(res1);

        var res2 = await client.PostAsJsonAsync("/api/herois", create2);
        Assert.Equal(HttpStatusCode.Created, res2.StatusCode);
        var env2 = await ReadJson<Result<HeroiDto>>(res2);

        // tenta atualizar o 2 para NomeHeroi do 1
        var update2 = new UpdateHeroRequest(
            Nome: "B",
            NomeHeroi: "HeroA",
            DataNascimento: new DateTime(2000, 1, 1),
            Altura: 1.7,
            Peso: 70,
            SuperpoderIds: powerIds,
            RowVersion: env2.Data!.RowVersion
        );

        var updRes = await client.PutAsJsonAsync($"/api/herois/{env2.Data!.Id}", update2);
        Assert.Equal(HttpStatusCode.Conflict, updRes.StatusCode);

        var problem = await ReadProblem(updRes);
        Assert.Equal(409, problem.Status);
    }
}
