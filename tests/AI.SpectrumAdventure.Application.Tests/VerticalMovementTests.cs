namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;
using Xunit;

public class VerticalMovementTests
{
    [Fact]
    public void ProcessAction_DownUsesAuthoredExitAndRecordsOrigin()
    {
        var game = CreateGameWithExits([new ExitSnapshot("Down", AdventureWorldFactory.DarkForest.Value, null, null)]);

        var result = AdventureOrchestrator.ProcessAction(game, GoIntent("Down"));

        result.Success.Should().BeTrue();
        result.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest.Value);
        game.Player.PreviousLocationIds.Should().ContainSingle().Which.Should().Be(AdventureWorldFactory.ForestEntrance);
    }

    [Fact]
    public void ProcessAction_VerticalExitConditionNotMet_LeavesLocationAndHistoryUnchanged()
    {
        var game = CreateGameWithExits([new ExitSnapshot("Up", AdventureWorldFactory.DarkForest.Value, "upper-exit-open", true)]);

        var result = AdventureOrchestrator.ProcessAction(game, GoIntent("Up"));

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("ExitConditionNotMet");
        result.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance.Value);
        game.Player.PreviousLocationIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessActionAsync_MissingVerticalExit_DoesNotInvokeUnknownBoundaryExploration()
    {
        var game = CreateGameWithExits([], assignWorldId: true);
        var repository = new TrackingWorldRepository();
        var generator = new WorldGenerator();
        var exploration = new ExploreUnknownDirectionUseCase(
            repository,
            generator,
            generator,
            generator,
            new AI.SpectrumAdventure.Application.Worlds.WorldGenerationRules(),
            new WorldConstraintValidator());

        var result = await AdventureOrchestrator.ProcessActionAsync(game, GoIntent("Down"), exploration);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("NoSuchExit");
        result.Narrative.Should().Be("There is no path in that direction.");
        result.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance.Value);
        repository.FindCalls.Should().Be(0);
        game.Player.PreviousLocationIds.Should().BeEmpty();
    }

    private static ParsedIntent GoIntent(string direction) =>
        new(IntentAction.Go, direction, new Dictionary<string, string>(), 1.0, $"go {direction}");

    private static Game CreateGameWithExits(IReadOnlyCollection<ExitSnapshot> exits, bool assignWorldId = false)
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var snapshot = game.ToSnapshot();
        var locations = snapshot.Locations
            .Select(location => location.Id == AdventureWorldFactory.ForestEntrance.Value
                ? location with { Exits = [.. exits] }
                : location)
            .ToList();
        snapshot = snapshot with
        {
            Locations = locations,
            WorldId = assignWorldId ? Guid.NewGuid().ToString() : null,
        };

        return Game.FromSnapshot(snapshot);
    }

    private sealed class TrackingWorldRepository : IWorldRepository
    {
        public int FindCalls { get; private set; }

        public Task<World?> FindAsync(WorldId id, CancellationToken cancellationToken = default)
        {
            FindCalls++;
            return Task.FromResult<World?>(null);
        }

        public Task CreateInitialWorldAsync(World world, Game game, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(World world, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveWorldAndGameAsync(World world, Game game, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<World> PersistExpansionAsync(World world, WorldExpandedEvent expansion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<World> MaterializeLegacyWorldAsync(Game game, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
