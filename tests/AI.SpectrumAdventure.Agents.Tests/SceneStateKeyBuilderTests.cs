namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.VisualArtDirector;
using FluentAssertions;
using Xunit;

public class SceneStateKeyBuilderTests
{
    [Fact]
    public void Build_SameLocationAndFlags_ProducesTheSameKey()
    {
        var key1 = SceneStateKeyBuilder.Build("dark-forest", ["npc-gave-clue"]);
        var key2 = SceneStateKeyBuilder.Build("dark-forest", ["npc-gave-clue"]);

        key1.Should().Be(key2);
    }

    [Fact]
    public void Build_SameFlagsInDifferentOrder_ProducesTheSameKey()
    {
        var key1 = SceneStateKeyBuilder.Build("dark-forest", ["tower-unlocked", "npc-gave-clue"]);
        var key2 = SceneStateKeyBuilder.Build("dark-forest", ["npc-gave-clue", "tower-unlocked"]);

        key1.Should().Be(key2);
    }

    [Fact]
    public void Build_DifferentRelevantFlags_ProducesADifferentKey()
    {
        var beforeUnlock = SceneStateKeyBuilder.Build("dark-forest", []);
        var afterUnlock = SceneStateKeyBuilder.Build("dark-forest", ["tower-unlocked"]);

        beforeUnlock.Should().NotBe(afterUnlock);
    }

    [Fact]
    public void Build_DifferentLocation_ProducesADifferentKey()
    {
        var forestEntrance = SceneStateKeyBuilder.Build("forest-entrance", []);
        var darkForest = SceneStateKeyBuilder.Build("dark-forest", []);

        forestEntrance.Should().NotBe(darkForest);
    }

    [Fact]
    public void Build_DifferentVisibleObjects_ProducesADifferentKey()
    {
        var beforeTakingKey = SceneStateKeyBuilder.Build("old-bridge", [], ["bridge-key"]);
        var afterTakingKey = SceneStateKeyBuilder.Build("old-bridge", [], []);

        beforeTakingKey.Should().NotBe(afterTakingKey);
    }

    [Fact]
    public void Build_SameVisibleObjectsInDifferentOrder_ProducesTheSameKey()
    {
        var key1 = SceneStateKeyBuilder.Build("old-bridge", [], ["bridge-key", "sign"]);
        var key2 = SceneStateKeyBuilder.Build("old-bridge", [], ["sign", "bridge-key"]);

        key1.Should().Be(key2);
    }
}
