namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;
using FluentAssertions;
using System.Text.Json;

public sealed class DraftUseCaseTests
{
    [Fact]
    public async Task CreateSaveAndLoad_RoundTripsEditableDraft()
    {
        var repository = new InMemoryAuthoringRepository();
        var useCases = new DraftUseCases(repository, new PermissiveAdventureValidator());
        var request = new AdventureDraftRequest("harbor", "Harbor", "pier", "{\"locations\":[\"pier\"]}");

        var created = await useCases.CreateAsync(request);
        created.Succeeded.Should().BeTrue();
        created.Draft.Should().NotBeNull();
        AssertDefinitionMetadata(created.Draft!.DefinitionJson, "harbor", "Harbor", "pier");

        var saved = await useCases.SaveAsync(created.Draft!.Id, created.Draft.Revision,
            request with { Title = "Edited Harbor", StartingLocationId = "dock" });
        var reopened = await useCases.LoadAsync("harbor");

        saved.Succeeded.Should().BeTrue();
        reopened.Draft!.Title.Should().Be("Edited Harbor");
        reopened.Draft.StartingLocationId.Should().Be("dock");
        AssertDefinitionMetadata(reopened.Draft.DefinitionJson, "harbor", "Edited Harbor", "dock");
        reopened.Draft.Revision.Should().Be(1);
    }

    private static void AssertDefinitionMetadata(string definitionJson, string id, string title, string startingLocationId)
    {
        using var document = JsonDocument.Parse(definitionJson);
        var root = document.RootElement;
        root.GetProperty("id").GetString().Should().Be(id);
        root.GetProperty("title").GetString().Should().Be(title);
        root.GetProperty("startingLocationId").GetString().Should().Be(startingLocationId);
    }

    [Fact]
    public async Task CreateWithDuplicateIdentifier_DoesNotOverwriteExistingDraft()
    {
        var repository = new InMemoryAuthoringRepository();
        var useCases = new DraftUseCases(repository, new PermissiveAdventureValidator());
        var original = new AdventureDraftRequest("same-id", "Original", "start", "{}");

        await useCases.CreateAsync(original);
        var duplicate = await useCases.CreateAsync(original with { Title = "Replacement" });

        duplicate.Status.Should().Be(DraftOperationStatus.DuplicateIdentifier);
        (await useCases.LoadAsync("same-id")).Draft!.Title.Should().Be("Original");
    }

    private sealed class InMemoryAuthoringRepository : IAdventureAuthoringRepository
    {
        private readonly Dictionary<AdventureDraftId, AdventureDraft> drafts = [];

        public Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(drafts.GetValueOrDefault(id));

        public Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default) =>
            Task.FromResult(drafts.Values.SingleOrDefault(draft => draft.AdventureIdentifier == adventureIdentifier));

        public Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default)
        {
            if (drafts.TryGetValue(draft.Id, out var existing) && existing.Revision != expectedRevision)
            {
                throw new DraftRevisionConflictException("stale");
            }

            drafts[draft.Id] = draft;
            return Task.CompletedTask;
        }

        public Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default)
        {
            drafts[draft.Id] = draft;
            return Task.CompletedTask;
        }

        public Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdventureVersion>>([]);
        public Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}