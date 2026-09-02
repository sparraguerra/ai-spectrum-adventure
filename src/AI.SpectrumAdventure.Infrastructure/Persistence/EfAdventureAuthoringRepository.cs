namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Authoring;
using Microsoft.EntityFrameworkCore;

public sealed class EfAdventureAuthoringRepository(AdventureDbContext dbContext) : IAdventureAuthoringRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AdventureDrafts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AdventureDrafts.AsNoTracking().SingleOrDefaultAsync(x => x.AdventureIdentifier == adventureIdentifier, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AdventureDrafts.SingleOrDefaultAsync(x => x.Id == draft.Id.Value, cancellationToken);
        if (record is null)
        {
            if (expectedRevision != -1) throw new DbUpdateConcurrencyException("The draft does not exist.");
            dbContext.AdventureDrafts.Add(ToRecord(draft));
        }
        else
        {
            if (record.Revision != expectedRevision) throw new DbUpdateConcurrencyException("The draft revision is stale.");
            record.Title = draft.Title; record.StartingLocationId = draft.StartingLocationId; record.DefinitionJson = draft.DefinitionJson;
            record.Revision = draft.Revision; record.Status = (int)draft.Status; record.CurrentVersionId = draft.CurrentVersionId?.Value; record.ValidationJson = Serialize(draft.LastValidation); record.ConcurrencyToken = Guid.NewGuid();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AdventureDrafts.SingleOrDefaultAsync(x => x.Id == draft.Id.Value, cancellationToken)
            ?? throw new DbUpdateConcurrencyException("The draft does not exist.");
        if (record.Revision != expectedRevision) throw new DbUpdateConcurrencyException("The draft revision is stale.");
        if (record.CurrentVersionId is not null && record.CurrentVersionId != version.Id.Value)
            throw new DbUpdateConcurrencyException("The draft was published by another request.");

        record.Status = (int)draft.Status;
        record.CurrentVersionId = version.Id.Value;
        record.ValidationJson = Serialize(draft.LastValidation);
        record.ConcurrencyToken = Guid.NewGuid();
        dbContext.AdventureVersions.Add(new AdventureVersionRecord
        {
            Id = version.Id.Value,
            DraftId = version.DraftId.Value,
            AdventureIdentifier = version.AdventureIdentifier,
            Sequence = version.Sequence,
            DefinitionJson = version.DefinitionJson,
            PublishedAt = version.PublishedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default)
    {
        dbContext.AdventureVersions.Add(new AdventureVersionRecord { Id = version.Id.Value, DraftId = version.DraftId.Value, AdventureIdentifier = version.AdventureIdentifier, Sequence = version.Sequence, DefinitionJson = version.DefinitionJson, PublishedAt = version.PublishedAt });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default) =>
        await dbContext.AdventureVersions.AsNoTracking().Where(x => x.DraftId == draftId.Value).OrderBy(x => x.Sequence).Select(x => new AdventureVersion(new AdventureVersionId(x.Id), new AdventureDraftId(x.DraftId), x.AdventureIdentifier, x.Sequence, x.DefinitionJson, x.PublishedAt)).ToListAsync(cancellationToken);

    public async Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default)
    {
        dbContext.AuthoringProposals.Add(new AuthoringProposalRecord { Id = proposal.Id.Value, DraftId = proposal.DraftId.Value, ContentType = proposal.ContentType, RequestSummary = proposal.RequestSummary, PatchJson = proposal.PatchJson, Status = (int)proposal.Status, ValidationJson = Serialize(proposal.Validation) });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthoringProposal?> FindProposalAsync(AuthoringProposalId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AuthoringProposals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        return record is null ? null : ToProposal(record);
    }

    public async Task SaveProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AuthoringProposals.SingleOrDefaultAsync(x => x.Id == proposal.Id.Value, cancellationToken);
        if (record is null)
        {
            await AddProposalAsync(proposal, cancellationToken);
            return;
        }

        record.Status = (int)proposal.Status;
        record.ValidationJson = Serialize(proposal.Validation);
        record.PatchJson = proposal.PatchJson;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.AuthoringAuditEntries.Add(new AuthoringAuditEntryRecord { Id = entry.Id, DraftId = entry.DraftId.Value, Action = (int)entry.Action, OccurredAt = entry.OccurredAt, Source = entry.Source });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AdventureDraft ToDomain(AdventureDraftRecord record)
    {
        var draft = new AdventureDraft(new AdventureDraftId(record.Id), record.AdventureIdentifier, record.Title, record.StartingLocationId, record.DefinitionJson, record.Revision, (AdventureDraftStatus)record.Status);
        draft.RestoreMetadata(record.ValidationJson is null ? null : JsonSerializer.Deserialize<AuthoringValidationResult>(record.ValidationJson, JsonOptions), record.CurrentVersionId is null ? null : new AdventureVersionId(record.CurrentVersionId.Value));
        return draft;
    }
    private static AdventureDraftRecord ToRecord(AdventureDraft draft) => new() { Id = draft.Id.Value, AdventureIdentifier = draft.AdventureIdentifier, Title = draft.Title, StartingLocationId = draft.StartingLocationId, DefinitionJson = draft.DefinitionJson, Revision = draft.Revision, Status = (int)draft.Status, CurrentVersionId = draft.CurrentVersionId?.Value, ValidationJson = Serialize(draft.LastValidation), ConcurrencyToken = Guid.NewGuid() };
    private static string? Serialize<T>(T? value) => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);

    private static AuthoringProposal ToProposal(AuthoringProposalRecord record)
    {
        var proposal = new AuthoringProposal(new AuthoringProposalId(record.Id), new AdventureDraftId(record.DraftId), record.ContentType, record.RequestSummary, record.PatchJson, (ProposalStatus)record.Status);
        if (record.ValidationJson is not null)
        {
            var validation = JsonSerializer.Deserialize<AuthoringValidationResult>(record.ValidationJson, JsonOptions);
            proposal.RestoreReview((ProposalStatus)record.Status, validation);
        }
        return proposal;
    }

    public async Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default)
    {
        dbContext.PlaytestSessions.Add(new PlaytestSessionRecord { Id = session.Id.Value, DraftId = session.DraftId.Value, DraftRevision = session.DraftRevision, SnapshotHash = session.SnapshotHash, GameId = session.GameId, StartedAt = session.StartedAt, SourceVersionId = session.SourceVersionId?.Value });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}