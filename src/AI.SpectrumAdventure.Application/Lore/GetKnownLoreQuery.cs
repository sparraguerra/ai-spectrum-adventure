namespace AI.SpectrumAdventure.Application.Lore;

using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

public static class GetKnownLoreQuery
{
    public static IReadOnlyCollection<LoreDiscoveryResult> Execute(Game game, World world) =>
        world.LoreEntries.Where(entry => game.PlayerKnowledge.Lore.ContainsKey(entry.Id.Value))
            .OrderBy(entry => game.PlayerKnowledge.Lore[entry.Id.Value])
            .Select(entry => DiscoverLoreUseCase.ToResult(entry, false)).ToArray();
}
