namespace AI.SpectrumAdventure.Application.Lore;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

public enum LoreDiscoverySource { Location, Item, Npc, Puzzle, Event }

public sealed record LoreDiscoveryTrigger(LoreDiscoverySource Source, string SourceId);

public sealed record LoreDiscoveryRule(LoreDiscoverySource Source, string SourceId, LoreId LoreId);

public sealed class LoreDiscoveryRules(IEnumerable<LoreDiscoveryRule> rules)
{
    private readonly IReadOnlyList<LoreDiscoveryRule> _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));

    public IReadOnlyCollection<LoreId> Evaluate(World world, IEnumerable<LoreDiscoveryTrigger> triggers)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(triggers);
        var sources = triggers.Where(trigger => !string.IsNullOrWhiteSpace(trigger.SourceId)).ToHashSet();
        return _rules.Where(rule => sources.Contains(new LoreDiscoveryTrigger(rule.Source, rule.SourceId)))
            .Select(rule => rule.LoreId).Where(world.HasLore).Distinct().ToArray();
    }
}
