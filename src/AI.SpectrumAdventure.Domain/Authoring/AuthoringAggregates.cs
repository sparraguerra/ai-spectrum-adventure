namespace AI.SpectrumAdventure.Domain.Authoring;

public sealed class AdventureVersion
{
    public AdventureVersionId Id { get; }
    public AdventureDraftId DraftId { get; }
    public string AdventureIdentifier { get; }
    public long Sequence { get; }
    public string DefinitionJson { get; }
    public DateTimeOffset PublishedAt { get; }

    public AdventureVersion(AdventureVersionId id, AdventureDraftId draftId, string adventureIdentifier, long sequence, string definitionJson, DateTimeOffset publishedAt)
    {
        Id = id; DraftId = draftId; AdventureIdentifier = adventureIdentifier; Sequence = sequence; DefinitionJson = definitionJson; PublishedAt = publishedAt;
    }
}

public sealed class AuthoringProposal
{
    public AuthoringProposalId Id { get; }
    public AdventureDraftId DraftId { get; }
    public string ContentType { get; }
    public string RequestSummary { get; }
    public string PatchJson { get; private set; }
    public ProposalStatus Status { get; private set; }
    public AuthoringValidationResult? Validation { get; private set; }

    public AuthoringProposal(AuthoringProposalId id, AdventureDraftId draftId, string contentType, string requestSummary, string patchJson, ProposalStatus status = ProposalStatus.Pending)
    {
        Id = id; DraftId = draftId; ContentType = contentType; RequestSummary = requestSummary; PatchJson = patchJson; Status = status;
    }

    public void Accept(AuthoringValidationResult validation) { if (validation.HasBlockingIssues) throw new InvalidOperationException("An invalid proposal cannot be accepted."); Validation = validation; Status = ProposalStatus.Accepted; }
    public void Reject() => Status = ProposalStatus.Rejected;
    public void MarkInvalid(AuthoringValidationResult validation) { Validation = validation; Status = ProposalStatus.Invalid; }
    public void Edit(string patchJson) { if (Status is not ProposalStatus.Pending and not ProposalStatus.Invalid) throw new InvalidOperationException("Only a pending proposal can be edited."); PatchJson = patchJson; Status = ProposalStatus.Pending; Validation = null; }
    public void RestoreReview(ProposalStatus status, AuthoringValidationResult? validation) { Status = status; Validation = validation; }
}

public sealed record AuthoringValidationIssue(string Element, string Reason, AuthoringIssueSeverity Severity)
{
    public bool Blocking => Severity == AuthoringIssueSeverity.Blocking;
}

public sealed class AuthoringValidationResult
{
    public AuthoringValidationResultId Id { get; }
    public AdventureDraftId DraftId { get; }
    public long EvaluationRevision { get; }
    public IReadOnlyList<AuthoringValidationIssue> Issues { get; }
    public bool IsValid => !HasBlockingIssues;
    public bool HasBlockingIssues => Issues.Any(issue => issue.Blocking);

    public AuthoringValidationResult(AdventureDraftId draftId, long evaluationRevision, IEnumerable<AuthoringValidationIssue> issues, AuthoringValidationResultId? id = null)
        : this(id ?? AuthoringValidationResultId.New(), draftId, evaluationRevision, issues.ToArray())
    {
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public AuthoringValidationResult(AuthoringValidationResultId id, AdventureDraftId draftId, long evaluationRevision, IReadOnlyList<AuthoringValidationIssue> issues)
    {
        Id = id; DraftId = draftId; EvaluationRevision = evaluationRevision; Issues = issues;
    }
}

public sealed record AuthoringAuditEntry(Guid Id, AdventureDraftId DraftId, AuthoringAction Action, DateTimeOffset OccurredAt, string Source);

public sealed record PlaytestSession(
    PlaytestSessionId Id,
    AdventureDraftId DraftId,
    long DraftRevision,
    string SnapshotHash,
    Guid GameId,
    DateTimeOffset StartedAt,
    AdventureVersionId? SourceVersionId = null);