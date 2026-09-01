namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>US2 acceptance #4: repeated location description requests must be factually identical.</summary>
public class LocationDescriptionConsistencyTests
{
    [Fact]
    public void RepeatedLookAtSameLocation_WithNoInterveningAction_ReturnsIdenticalDescription()
    {
        var game = AdventureWorldFactory.CreateNewGame(Domain.Common.GameId.New(), DateTimeOffset.UtcNow);

        var first = GetLocationDescriptionQuery.Execute(game);
        var second = GetLocationDescriptionQuery.Execute(game);

        second.Should().Be(first);
    }

    [Fact]
    public void LookAfterTakingBridgeKey_RemovesKeyFromLocationDescription()
    {
        var game = AdventureWorldFactory.CreateNewGame(Domain.Common.GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge));

        var beforeTakingKey = GetLocationDescriptionQuery.Execute(game);
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));
        var afterTakingKey = GetLocationDescriptionQuery.Execute(game);

        beforeTakingKey.Should().Contain("You notice: Rusted Bridge Key.");
        afterTakingKey.Should().Be("A weathered wooden bridge spans a narrow ravine. Its planks creak underfoot. Something metallic glints between two loose boards. Exits: West.");
        afterTakingKey.Should().NotContain("Rusted Bridge Key");
    }
}
