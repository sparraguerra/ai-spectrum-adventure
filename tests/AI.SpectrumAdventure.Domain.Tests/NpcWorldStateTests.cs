namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Npcs;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;
using Xunit;

public sealed class NpcWorldStateTests
{
    [Fact]
    public void Place_RelocatesNpcToOneExistingLocationAndRejectsInconsistentState()
    {
        var region = new Region(new RegionId("region"), RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(2), [new LocationId("start"), new LocationId("clearing")]);
        var world = new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("v1"), [region],
        [
            new WorldLocation(new LocationId("start"), region.Id, LocationType.Path, "start", "Start", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "day")),
            new WorldLocation(new LocationId("clearing"), region.Id, LocationType.Path, "clearing", "Clearing", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "day")),
        ], []);
        var npc = new Npc(new NpcId("warden"), "Warden", "watchful", ["local-rumour"], new LocationId("start"));
        NpcWorldStateService.Place(world, npc, new LocationId("start"), WorldEvent.FormatCause(WorldEventCause.WorldRule, "initial-placement"), DateTimeOffset.UnixEpoch);

        NpcWorldStateService.Place(world, npc, new LocationId("clearing"), WorldEvent.FormatCause(WorldEventCause.TurnElapsed, "patrol"), DateTimeOffset.UnixEpoch.AddMinutes(1));

        NpcWorldStateService.GetLocation(world, npc.Id).Should().Be(new LocationId("clearing"));
        npc.WorldLocationId.Should().Be(new LocationId("clearing"));
        world.Events.Should().HaveCount(2);
        var inconsistentNpc = new Npc(npc.Id, "Warden", "watchful", ["local-rumour"], new LocationId("start"));
        var relocate = () => NpcWorldStateService.Place(world, inconsistentNpc, new LocationId("start"), WorldEvent.FormatCause(WorldEventCause.WorldRule, "invalid"), DateTimeOffset.UnixEpoch.AddMinutes(2));
        relocate.Should().Throw<InvalidOperationException>();
    }
}