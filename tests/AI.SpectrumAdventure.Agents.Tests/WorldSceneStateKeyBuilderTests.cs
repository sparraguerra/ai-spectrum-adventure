namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.VisualArtDirector;
using FluentAssertions;

public sealed class WorldSceneStateKeyBuilderTests
{
    [Fact]
    public void Build_ChangesForAdventureVersionOrMaterialSceneVersion_NotWorldInstance()
    {
        var adventureVersionId = Guid.NewGuid();
        var original = SceneStateKeyBuilder.Build("forgotten-tower", adventureVersionId, "forest-path", 1, ["opened-gate"], ["sign"]);
        var sameAcrossPlaythroughs = SceneStateKeyBuilder.Build("forgotten-tower", adventureVersionId, "forest-path", 1, ["opened-gate"], ["sign"]);
        var changedSceneVersion = SceneStateKeyBuilder.Build("forgotten-tower", adventureVersionId, "forest-path", 2, ["opened-gate"], ["sign"]);
        var changedAdventureVersion = SceneStateKeyBuilder.Build("forgotten-tower", Guid.NewGuid(), "forest-path", 1, ["opened-gate"], ["sign"]);

        sameAcrossPlaythroughs.Should().Be(original);
        changedSceneVersion.Should().NotBe(original);
        changedAdventureVersion.Should().NotBe(original);
    }
}