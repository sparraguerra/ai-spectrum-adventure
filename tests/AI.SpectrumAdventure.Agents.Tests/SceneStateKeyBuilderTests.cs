namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.VisualArtDirector;
using FluentAssertions;
using Xunit;

public class SceneStateKeyBuilderTests
{
    private static readonly Guid AdventureVersionId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void Build_SameLocationAndFlags_ProducesTheSameKey()
    {
        var key1 = Build("dark-forest", ["npc-gave-clue"]);
        var key2 = Build("dark-forest", ["npc-gave-clue"]);

        key1.Should().Be(key2);
    }

    [Fact]
    public void Build_SameFlagsInDifferentOrder_ProducesTheSameKey()
    {
        var key1 = Build("dark-forest", ["tower-unlocked", "npc-gave-clue"]);
        var key2 = Build("dark-forest", ["npc-gave-clue", "tower-unlocked"]);

        key1.Should().Be(key2);
    }

    [Fact]
    public void Build_DifferentRelevantFlags_ProducesADifferentKey()
    {
        var beforeUnlock = Build("dark-forest", []);
        var afterUnlock = Build("dark-forest", ["tower-unlocked"]);

        beforeUnlock.Should().NotBe(afterUnlock);
    }

    [Fact]
    public void Build_DifferentLocation_ProducesADifferentKey()
    {
        var forestEntrance = Build("forest-entrance", []);
        var darkForest = Build("dark-forest", []);

        forestEntrance.Should().NotBe(darkForest);
    }

    [Fact]
    public void Build_DifferentVisibleObjects_ProducesADifferentKey()
    {
        var beforeTakingKey = Build("old-bridge", [], ["bridge-key"]);
        var afterTakingKey = Build("old-bridge", [], []);

        beforeTakingKey.Should().NotBe(afterTakingKey);
    }

    [Fact]
    public void Build_SameVisibleObjectsInDifferentOrder_ProducesTheSameKey()
    {
        var key1 = Build("old-bridge", [], ["bridge-key", "sign"]);
        var key2 = Build("old-bridge", [], ["sign", "bridge-key"]);

        key1.Should().Be(key2);
    }

    [Fact]
    public void Build_DifferentAdventureOrPublishedVersion_ProducesDifferentKeys()
    {
        var original = Build("forest-entrance", []);
        var differentAdventure = SceneStateKeyBuilder.Build("another-adventure", AdventureVersionId, "forest-entrance", 1, []);
        var differentVersion = SceneStateKeyBuilder.Build("forgotten-tower", Guid.NewGuid(), "forest-entrance", 1, []);

        differentAdventure.Should().NotBe(original);
        differentVersion.Should().NotBe(original);
    }

    private static string Build(string locationId, IEnumerable<string> flags, IEnumerable<string>? visibleObjectIds = null) =>
        SceneStateKeyBuilder.Build("forgotten-tower", AdventureVersionId, locationId, 1, flags, visibleObjectIds);
}
