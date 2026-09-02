namespace AI.SpectrumAdventure.Domain.Authoring;

public readonly record struct AdventureDraftId(Guid Value)
{
    public static AdventureDraftId New() => new(Guid.NewGuid());
}

public readonly record struct AdventureVersionId(Guid Value)
{
    public static AdventureVersionId New() => new(Guid.NewGuid());
}

public readonly record struct AuthoringProposalId(Guid Value)
{
    public static AuthoringProposalId New() => new(Guid.NewGuid());
}

public readonly record struct AuthoringValidationResultId(Guid Value)
{
    public static AuthoringValidationResultId New() => new(Guid.NewGuid());
}

public readonly record struct PlaytestSessionId(Guid Value)
{
    public static PlaytestSessionId New() => new(Guid.NewGuid());
}

public enum AdventureDraftStatus { Draft, Published, Archived }
public enum ProposalStatus { Pending, Accepted, Rejected, Invalid }
public enum AuthoringIssueSeverity { Informational, Blocking }
public enum AuthoringAction { ProposalAccepted, ProposalRejected, Published, Restored, PlaytestStarted }