namespace AI.SpectrumAdventure.IntegrationTests;

using System.Net;
using AI.SpectrumAdventure.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class LocalOrchestrationFeatureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocalOrchestrationFeatureTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AppHost_DeclaresLocalTopologyAndReadinessDependencies()
    {
        var appHostSource = ReadWorkspaceFile("src", "AI.SpectrumAdventure.AppHost", "Program.cs");

        appHostSource.Should().Contain("builder.AddPostgres(\"postgres\")");
        appHostSource.Should().Contain(".WithImage(\"postgres\", \"16\")");
        appHostSource.Should().Contain("postgres.AddDatabase(\"AdventureDb\", \"adventure\")");
        appHostSource.Should().Contain("builder.AddAzureStorage(\"storage\")");
        appHostSource.Should().Contain(".RunAsEmulator(emulator => emulator.WithDataVolume(\"spectrum-adventure-azurite\"))");
        appHostSource.Should().Contain("storage.AddBlobs(\"VisualAssets\")");
        appHostSource.Should().Contain("builder.AddProject<Projects.AI_SpectrumAdventure_Web>(\"web\")");
        appHostSource.Should().Contain(".WithReference(adventureDb)");
        appHostSource.Should().Contain(".WithReference(visualAssets, \"AzureStorage\")");
        appHostSource.Should().Contain(".WaitFor(adventureDb)");
        appHostSource.Should().Contain(".WaitFor(visualAssets)");
    }

    [Fact]
    public void AppHostLaunchProfile_ConfiguresDashboardAndLocalTransport()
    {
        var launchSettings = ReadWorkspaceFile("src", "AI.SpectrumAdventure.AppHost", "Properties", "launchSettings.json");

        launchSettings.Should().Contain("\"applicationUrl\": \"http://localhost:15888\"");
        launchSettings.Should().Contain("\"ASPIRE_ALLOW_UNSECURED_TRANSPORT\": \"true\"");
        launchSettings.Should().Contain("\"ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL\": \"http://localhost:16000\"");
        launchSettings.Should().Contain("\"ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL\": \"http://localhost:17000\"");
    }

    [Fact]
    public void AzureStorageConnectionString_TakesPrecedenceOverLegacySetting()
    {
        var webProgramSource = ReadWorkspaceFile("src", "AI.SpectrumAdventure.Web", "Program.cs");
        const string AspireConnectionString = "configuration.GetConnectionString(\"AzureStorage\")";
        const string LegacyConnectionString = "configuration[\"AzureStorage:ConnectionString\"]";

        webProgramSource.IndexOf(AspireConnectionString, StringComparison.Ordinal).Should().BePositive();
        webProgramSource.IndexOf(LegacyConnectionString, StringComparison.Ordinal).Should().BeGreaterThan(
            webProgramSource.IndexOf(AspireConnectionString, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ServiceDefaultHealthAndLivenessEndpoints_ReturnOkInDevelopment()
    {
        using var client = _factory.CreateClient();

        var healthResponse = await client.GetAsync("/health");
        var livenessResponse = await client.GetAsync("/alive");

        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        livenessResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void AppHost_DoesNotContainTrackedAiSecretsOrConcreteCredentials()
    {
        var appHostSource = ReadWorkspaceFile("src", "AI.SpectrumAdventure.AppHost", "Program.cs");

        appHostSource.Should().NotContain("AzureAI:ApiKey");
        appHostSource.Should().NotContain("AzureAI:Endpoint");
        appHostSource.Should().NotContain("AccountKey=");
        appHostSource.Should().NotContain("DefaultEndpointsProtocol=");
        appHostSource.Should().NotContain("sk-");
    }

    [Fact]
    public void ResetLocalStateScript_RequiresExplicitConfirmationAndOnlyTargetsNamedLocalState()
    {
        var resetScript = ReadWorkspaceFile("scripts", "reset-local-state.ps1");

        resetScript.Should().Contain("Read-Host");
        resetScript.Should().Contain("$confirmation -cne \"RESET\"");
        resetScript.Should().Contain("spectrum-adventure-postgres");
        resetScript.Should().Contain("spectrum-adventure-azurite");
        resetScript.Should().Contain("data-protection-keys");
        resetScript.Should().NotContain("az ");
        resetScript.Should().NotContain("docker compose down");
    }

    private static string ReadWorkspaceFile(params string[] pathSegments)
    {
        var workspaceRoot = FindWorkspaceRoot();
        return File.ReadAllText(Path.Combine([workspaceRoot, .. pathSegments]));
    }

    private static string FindWorkspaceRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AISpectrumAdventure.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the workspace root from the test output directory.");
    }
}