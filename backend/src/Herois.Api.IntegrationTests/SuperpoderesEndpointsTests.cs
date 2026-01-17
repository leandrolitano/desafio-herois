using Herois.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Herois.Api.IntegrationTests;

public class SuperpoderesEndpointsTests
{
    private static HttpClient CreateClient(CustomWebApplicationFactory factory)
        => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<ApiEnvelope<T>> ReadEnvelope<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ApiEnvelope<T>>(json, opts)
               ?? throw new InvalidOperationException("Resposta inválida: " + json);
    }

    [Fact]
    public async Task GetSuperpoderes_ReturnsSeededList()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateClient(factory);

        var res = await client.GetAsync("/api/superpoderes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var env = await ReadEnvelope<List<SuperpoderDto>>(res);
        Assert.True(env.Success);
        Assert.NotNull(env.Data);
        Assert.True(env.Data!.Count >= 4);

        Assert.All(env.Data!, p => Assert.False(string.IsNullOrWhiteSpace(p.Nome)));
    }

    private record ApiEnvelope<T>(bool Success, int StatusCode, string Message, T? Data);
}
