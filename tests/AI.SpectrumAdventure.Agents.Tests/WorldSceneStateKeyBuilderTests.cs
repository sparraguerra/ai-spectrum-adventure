namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.VisualArtDirector;
using FluentAssertions;

public sealed class WorldSceneStateKeyBuilderTests
{
    [Fact]
    public void Build_ChangesOnlyWhenWorldLocationOrMaterialSceneVersionChanges()
    {
        var worldId = Guid.NewGuid();
        var original = SceneStateKeyBuilder.Build(worldId, "forest-path", 1, ["opened-gate"], ["sign"]);
        var same = SceneStateKeyBuilder.Build(worldId, "forest-path", 1, ["opened-gate"], ["sign"]);
        var changedVersion = SceneStateKeyBuilder.Build(worldId, "forest-path", 2, ["opened-gate"], ["sign"]);
        var changedWorld = SceneStateKeyBuilder.Build(Guid.NewGuid(), "forest-path", 1, ["opened-gate"], ["sign"]);

        same.Should().Be(original);
        changedVersion.Should().NotBe(original);
        changedWorld.Should().NotBe(original);
    }
}