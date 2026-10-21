namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

public static class BackNavigationRules
{
    public static RulesValidationResult Validate(Game game)
    {
        var previousLocationIds = game.Player.PreviousLocationIds;
        if (previousLocationIds.Count == 0)
        {
            return RulesValidationResult.Fail(RulesFailureReason.NoPreviousLocation);
        }

        var previousLocationId = previousLocationIds[^1];
        var backtrackedEvent = new PlayerBacktrackedEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            game.Player.CurrentLocationId,
            previousLocationId);

        return RulesValidationResult.Ok(backtrackedEvent);
    }
}
