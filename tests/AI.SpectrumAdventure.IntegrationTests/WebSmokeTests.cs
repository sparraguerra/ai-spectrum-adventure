namespace AI.SpectrumAdventure.IntegrationTests;

using System.Net;
using AI.SpectrumAdventure.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public class WebSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_RendersAdventureUI()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
        });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Adventure");
        html.Should().Contain("Choose your adventure");
        html.Should().Contain("The Forgotten Tower");
        html.Should().Contain("Start game");
    }
}
