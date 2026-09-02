namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class LoreConsistencyTests
{
    [Fact]
    public void Constructor_RejectsContradictoryImmutableOrHistoricalLore()
    {
        var immutable = new LoreEntry(new LoreId("tower-origin"), LoreCategory.History, LoreScope.World, LoreTruthClassification.ImmutableFact, "tower-origin", "The tower was built by the old king.");
        var contradiction = new LoreEntry(new LoreId("tower-origin-rewrite"), LoreCategory.History, LoreScope.World, LoreTruthClassification.HistoricalFact, "tower-origin", "The tower was built by a sea captain.");

        Action create = () => CreateWorld([immutable, contradiction]);

        create.Should().Throw<InvalidOperationException>().WithMessage("*cannot be contradictory*");
    }

    [Fact]
    public void Rumour_IsExplicitlyUncertain()
    {
        var rumour = new LoreEntry(new LoreId("whispered-treasure"), LoreCategory.Rumour, LoreScope.Location, LoreTruthClassification.Rumour, "whispered-treasure", "They say gold sleeps beneath the roots.", ["start"]);

        rumour.IsUncertain.Should().BeTrue();
        rumour.TruthClassification.Should().Be(LoreTruthClassification.Rumour);
    }

    private static World CreateWorld(IEnumerable<LoreEntry> lore)
    {
        var regionId = new RegionId("region");
        var location = new WorldLocation(new LocationId("start"), regionId, LocationType.Path, "start", "A path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        return new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("1"), [new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(1), [location.Id])], [location], [], loreEntries: lore);
    }
}
