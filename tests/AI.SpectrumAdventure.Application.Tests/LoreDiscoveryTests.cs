namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Lore;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class LoreDiscoveryTests
{
    [Fact]
    public async Task ExecuteAsync_IsIdempotentAndKnownLoreExcludesUndiscoveredEntries()
    {
        var world = CreateWorld();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        game.BindWorld(world.Id);
        var repository = new InMemoryWorldRepository(world);
        var useCase = new DiscoverLoreUseCase(repository, new LoreDiscoveryRules([new LoreDiscoveryRule(LoreDiscoverySource.Item, "sign", new LoreId("known"))]));

        var first = await useCase.ExecuteAsync(game, world, [new LoreDiscoveryTrigger(LoreDiscoverySource.Item, "sign")], DateTimeOffset.UnixEpoch);
        var duplicate = await useCase.ExecuteAsync(game, world, [new LoreDiscoveryTrigger(LoreDiscoverySource.Item, "sign")], DateTimeOffset.UnixEpoch.AddMinutes(1));
        var known = GetKnownLoreQuery.Execute(game, world);

        first.Should().ContainSingle().Which.IsNewDiscovery.Should().BeTrue();
        duplicate.Should().ContainSingle().Which.IsNewDiscovery.Should().BeFalse();
        game.PlayerKnowledge.Lore.Should().ContainSingle();
        known.Should().ContainSingle().Which.LoreId.Should().Be("known");
        repository.SaveCalls.Should().Be(1);
    }

    private static World CreateWorld()
    {
        var regionId = new RegionId("region");
        var location = new WorldLocation(new LocationId("start"), regionId, LocationType.Path, "start", "A path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        var lore = new[]
        {
            new LoreEntry(new LoreId("known"), LoreCategory.Object, LoreScope.Object, LoreTruthClassification.CurrentFact, "sign", "The sign points north."),
            new LoreEntry(new LoreId("hidden"), LoreCategory.History, LoreScope.World, LoreTruthClassification.HistoricalFact, "hidden", "An undiscovered truth.")
        };
        return new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("1"), [new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(1), [location.Id])], [location], [], loreEntries: lore);
    }

    private sealed class InMemoryWorldRepository(World world) : IWorldRepository
    {
        public int SaveCalls { get; private set; }
        public Task CreateInitialWorldAsync(World world, Game game, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<World?> FindAsync(WorldId id, CancellationToken cancellationToken = default) => Task.FromResult<World?>(world.Id == id ? world : null);
        public Task SaveAsync(World world, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveWorldAndGameAsync(World world, Game game, CancellationToken cancellationToken = default) { SaveCalls++; return Task.CompletedTask; }
        public Task<World> PersistExpansionAsync(World world, WorldExpandedEvent expansion, CancellationToken cancellationToken = default) => Task.FromResult(world);
        public Task<World> MaterializeLegacyWorldAsync(Game game, CancellationToken cancellationToken = default) => Task.FromResult(world);
    }
}
