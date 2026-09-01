namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_StartGameTests
{
    [Fact]
    public async Task StartGame_PlacesPlayerAtForestEntrance_WithNarrativeAndVisualRequest()
    {
        var game = await new StartGameUseCase(new E2EInMemoryGameRepository()).ExecuteAsync();
        var result = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Look));
        var pipeline = new RecordingImagePipeline();

        await VisualSceneOrchestrator.AttachVisualAsync(game, result, new StaticVisualArtDirector(), pipeline, new E2EVisualAssetRepository(), ensureCurrentScene: true);

        result.Success.Should().BeTrue();
        result.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance.Value);
        result.Narrative.Should().Contain("forest", Exactly.Once());
        pipeline.Requests.Should().ContainSingle();
    }
}