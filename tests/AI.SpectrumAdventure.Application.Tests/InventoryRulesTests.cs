namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class InventoryRulesTests
{
    private static Game CreateGameAtOldBridge()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge));
        return game;
    }

    [Fact]
    public void ValidateTake_CollectibleItemPresent_SucceedsWithItemTakenEvent()
    {
        var game = CreateGameAtOldBridge();

        var result = InventoryRules.ValidateTake(AdventureWorldFactory.BridgeKey, game);

        result.Success.Should().BeTrue();
        result.ProposedEvents.Should().ContainSingle().Which.Should().BeOfType<ItemTakenEvent>();
    }

    [Fact]
    public void ValidateTake_ItemNotPresentHere_FailsWithTargetNotPresent()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        var result = InventoryRules.ValidateTake(AdventureWorldFactory.BridgeKey, game);

        result.Reason.Should().Be(RulesFailureReason.TargetNotPresent);
    }

    [Fact]
    public void ValidateTake_AlreadyInInventory_FailsWithAlreadyInState()
    {
        var game = CreateGameAtOldBridge();
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));

        var result = InventoryRules.ValidateTake(AdventureWorldFactory.BridgeKey, game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.AlreadyInState);
    }

    [Fact]
    public void ValidateUse_ItemNotInInventory_FailsWithItemNotInInventory()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        var result = ItemUsageRules.ValidateUse(AdventureWorldFactory.BridgeKey, null, game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.ItemNotInInventory);
        result.ProposedEvents.Should().BeEmpty();
    }
}
