namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AdventurePlaytestFlowTests
{
    [Fact]
    public async Task DraftPlaytestAndPublishedGameHaveEquivalentMovementAndPuzzleResults()
    {
        await using var context = TestDbContextFactory.Create();
        var authoring = new EfAdventureAuthoringRepository(context);
        var games = new EfGameRepository(context);
        var definition = "{\"id\":\"playtest-flow\",\"startingLocationId\":\"start\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}],\"objectIds\":[\"key\"]},{\"id\":\"room\"}],\"items\":[{\"id\":\"key\"}],\"puzzle\":{\"id\":\"gate\",\"requiredItemId\":\"key\",\"requiredClueKey\":\"\"}}";
        var draft = new AdventureDraft(AdventureDraftId.New(), "playtest-flow", "Playtest Flow", "start", definition);
        await authoring.SaveDraftAsync(draft, -1);
        var publisher = new PublishAdventureUseCase(authoring, new AdventureAuthoringValidator());
        var published = await publisher.ExecuteAsync(draft.Id, draft.Revision);
        published.Published.Should().BeTrue();

        var playtest = await new StartPlaytestUseCase(authoring, games, new AdventureAuthoringValidator()).ExecuteAsync(draft.Id, draft.Revision);
        var catalog = new DatabaseAdventureCatalog(context, "missing-adventures");
        var publishedGame = await new StartGameUseCase(games, catalog).ExecuteAsync("playtest-flow");

        var actions = new[]
        {
            new ParsedIntent(IntentAction.Take, "key", new Dictionary<string, string>(), 1, "take key"),
            new ParsedIntent(IntentAction.Go, "East", new Dictionary<string, string>(), 1, "go east"),
            new ParsedIntent(IntentAction.Use, "key", new Dictionary<string, string> { ["on"] = "gate" }, 1, "use key on gate"),
        };
        var playtestResults = actions.Select(action => AdventureOrchestrator.ProcessAction(playtest.Game!, action)).ToArray();
        var publishedResults = actions.Select(action => AdventureOrchestrator.ProcessAction(publishedGame, action)).ToArray();

        playtest.Started.Should().BeTrue();
        playtestResults.Select(result => (result.Success, result.Reason, result.CurrentLocationId, result.PuzzleEvaluation?.Accepted))
            .Should().Equal(publishedResults.Select(result => (result.Success, result.Reason, result.CurrentLocationId, result.PuzzleEvaluation?.Accepted)));
        playtest.Game!.Puzzle.Solved.Should().Be(publishedGame.Puzzle.Solved);
        context.PlaytestSessions.Should().ContainSingle(session => session.DraftId == draft.Id.Value && session.DraftRevision == draft.Revision);
    }
}