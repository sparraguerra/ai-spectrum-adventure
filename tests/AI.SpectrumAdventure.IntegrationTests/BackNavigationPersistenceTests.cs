namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public class BackNavigationPersistenceTests
{
    [Fact]
    public async Task SaveThenFind_PreservesRemainingBackNavigationHistory()
    {
        var databaseName = Guid.NewGuid().ToString();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        AdventureOrchestrator.ProcessAction(game, GoIntent("north"));
        AdventureOrchestrator.ProcessAction(game, GoIntent("east"));
        AdventureOrchestrator.ProcessAction(game, BackIntent());

        await using (var writeContext = TestDbContextFactory.Create(databaseName))
        {
            await new EfGameRepository(writeContext).SaveAsync(game);
        }

        await using var readContext = TestDbContextFactory.Create(databaseName);
        var reloaded = await new EfGameRepository(readContext).FindAsync(game.Id);
        reloaded.Should().NotBeNull();

        var back = AdventureOrchestrator.ProcessAction(reloaded!, BackIntent());

        back.Success.Should().BeTrue();
        back.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance.Value);
        reloaded!.Player.PreviousLocationIds.Should().BeEmpty();
    }

    private static ParsedIntent GoIntent(string direction) =>
        new(IntentAction.Go, direction, new Dictionary<string, string>(), 1.0, $"go {direction}");

    private static ParsedIntent BackIntent() =>
        new(IntentAction.Back, null, new Dictionary<string, string>(), 1.0, "back");
}
