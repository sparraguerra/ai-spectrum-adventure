namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class WorldEnrichmentFailureTests
{
    [Fact]
    public async Task ExecuteAsync_EnrichmentFailurePreservesCommittedExplorationAndFactualNarration()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var generator = new WorldGenerator();
        var enrichment = new WorldEnrichmentService(new ThrowingEnrichmentAgent(), new InMemoryPresentationRepository(), new WorldEnrichmentValidator());
        var useCase = new ExploreUnknownDirectionUseCase(new EfWorldRepository(context), generator, generator, generator,
            new WorldGenerationRules(), new WorldConstraintValidator(), enrichment);

        var result = await useCase.ExecuteAsync(game, "South");

        result.Success.Should().BeTrue();
        var persistedDestination = context.WorldLocations.Single(location => location.WorldId == game.WorldId!.Value.Value && location.Id == game.Player.CurrentLocationId.Value);
        result.Narrative.Should().Be(persistedDestination.BaseDescription);
        persistedDestination.Id.Should().Be(game.Player.CurrentLocationId.Value);
    }

    private sealed class ThrowingEnrichmentAgent : IWorldEnrichmentAgent
    {
        public Task<WorldEnrichmentProposal> EnrichAsync(WorldEnrichmentContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Enrichment unavailable.");
    }

    private sealed class InMemoryPresentationRepository : IWorldEnrichmentRepository
    {
        public Task<WorldPresentationMetadata?> FindAsync(Guid worldId, string locationId, int sceneVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorldPresentationMetadata?>(null);

        public Task SaveAsync(WorldPresentationMetadata metadata, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}