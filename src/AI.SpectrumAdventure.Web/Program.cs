using AI.SpectrumAdventure.Agents;
using AI.SpectrumAdventure.Agents.ImagePipeline;
using AI.SpectrumAdventure.Agents.Narrator;
using AI.SpectrumAdventure.Agents.Npc;
using AI.SpectrumAdventure.Agents.WorldEnrichment;
using AI.SpectrumAdventure.Agents.Authoring;
using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.VisualArtDirector;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Application.Lore;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using AI.SpectrumAdventure.Infrastructure.Storage;
using AI.SpectrumAdventure.Web.Components;
using AI.SpectrumAdventure.Web.Services;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Blobs;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var applicationInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("AI.SpectrumAdventure.Application", "AI.SpectrumAdventure.Agents", "AI.SpectrumAdventure.Infrastructure");

        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }

        if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
        {
            tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = applicationInsightsConnectionString);
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (builder.Environment.IsDevelopment())
        {
            metrics.AddConsoleExporter();
        }

        if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
        {
            metrics.AddAzureMonitorMetricExporter(options => options.ConnectionString = applicationInsightsConnectionString);
        }
    });

builder.Services.AddDbContext<AdventureDbContext>(options =>
    options.UseNpgsql("Host=psql-spectrum-adventure-blvdr6cch7bk2.postgres.database.azure.com;Database=adventure;Username=pgadmin;Password=P@ssw0rd123!;Port=5432;Ssl Mode=Require;"));//builder.Configuration.GetConnectionString("AdventureDb")));
builder.Services.AddScoped<IGameRepository, EfGameRepository>();
builder.Services.AddScoped<IAdventureAuthoringRepository, EfAdventureAuthoringRepository>();
builder.Services.AddSingleton<IAdventureValidator, AdventureAuthoringValidator>();
builder.Services.AddScoped<DraftUseCases>();
builder.Services.AddScoped<PublishAdventureUseCase>();
builder.Services.AddScoped<VersionHistoryUseCases>();
builder.Services.AddScoped<StartPlaytestUseCase>();
builder.Services.AddScoped<ProposalUseCases>();
builder.Services.AddScoped<AuthoringPreviewService>();
builder.Services.AddScoped<IWorldRepository, EfWorldRepository>();
builder.Services.AddSingleton<WorldGenerator>();
builder.Services.AddSingleton<IRegionGenerator>(sp => sp.GetRequiredService<WorldGenerator>());
builder.Services.AddSingleton<ILocationGenerator>(sp => sp.GetRequiredService<WorldGenerator>());
builder.Services.AddSingleton<IConnectionGenerator>(sp => sp.GetRequiredService<WorldGenerator>());
builder.Services.AddSingleton<IWorldGenerationRules, WorldGenerationRules>();
builder.Services.AddSingleton<IWorldConstraintValidator, WorldConstraintValidator>();
builder.Services.AddSingleton<WorldEnrichmentValidator>();
builder.Services.AddScoped<WorldEnrichmentService>();
builder.Services.AddSingleton<IWorldEnrichmentAgent, WorldEnrichmentAgent>();
builder.Services.AddScoped<IWorldEnrichmentRepository, WorldEnrichmentRepository>();
builder.Services.AddScoped<ExploreUnknownDirectionUseCase>();
builder.Services.AddSingleton<WorldEventScheduler>();
builder.Services.AddScoped<ApplyWorldEventUseCase>();
builder.Services.AddSingleton(new LoreDiscoveryRules([]));
builder.Services.AddScoped<DiscoverLoreUseCase>();
builder.Services.AddScoped<IAdventureCatalog>(sp =>
{
    var localAdventureDirectory = Path.Combine(AppContext.BaseDirectory, "Adventures");
    return string.IsNullOrWhiteSpace(sp.GetRequiredService<IConfiguration>().GetConnectionString("AdventureDb"))
        ? new DatabaseAdventureCatalog(null, localAdventureDirectory)
        : new DatabaseAdventureCatalog(sp.GetRequiredService<AdventureDbContext>(), localAdventureDirectory);
});
builder.Services.AddSingleton<IIntentInterpreter, TwoStageIntentInterpreter>();

