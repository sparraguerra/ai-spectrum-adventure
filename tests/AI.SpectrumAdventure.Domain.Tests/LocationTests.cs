namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Locations;
using FluentAssertions;
using Xunit;

public class LocationTests
{
    [Fact]
    public void FindExit_ReturnsMatchingExit_CaseInsensitively()
    {
        var destination = new LocationId("dest");
        var location = new Location(new LocationId("origin"), "Origin", "desc", exits: [new Exit("North", destination)]);

        var exit = location.FindExit("north");

        exit.Should().NotBeNull();
        exit!.DestinationId.Should().Be(destination);
    }

    [Fact]
    public void FindExit_ReturnsNull_WhenNoSuchDirection()
    {
        var location = new Location(new LocationId("origin"), "Origin", "desc");

        location.FindExit("south").Should().BeNull();
    }

    [Fact]
    public void Exit_WithRequiredCondition_IsNotSatisfied_UntilFlagIsSet()
    {
        var condition = new WorldFlagCondition("tower-unlocked");
        var exit = new Exit("West", new LocationId("tower"), condition);

        exit.RequiredCondition!.IsSatisfiedBy(new HashSet<string>()).Should().BeFalse();
        exit.RequiredCondition!.IsSatisfiedBy(new HashSet<string> { "tower-unlocked" }).Should().BeTrue();
    }

    [Fact]
    public void MarkDiscovered_SetsDiscoveredTrue()
    {
        var location = new Location(new LocationId("origin"), "Origin", "desc");

        location.Discovered.Should().BeFalse();
        location.MarkDiscovered();
        location.Discovered.Should().BeTrue();
    }
}
