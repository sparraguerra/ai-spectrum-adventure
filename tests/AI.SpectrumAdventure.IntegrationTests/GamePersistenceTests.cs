namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public class GamePersistenceTests
{
    [Fact]
    public async Task SaveThenFind_ReproducesAnIdenticalGameState()
    {
        var databaseName = Guid.NewGuid().ToString();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Sign, AdventureWorldFactory.ForestEntrance));

        await using (var writeContext = TestDbContextFactory.Create(databaseName))
        {
            var repository = new EfGameRepository(writeContext);
            await repository.SaveAsync(game);
        }

        await using var readContext = TestDbContextFactory.Create(databaseName);
        var reloaded = await new EfGameRepository(readContext).FindAsync(game.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Player.CurrentLocationId.Should().Be(game.Player.CurrentLocationId);
        reloaded.Player.Inventory.Contains(AdventureWorldFactory.Sign).Should().BeTrue();
        reloaded.EventHistory.Should().HaveCount(game.EventHistory.Count);
    }

    [Fact]
    public async Task FindAsync_UnknownGameId_ReturnsNull()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfGameRepository(context);

        var result = await repository.FindAsync(GameId.New());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ContinuingAGame_AfterReload_CanStillProcessNewActions()
    {
        var databaseName = Guid.NewGuid().ToString();
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        await using (var writeContext = TestDbContextFactory.Create(databaseName))
        {
            await new EfGameRepository(writeContext).SaveAsync(original);
        }

        await using var readContext = TestDbContextFactory.Create(databaseName);
        var reloaded = await new EfGameRepository(readContext).FindAsync(original.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        reloaded.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest);
    }

    [Fact]
    public async Task DroppedItem_SaveThenFind_ReplaysItsPlacementAtTheCurrentLocation()
    {
        var databaseName = Guid.NewGuid().ToString();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Go, "north", new Dictionary<string, string>(), 1, "go north"));
        AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Go, "east", new Dictionary<string, string>(), 1, "go east"));
        AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string>(), 1, "take bridge key"));
        var dropIntent = CommandPatternInterpreter.TryInterpret("drop bridge key");

        Enum.TryParse<IntentAction>("Drop", out var dropAction).Should().BeTrue();
        dropIntent.Should().NotBeNull();
        dropIntent!.Action.Should().Be(dropAction);
        AdventureOrchestrator.ProcessAction(game, dropIntent).Success.Should().BeTrue();

        await using (var writeContext = TestDbContextFactory.Create(databaseName))
        {
            await new EfGameRepository(writeContext).SaveAsync(game);
        }

        await using var readContext = TestDbContextFactory.Create(databaseName);
        var reloaded = await new EfGameRepository(readContext).FindAsync(game.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeFalse();
        reloaded.GetItem(AdventureWorldFactory.BridgeKey).LocationId.Should().Be(AdventureWorldFactory.OldBridge);
        reloaded.GetLocation(AdventureWorldFactory.OldBridge).ObjectIds.Should().Contain(AdventureWorldFactory.BridgeKey);
        reloaded.EventHistory.Should().Contain(eventEntry => eventEntry.GetType().Name == "ItemDroppedEvent");
    }
}
