namespace AI.SpectrumAdventure.Infrastructure.Persistence;

public sealed class AdventureDraftRecord
{
    public Guid Id { get; set; }
    public required string AdventureIdentifier { get; set; }
    public required string Title { get; set; }
    public required string StartingLocationId { get; set; }
    public required string DefinitionJson { get; set; }
    public long Revision { get; set; }
    public int Status { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public string? ValidationJson { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class AdventureVersionRecord
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public required string AdventureIdentifier { get; set; }
    public long Sequence { get; set; }
    public required string DefinitionJson { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}

public sealed class AuthoringProposalRecord
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public required string ContentType { get; set; }
    public required string RequestSummary { get; set; }
    public required string PatchJson { get; set; }
    public int Status { get; set; }
    public string? ValidationJson { get; set; }
}

public sealed class AuthoringAuditEntryRecord
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public int Action { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string Source { get; set; }
}

public sealed class PlaytestSessionRecord
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public long DraftRevision { get; set; }
    public required string SnapshotHash { get; set; }
    public Guid GameId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public Guid? SourceVersionId { get; set; }
}