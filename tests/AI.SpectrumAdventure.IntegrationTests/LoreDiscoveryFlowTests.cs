namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Lore;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class LoreDiscoveryFlowTests
{
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
