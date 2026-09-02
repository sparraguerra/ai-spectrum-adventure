namespace AI.SpectrumAdventure.Application.Lore;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Application.Worlds;

public sealed class DiscoverLoreUseCase(IWorldRepository worldRepository, LoreDiscoveryRules discoveryRules)
{
    public async Task<IReadOnlyCollection<LoreDiscoveryResult>> ExecuteAsync(Game game, World world, IEnumerable<LoreDiscoveryTrigger> triggers, DateTimeOffset discoveredAt, CancellationToken cancellationToken = default)
    {
        using var activity = WorldTelemetry.Start("lore.discovery");
        if (game.WorldId != world.Id) throw new InvalidOperationException("The game must be bound to the supplied world.");
        var results = discoveryRules.Evaluate(world, triggers).Select(loreId =>
        {
            var lore = world.GetLore(loreId);
            return ToResult(lore, game.PlayerKnowledge.DiscoverLore(loreId, discoveredAt));
        }).ToArray();

        if (results.Any(result => result.IsNewDiscovery)) await worldRepository.SaveWorldAndGameAsync(world, game, cancellationToken);
    activity?.SetTag("world.lore_new_discoveries", results.Count(result => result.IsNewDiscovery));
        return results;
    }

    internal static LoreDiscoveryResult ToResult(Domain.Lore.LoreEntry lore, bool isNewDiscovery) => new(lore.Id.Value, lore.ContentKey, lore.Content, lore.Category.ToString(), lore.Scope.ToString(), lore.TruthClassification.ToString(), isNewDiscovery, lore.IsUncertain);
}