// Microsoft Agent Framework: a single Azure OpenAI-backed AIAgent shared by all agent roles (research.md Decisions
// 4/11) — each role's full system prompt is embedded per-call in its own BuildPrompt, so one underlying AIAgent
// suffices. DefaultAzureCredential is convenient for local dev; production should pin a specific credential
// (e.g., ManagedIdentityCredential) per constitution Principle XV.
builder.Services.AddSingleton<IAgentRunner>(sp =>
{
    var endpoint = builder.Configuration["AzureAI:Endpoint"];
    var deployment = builder.Configuration["AzureAI:ChatDeployment"] ?? "gpt-4o-mini";
    var client = new AzureOpenAIClient(new Uri(string.IsNullOrWhiteSpace(endpoint) ? "https://placeholder.openai.azure.com" : endpoint), new DefaultAzureCredential());
    AIAgent agent = client.GetChatClient(deployment).AsIChatClient().AsAIAgent(
        instructions: "You are a component of a text adventure game engine. Follow the per-request instructions exactly.",
        name: "AdventureAgent");
    return new AIAgentRunner(agent);
});
builder.Services.AddSingleton<INarratorAgent, NarratorAgent>();
builder.Services.AddSingleton<INpcAgent, NpcAgent>();
builder.Services.AddSingleton<IVisualArtDirector, VisualArtDirectorAgent>();
builder.Services.AddSingleton<IAuthoringProposalAgent, AuthoringProposalAgent>();

// Image pipeline (Phase 13/14): async, non-blocking generation with retro post-processing and Azure Blob storage.
builder.Services.AddSingleton<ImageGenerationQueue>();
builder.Services.AddSingleton<IImagePipeline>(sp => sp.GetRequiredService<ImageGenerationQueue>());
builder.Services.AddSingleton<ISceneUpdateNotifier, SceneUpdateNotifier>();
builder.Services.AddHostedService<ImageGenerationWorker>();
builder.Services.AddScoped<AI.SpectrumAdventure.Application.Abstractions.IImageGenerator>(sp =>
{
    var endpoint = builder.Configuration["AzureAI:Endpoint"];
    var deployment = builder.Configuration["AzureAI:ImageDeployment"] ?? "dall-e-3";
    var client = new AzureOpenAIClient(new Uri(string.IsNullOrWhiteSpace(endpoint) ? "https://placeholder.openai.azure.com" : endpoint), new DefaultAzureCredential());
    return new AzureOpenAiImageGenerator(client.GetImageClient(deployment));
});
builder.Services.AddScoped<IRetroImageProcessor, RetroImageProcessor>();
builder.Services.AddScoped<IVisualAssetBlobStore>(sp =>
{
    var connectionString = builder.Configuration["AzureStorage:ConnectionString"];
    var containerClient = connectionString is not null
        ? new BlobContainerClient(connectionString, "visual-assets")
        : new BlobContainerClient(new Uri("https://placeholder.blob.core.windows.net/visual-assets"), new DefaultAzureCredential());
    return new BlobVisualAssetStore(containerClient);
});
builder.Services.AddScoped<IVisualAssetRepository, EfVisualAssetRepository>();

var app = builder.Build();

if (builder.Configuration.GetValue("Database:MigrateOnStartup", true)
    && !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("AdventureDb")))
{
    using var migrationScope = app.Services.CreateScope();
    var logger = migrationScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await migrationScope.ServiceProvider.GetRequiredService<AdventureDbContext>().Database.MigrateAsync();
        logger.LogInformation("Database migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed at startup. The application cannot safely serve requests with an incomplete schema.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/docs/adventure-authoring-guide.md", () =>
{
    var manualPath = Path.Combine(AppContext.BaseDirectory, "docs", "adventure-authoring-guide.md");
    return File.Exists(manualPath)
        ? Results.File(manualPath, "text/markdown; charset=utf-8")
        : Results.NotFound();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
