namespace AI.SpectrumAdventure.Domain.Lore;

using AI.SpectrumAdventure.Domain.Common;

public sealed class LoreEntry
{
    private readonly HashSet<string> _subjectReferences;

    public LoreId Id { get; }
    public LoreCategory Category { get; }
    public LoreScope Scope { get; }
    public LoreTruthClassification TruthClassification { get; }
    public string ContentKey { get; }
    public string Content { get; }
    public IReadOnlySet<string> SubjectReferences => _subjectReferences;
    public bool IsUncertain => TruthClassification == LoreTruthClassification.Rumour;

    public LoreEntry(LoreId id, LoreCategory category, LoreScope scope, LoreTruthClassification truthClassification, string contentKey, string content, IEnumerable<string>? subjectReferences = null)
    {
        if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("A lore identifier is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(contentKey)) throw new ArgumentException("A lore content key is required.", nameof(contentKey));
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Lore content is required.", nameof(content));
        if (category == LoreCategory.Rumour && truthClassification != LoreTruthClassification.Rumour) throw new InvalidOperationException("Rumour lore must be explicitly uncertain.");
        if (truthClassification == LoreTruthClassification.Rumour && category != LoreCategory.Rumour) throw new InvalidOperationException("Uncertain lore must use the rumour category.");

        Id = id;
        Category = category;
        Scope = scope;
        TruthClassification = truthClassification;
        ContentKey = contentKey;
        Content = content;
        _subjectReferences = [.. (subjectReferences ?? []).Where(reference => !string.IsNullOrWhiteSpace(reference))];
    }

    public bool Contradicts(LoreEntry other) =>
        other is not null &&
        !IsUncertain &&
        !other.IsUncertain &&
        (TruthClassification is LoreTruthClassification.ImmutableFact or LoreTruthClassification.HistoricalFact || other.TruthClassification is LoreTruthClassification.ImmutableFact or LoreTruthClassification.HistoricalFact) &&
        ContentKey.Equals(other.ContentKey, StringComparison.OrdinalIgnoreCase) &&
        !Content.Equals(other.Content, StringComparison.Ordinal);
}
