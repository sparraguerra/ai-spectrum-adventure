namespace AI.SpectrumAdventure.Domain.Npcs;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>A non-player character (spec Key Entity "NPC"); FR-017 requires the KnowledgeBoundary whitelist below.</summary>
public sealed class Npc
{
    private readonly HashSet<string> _knowledgeBoundary;
    private readonly HashSet<string> _allowedLoreReferences;
    private readonly List<string> _goals;
    private readonly List<ConversationTurn> _conversationMemory = [];
    private readonly List<WorldFlag> _relationshipFlags = [];

    public NpcId Id { get; }
    public string Name { get; }
    public string PersonalityProfile { get; }
    public IReadOnlySet<string> KnowledgeBoundary => _knowledgeBoundary;
    public IReadOnlySet<string> AllowedLoreReferences => _allowedLoreReferences;
    public IReadOnlyCollection<string> Goals => _goals;
    public LocationId? WorldLocationId { get; private set; }
    public long StateVersion { get; private set; }
    public IReadOnlyCollection<ConversationTurn> ConversationMemory => _conversationMemory;
    public IReadOnlyCollection<WorldFlag> RelationshipFlags => _relationshipFlags;

    public Npc(
        NpcId id,
        string name,
        string personalityProfile,
        IEnumerable<string> knowledgeBoundary,
        LocationId? worldLocationId = null,
        IEnumerable<string>? goals = null,
        IEnumerable<string>? allowedLoreReferences = null,
        long stateVersion = 1)
    {
        if (stateVersion < 1) throw new ArgumentOutOfRangeException(nameof(stateVersion));
        Id = id;
        Name = name;
        PersonalityProfile = personalityProfile;
        _knowledgeBoundary = [.. knowledgeBoundary];
        _allowedLoreReferences = [.. allowedLoreReferences ?? _knowledgeBoundary];
        _goals = [.. goals ?? []];
        WorldLocationId = worldLocationId;
        StateVersion = stateVersion;
    }

    /// <summary>Enforces FR-017: only topics explicitly on this whitelist may ever be revealed.</summary>
    public bool Knows(string topicKey) => _knowledgeBoundary.Contains(topicKey);

    internal void RecordConversationTurn(string playerUtterance, string npcReply, DateTimeOffset timestamp) =>
        _conversationMemory.Add(new ConversationTurn(playerUtterance, npcReply, timestamp));

    internal void RecordRelationshipFlag(WorldFlag flag)
    {
        _relationshipFlags.Add(flag);
        StateVersion++;
    }

    internal void RehydrateRelationshipFlag(WorldFlag flag) => _relationshipFlags.Add(flag);

    public void Relocate(LocationId locationId)
    {
        if (WorldLocationId == locationId) return;
        WorldLocationId = locationId;
        StateVersion++;
    }
}
