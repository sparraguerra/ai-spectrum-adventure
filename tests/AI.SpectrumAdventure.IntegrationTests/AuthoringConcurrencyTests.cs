namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class AuthoringConcurrencyTests
{
    [Fact]
    public async Task ConcurrentDraftSaves_OnlyOneAcceptedWriteSurvives()
    {
        var databaseName = Guid.NewGuid().ToString();
        var draft = await SeedAsync(databaseName);

        await using var contextA = TestDbContextFactory.Create(databaseName);
        await using var contextB = TestDbContextFactory.Create(databaseName);
        var useCaseA = new DraftUseCases(new EfAdventureAuthoringRepository(contextA), new AdventureAuthoringValidator());
        var useCaseB = new DraftUseCases(new EfAdventureAuthoringRepository(contextB), new AdventureAuthoringValidator());
        var requestA = new AdventureDraftRequest(draft.AdventureIdentifier, "Author A", draft.StartingLocationId, draft.DefinitionJson);
        var requestB = new AdventureDraftRequest(draft.AdventureIdentifier, "Author B", draft.StartingLocationId, draft.DefinitionJson);

        var results = await Task.WhenAll(
            useCaseA.SaveAsync(draft.Id, draft.Revision, requestA),
            useCaseB.SaveAsync(draft.Id, draft.Revision, requestB));

        results.Count(result => result.Succeeded).Should().Be(1);
        results.Count(result => result.Status == DraftOperationStatus.Conflict).Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentPublicationRequests_CreateAtMostOneVersion()
    {
        var databaseName = Guid.NewGuid().ToString();
        var draft = await SeedAsync(databaseName);

        await using var contextA = TestDbContextFactory.Create(databaseName);
        await using var contextB = TestDbContextFactory.Create(databaseName);
        var publisherA = new PublishAdventureUseCase(new EfAdventureAuthoringRepository(contextA), new AdventureAuthoringValidator());
        var publisherB = new PublishAdventureUseCase(new EfAdventureAuthoringRepository(contextB), new AdventureAuthoringValidator());

        var results = await Task.WhenAll(
            publisherA.ExecuteAsync(draft.Id, draft.Revision),
            publisherB.ExecuteAsync(draft.Id, draft.Revision));

        results.Count(result => result.Published).Should().Be(1);
        await using var verificationContext = TestDbContextFactory.Create(databaseName);
        (await new EfAdventureAuthoringRepository(verificationContext).GetVersionsAsync(draft.Id)).Should().HaveCount(1);
    }

    [Fact]
    public async Task PreviewFailure_ReturnsFactualFallbackWithoutBlockingAuthoring()
    {
        var service = new AuthoringPreviewService(new ThrowingVisualDirector(), new ThrowingImageGenerator(), new PassthroughRetroProcessor());
        var result = await service.CreateAsync(new VisualContext("start", "Tower entrance", ["iron key"], "tense", "start-v1"));

        result.UsedFallback.Should().BeTrue();
        result.ImageBytes.Should().BeNull();
        result.FactualSummary.Should().Contain("Tower entrance").And.Contain("iron key");
        result.Scene.Style.Should().Be("zx-spectrum-8-bit");
    }

    private static async Task<AdventureDraft> SeedAsync(string databaseName)
    {
        await using var context = TestDbContextFactory.Create(databaseName);
        var draft = new AdventureDraft(AdventureDraftId.New(), "race-adventure", "Race Adventure", "start", ValidDefinition());
        await new EfAdventureAuthoringRepository(context).SaveDraftAsync(draft, -1);
        return draft;
    }

    private static string ValidDefinition() => "{\"id\":\"race-adventure\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}],\"objectIds\":[\"key\"]},{\"id\":\"room\"}],\"items\":[{\"id\":\"key\"}]}";

    private sealed class ThrowingVisualDirector : IVisualArtDirector
    {
        public Task<VisualSceneSpec> DescribeSceneAsync(VisualContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("preview unavailable");
    }

    private sealed class ThrowingImageGenerator : IImageGenerator
    {
        public Task<byte[]> GenerateAsync(VisualSceneSpec spec, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("image unavailable");
    }

    private sealed class PassthroughRetroProcessor : IRetroImageProcessor
    {
        public byte[] Process(byte[] rawImageBytes) => rawImageBytes;
    }
}