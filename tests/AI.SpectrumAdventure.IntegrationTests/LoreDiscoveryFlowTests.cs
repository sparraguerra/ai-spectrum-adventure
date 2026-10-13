namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Lore;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class LoreDiscoveryFlowTests
{
    [Fact]
    public async Task StartGameFromAdventureDefinition_BootstrapsAndDiscoversAuthoredLore()
    {
        await using var context = TestDbContextFactory.Create();
        const string definition = """
        {
          "id": "harbor-lore",
          "title": "Harbor Lore",
          "startingLocationId": "harbor",
                    "locations": [{
                        "id": "harbor", "name": "Harbor", "description": "A quiet harbor.",
                        "type": "Settlement",
                        "environment": { "terrain": "Water", "climate": "coastal", "lighting": "night", "features": ["salt air"] }
                    }],
          "items": [],
          "npcs": [],
          "lore": [{
            "id": "first-tide",
            "classification": "PlayerDiscoverableKnowledge",
            "category": "Geography",
            "scope": "Location",
            "contentKey": "harbor-tide",
            "content": "The tide here rises before moonset.",
            "isProtected": true,
            "subjectReferences": ["harbor"]
                    }, {
                        "id": "regional-tide",
                        "classification": "RegionalFact",
                        "category": "History",
                        "scope": "Regional",
                        "contentKey": "harbor-history",
                        "content": "The harbor was built after the old sea wall fell."
                    }],
          "puzzle": { "id": "harbor-gate", "requiredItemId": "key", "requiredClueKey": "" }
        }
        """;
        context.Adventures.Add(new AdventureRecord
        {
            Id = "harbor-lore",
            Title = "Harbor Lore",
            Json = definition,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        });
        await context.SaveChangesAsync();

        var worldRepository = new EfWorldRepository(context);
        var game = await new StartGameUseCase(
            new EfGameRepository(context),
            new DatabaseAdventureCatalog(context, "missing-adventures"),
            worldRepository).ExecuteAsync("harbor-lore");
        var world = (await worldRepository.FindAsync(game.WorldId!.Value))!;
        var harbor = world.GetLocation(new LocationId("harbor"));
        harbor.Type.Should().Be(LocationType.Settlement);
        harbor.Environment.Terrain.Should().Be(TerrainKind.Water);
        world.Regions.Single().TerrainProfile.Should().Contain(TerrainKind.Water);
        world.LoreEntries.Should().ContainSingle(entry => entry.Id == new LoreId("first-tide"));
        var authoredLore = world.GetLore(new LoreId("first-tide"));
        authoredLore.Category.Should().Be(LoreCategory.Geography);
        authoredLore.Scope.Should().Be(LoreScope.Location);
        authoredLore.IsPlayerDiscoverable.Should().BeTrue();
        authoredLore.IsProtected.Should().BeTrue();
        world.GetLore(new LoreId("regional-tide")).SubjectReferences.Should().Contain("harbor-lore-initial");

        var discoveries = await new DiscoverLoreUseCase(worldRepository, new LoreDiscoveryRules([])).ExecuteAsync(
            game,
            world,
            [
                new LoreDiscoveryTrigger(LoreDiscoverySource.Location, "harbor"),
                new LoreDiscoveryTrigger(LoreDiscoverySource.Region, "harbor-lore-initial")
            ],
            DateTimeOffset.UnixEpoch);

        discoveries.Should().HaveCount(2).And.OnlyContain(entry => entry.IsNewDiscovery);
        var savedGame = (await new EfGameRepository(context).FindAsync(game.Id))!;
        GetKnownLoreQuery.Execute(savedGame, (await worldRepository.FindAsync(game.WorldId.Value))!)
            .Select(entry => entry.LoreId).Should().BeEquivalentTo("first-tide", "regional-tide");
    }

    [Fact]
    public async Task ExecuteAsync_DiscoversNpcObjectAndEnvironmentalLoreForLaterRecall()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var repository = new EfWorldRepository(context);
        var baseWorld = (await repository.FindAsync(game.WorldId!.Value))!;
        var loreWorld = WithLore(baseWorld);
        await repository.SaveAsync(loreWorld);

        var rules = new LoreDiscoveryRules([
            new(LoreDiscoverySource.Npc, "hermit", new LoreId("hermit")),
            new(LoreDiscoverySource.Item, "sign", new LoreId("sign")),
            new(LoreDiscoverySource.Location, "forest-entrance", new LoreId("mist"))]);
        var result = await new DiscoverLoreUseCase(repository, rules).ExecuteAsync(game, loreWorld,
            [new(LoreDiscoverySource.Npc, "hermit"), new(LoreDiscoverySource.Item, "sign"), new(LoreDiscoverySource.Location, "forest-entrance")], DateTimeOffset.UnixEpoch);

        result.Should().HaveCount(3).And.OnlyContain(entry => entry.IsNewDiscovery);
        var savedGame = (await new EfGameRepository(context).FindAsync(game.Id))!;
        var reloadedWorld = (await repository.FindAsync(game.WorldId.Value))!;
        GetKnownLoreQuery.Execute(savedGame, reloadedWorld).Select(entry => entry.LoreId).Should().BeEquivalentTo(["hermit", "sign", "mist"]);
    }

    private static World WithLore(World world) => new(world.Id, world.Seed, world.GenerationVersion, world.Regions, world.Locations, world.Connections, world.GenerationMetadata, world.Events,
    [
        new LoreEntry(new LoreId("hermit"), LoreCategory.Person, LoreScope.Character, LoreTruthClassification.CurrentFact, "hermit", "The hermit guards old secrets."),
        new LoreEntry(new LoreId("sign"), LoreCategory.Object, LoreScope.Object, LoreTruthClassification.CurrentFact, "sign", "The sign warns of a broken bridge."),
        new LoreEntry(new LoreId("mist"), LoreCategory.Geography, LoreScope.Location, LoreTruthClassification.CurrentFact, "mist", "Cold mist gathers here.", ["forest-entrance"])
    ]);
}
