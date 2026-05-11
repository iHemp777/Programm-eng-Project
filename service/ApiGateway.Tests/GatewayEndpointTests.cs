using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiGateway.Tests;

public class GatewayEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk_WithGatewayPayload()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthPayload>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(body);
        Assert.Equal("ok", body!.Status);
        Assert.Equal("api-gateway", body.Service);
    }

    [Fact]
    public async Task Swagger_ReturnsHtml_WithLinksPage()
    {
        var response = await _client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger Endpoints", html, StringComparison.Ordinal);
        Assert.Contains("Survey Service Swagger", html, StringComparison.Ordinal);
        Assert.Contains("Voting Service Swagger", html, StringComparison.Ordinal);
    }

    private sealed record HealthPayload(string Status, string Service);
}
