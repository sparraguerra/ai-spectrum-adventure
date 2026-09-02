namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Lore;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class Feature002EndToEndTests
{
    [Fact]
    public async Task DynamicWorld_ExpandsValidBoundaryAndRejectsUnavailableDirection()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var generator = new WorldGenerator();
        var useCase = new ExploreUnknownDirectionUseCase(new EfWorldRepository(context), generator, generator, generator, new AI.SpectrumAdventure.Application.Worlds.WorldGenerationRules(), new WorldConstraintValidator());

        var rejected = await useCase.ExecuteAsync(game, "Sideways");
        var expanded = await useCase.ExecuteAsync(game, "South");
        var persisted = await new EfWorldRepository(context).FindAsync(game.WorldId!.Value);

        rejected.Success.Should().BeFalse();
        expanded.Success.Should().BeTrue();
        persisted!.Locations.Should().Contain(location => location.Id == game.Player.CurrentLocationId);
        game.PlayerKnowledge.Locations.Should().ContainKey(game.Player.CurrentLocationId.Value);
    }

    [Fact]
    public async Task DynamicWorld_DiscoversLoreOnceAndRetainsItAfterReload()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var repository = new EfWorldRepository(context);
        var baseWorld = (await repository.FindAsync(game.WorldId!.Value))!;
        var lore = new LoreEntry(new LoreId("forest-sign"), LoreCategory.Object, LoreScope.Object, LoreTruthClassification.CurrentFact, "forest-sign", "The sign points south.");
        var world = new World(baseWorld.Id, baseWorld.Seed, baseWorld.GenerationVersion, baseWorld.Regions, baseWorld.Locations, baseWorld.Connections, baseWorld.GenerationMetadata, baseWorld.Events, [lore]);
        await repository.SaveAsync(world);
        var discovery = new DiscoverLoreUseCase(repository, new LoreDiscoveryRules([new LoreDiscoveryRule(LoreDiscoverySource.Item, "sign", lore.Id)]));

        var first = await discovery.ExecuteAsync(game, world, [new LoreDiscoveryTrigger(LoreDiscoverySource.Item, "sign")], DateTimeOffset.UnixEpoch);
        var repeat = await discovery.ExecuteAsync(game, world, [new LoreDiscoveryTrigger(LoreDiscoverySource.Item, "sign")], DateTimeOffset.UnixEpoch.AddMinutes(1));
        var reloadedGame = (await new EfGameRepository(context).FindAsync(game.Id))!;

        first.Should().ContainSingle().Which.IsNewDiscovery.Should().BeTrue();
        repeat.Should().ContainSingle().Which.IsNewDiscovery.Should().BeFalse();
        reloadedGame.PlayerKnowledge.Lore.Should().ContainKey(lore.Id.Value);
    }
}