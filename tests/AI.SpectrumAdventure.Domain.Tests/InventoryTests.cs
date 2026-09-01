namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Players;
using FluentAssertions;
using Xunit;

public class InventoryTests
{
    [Fact]
    public void Add_ThenContains_ReturnsTrue()
    {
        var inventory = new Inventory();
        var key = new ItemId("bridge-key");

        inventory.Add(key);

        inventory.Contains(key).Should().BeTrue();
        inventory.Items.Should().ContainSingle().Which.Should().Be(key);
    }

    [Fact]
    public void Remove_ExistingItem_ReturnsTrueAndRemovesIt()
    {
        var inventory = new Inventory();
        var key = new ItemId("bridge-key");
        inventory.Add(key);

        var removed = inventory.Remove(key);

        removed.Should().BeTrue();
        inventory.Contains(key).Should().BeFalse();
    }

    [Fact]
    public void Remove_MissingItem_ReturnsFalse_AndDoesNotThrow()
    {
        var inventory = new Inventory();

        inventory.Remove(new ItemId("nonexistent")).Should().BeFalse();
    }

    [Fact]
    public void Contains_MissingItem_ReturnsFalse()
    {
        var inventory = new Inventory();

        inventory.Contains(new ItemId("bridge-key")).Should().BeFalse();
    }
}
