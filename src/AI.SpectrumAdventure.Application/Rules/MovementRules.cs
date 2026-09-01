namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-007/FR-008: validates movement between connected locations.</summary>
public static class MovementRules
{
    public static RulesValidationResult Validate(string direction, Game game)
    {
        var exit = game.CurrentLocation.FindExit(direction);
        if (exit is null)
        {
            return RulesValidationResult.Fail(RulesFailureReason.NoSuchExit);
        }

        if (exit.RequiredCondition is not null && !exit.RequiredCondition.IsSatisfiedBy(game.WorldFlagKeys))
        {
            return RulesValidationResult.Fail(RulesFailureReason.ExitConditionNotMet);
        }

        var now = DateTimeOffset.UtcNow;
        var events = new List<GameEvent>
        {
            new PlayerMovedEvent(Guid.NewGuid(), now, game.Player.CurrentLocationId, exit.DestinationId),
        };

        if (!game.GetLocation(exit.DestinationId).Discovered)
        {
            events.Add(new LocationDiscoveredEvent(Guid.NewGuid(), now, exit.DestinationId));
        }

        return RulesValidationResult.Ok(events);
    }
}
