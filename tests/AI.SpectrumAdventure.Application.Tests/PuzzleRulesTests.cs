namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class PuzzleRulesTests
{
    private static Game CreateGameHoldingBridgeKey()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));
        return game;
    }

    [Fact]
    public void ValidateSolve_WithItemButWithoutClue_FailsWithPuzzleConditionNotMet()
    {
        var game = CreateGameHoldingBridgeKey();

        var result = PuzzleRules.ValidateSolve(AdventureWorldFactory.BridgeKey, game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.PuzzleConditionNotMet);
        game.Puzzle.Solved.Should().BeFalse();
    }

    [Fact]
    public void ValidateSolve_WithClueButWithoutItem_FailsWithPuzzleConditionNotMet()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new NpcRelationshipChangedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, Domain.Games.WorldFlags.NpcGaveClue));

        var result = PuzzleRules.ValidateSolve(AdventureWorldFactory.BridgeKey, game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.PuzzleConditionNotMet);
    }

    [Fact]
    public void ValidateSolve_WithBothItemAndClue_SucceedsWithPuzzleSolvedEvent()
    {
        var game = CreateGameHoldingBridgeKey();
        game.Apply(new NpcRelationshipChangedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, Domain.Games.WorldFlags.NpcGaveClue));

        var result = PuzzleRules.ValidateSolve(AdventureWorldFactory.BridgeKey, game);

        result.Success.Should().BeTrue();
        result.ProposedEvents.Should().ContainSingle(e => e is PuzzleSolvedEvent);
    }

    [Fact]
    public void ValidateSolve_CreativeButIncorrectItem_FailsWithoutSolvingPuzzle()
    {
        var game = CreateGameHoldingBridgeKey();
        game.Apply(new NpcRelationshipChangedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, Domain.Games.WorldFlags.NpcGaveClue));

        var result = PuzzleRules.ValidateSolve(AdventureWorldFactory.Sign, game);

        result.Success.Should().BeFalse();
        game.Puzzle.Solved.Should().BeFalse();
    }
}
