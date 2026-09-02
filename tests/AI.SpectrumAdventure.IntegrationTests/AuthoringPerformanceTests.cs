namespace AI.SpectrumAdventure.IntegrationTests;

using System.Diagnostics;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AuthoringPerformanceTests
{
    [Fact]
    public async Task SmallDraftSaveAndValidate_P95CompletesWithinTwoSeconds()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var validator = new AdventureAuthoringValidator();
        var draft = new AdventureDraft(AdventureDraftId.New(), "performance-adventure", "Performance Adventure", "start", ValidDefinition());
        await repository.SaveDraftAsync(draft, -1);
        var useCases = new DraftUseCases(repository, validator);
        var durations = new List<double>(capacity: 100);
        var request = new AdventureDraftRequest(draft.AdventureIdentifier, draft.Title, draft.StartingLocationId, draft.DefinitionJson);

        for (var iteration = 0; iteration < 100; iteration++)
        {
            var current = await repository.FindDraftAsync(draft.Id);
            var stopwatch = Stopwatch.StartNew();
            var result = await useCases.SaveAsync(draft.Id, current!.Revision, request);
            stopwatch.Stop();
            result.Succeeded.Should().BeTrue();
            durations.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        var ordered = durations.OrderBy(duration => duration).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        p95.Should().BeLessThan(2000, "small draft save and deterministic validation should meet SC-011");
    }

    private static string ValidDefinition() => "{\"id\":\"performance-adventure\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}],\"objectIds\":[\"key\"]},{\"id\":\"room\"}],\"items\":[{\"id\":\"key\"}]}";
}