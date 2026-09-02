namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class WorldGenerationFailureTests
{
    [Fact]
    public async Task ExecuteAsync_PersistenceFailureLeavesPlayerAndWorldUnchanged()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        game.BindWorld(world.Id);
        var generator = new WorldGenerator();
        var useCase = new ExploreUnknownDirectionUseCase(new FailingWorldRepository(world), generator, generator, generator, new AI.SpectrumAdventure.Application.Worlds.WorldGenerationRules(), new WorldConstraintValidator());

        var result = await useCase.ExecuteAsync(game, "South");

        result.Success.Should().BeFalse();
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
        world.Locations.Should().HaveCount(game.Locations.Count);
    }

    private sealed class FailingWorldRepository(World world) : IWorldRepository
    {
        public Task CreateInitialWorldAsync(World value, Game game, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<World?> FindAsync(WorldId id, CancellationToken cancellationToken = default) => Task.FromResult<World?>(world);
        public Task SaveAsync(World value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveWorldAndGameAsync(World value, Game game, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<World> PersistExpansionAsync(World value, WorldExpandedEvent expansion, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Persistence failed.");
        public Task<World> MaterializeLegacyWorldAsync(Game game, CancellationToken cancellationToken = default) => Task.FromResult(world);
    }
}