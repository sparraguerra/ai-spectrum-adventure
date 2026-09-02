namespace AI.SpectrumAdventure.IntegrationTests;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AdventureAuthoringManualTests
{
    [Fact]
    public async Task ManualContainsCompleteExampleAndDocumentedWorkflowSucceeds()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manualPath = Path.Combine(repositoryRoot, "docs", "adventure-authoring-guide.md");
        var manual = await File.ReadAllTextAsync(manualPath);

        foreach (var requiredSection in new[]
        {
            "## Authoring boundary", "## 1. Create a draft", "## 2. Build the world structure",
            "## 3. Add NPCs and lore", "## 4. Add puzzles and chains", "## Complete example",
            "## AI proposals", "## Validate and publish", "## Versions and restore",
            "## Playtest", "## Failure recovery"
        })
        {
            manual.Should().Contain(requiredSection);
        }

        var example = ExtractJsonExample(manual);
        using var document = JsonDocument.Parse(example);
        document.RootElement.GetProperty("locations").GetArrayLength().Should().BeGreaterThanOrEqualTo(3);
        document.RootElement.GetProperty("npcs").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        document.RootElement.GetProperty("lore").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        document.RootElement.GetProperty("puzzles").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        await using var context = TestDbContextFactory.Create();
        var authoring = new EfAdventureAuthoringRepository(context);
        var games = new EfGameRepository(context);
        var draftUseCases = new DraftUseCases(authoring, new AdventureAuthoringValidator());
        var created = await draftUseCases.CreateAsync(new(
            "lantern-trail", "The Lantern Trail", "gate", example));
        created.Succeeded.Should().BeTrue();
        created.Draft.Should().NotBeNull();

        var draft = created.Draft!;
        var validator = new AdventureAuthoringValidator();
        validator.Validate(draft).HasBlockingIssues.Should().BeFalse();

        var published = await new PublishAdventureUseCase(authoring, validator)
            .ExecuteAsync(draft.Id, draft.Revision);
        published.Published.Should().BeTrue();

        var playtest = await new StartPlaytestUseCase(authoring, games, validator)
            .ExecuteAsync(draft.Id, draft.Revision);
        playtest.Started.Should().BeTrue();
        playtest.Game.Should().NotBeNull();
    }

    private static string ExtractJsonExample(string manual)
    {
        const string startMarker = "```json";
        var start = manual.IndexOf(startMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        start = manual.IndexOf('\n', start + startMarker.Length) + 1;
        var end = manual.IndexOf("```", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        return manual[start..end].Trim();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }
}