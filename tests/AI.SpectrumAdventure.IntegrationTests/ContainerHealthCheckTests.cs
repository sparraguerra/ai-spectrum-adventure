namespace AI.SpectrumAdventure.IntegrationTests;

using System.Net;
using AI.SpectrumAdventure.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ContainerHealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ContainerHealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}