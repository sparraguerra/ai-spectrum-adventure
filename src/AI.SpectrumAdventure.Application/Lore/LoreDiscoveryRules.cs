namespace AI.SpectrumAdventure.Application.Lore;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;

public enum LoreDiscoverySource { Location, Item, Npc, Puzzle, Event, Region }

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
        var configuredLore = _rules.Where(rule => sources.Contains(new LoreDiscoveryTrigger(rule.Source, rule.SourceId)))
            .Select(rule => rule.LoreId);
        var referencedLore = world.LoreEntries
            .Where(entry => entry.IsPlayerDiscoverable && entry.SubjectReferences.Any(reference => sources.Any(trigger =>
                string.Equals(reference, trigger.SourceId, StringComparison.OrdinalIgnoreCase)
                && MatchesScope(entry.Scope, trigger.Source))))
            .Select(entry => entry.Id);
        return configuredLore.Concat(referencedLore).Where(world.HasLore).Distinct().ToArray();
    }

    private static bool MatchesScope(LoreScope scope, LoreDiscoverySource source) => scope switch
    {
        LoreScope.World => true,
        LoreScope.Regional => source == LoreDiscoverySource.Region,
        LoreScope.Location => source == LoreDiscoverySource.Location,
        LoreScope.Character => source == LoreDiscoverySource.Npc,
        LoreScope.Object => source == LoreDiscoverySource.Item,
        LoreScope.Puzzle => source == LoreDiscoverySource.Puzzle,
        LoreScope.Event => source == LoreDiscoverySource.Event,
        _ => false
    };
}
