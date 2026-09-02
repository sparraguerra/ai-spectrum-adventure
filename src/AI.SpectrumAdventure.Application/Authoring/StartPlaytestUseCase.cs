namespace AI.SpectrumAdventure.Application.Authoring;

using System.Security.Cryptography;
using System.Text;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Games;

public sealed record StartPlaytestUseCaseResult(bool Started, Game? Game, PlaytestSession? Session, AuthoringValidationResult Validation, IReadOnlyList<string> Issues)
{
    public Guid? GameId => Game?.Id.Value;
}

public sealed class StartPlaytestUseCase(
    IAdventureAuthoringRepository authoringRepository,
    IGameRepository gameRepository,
    IAdventureValidator validator,
    IWorldRepository? worldRepository = null)
{
    public async Task<StartPlaytestUseCaseResult> ExecuteAsync(AdventureDraftId draftId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("playtest");
        var draft = await authoringRepository.FindDraftAsync(draftId, cancellationToken);
        if (draft is null)
        {
            return Failure(draftId, expectedRevision, "The adventure draft was not found.");
        }

        if (draft.Revision != expectedRevision)
        {
            return Failure(draft.Id, draft.Revision, "The draft changed elsewhere. Reload it before starting a playtest.");
        }

        var validation = validator.Validate(draft);
        if (validation.HasBlockingIssues)
        {
            return new(false, null, null, validation, validation.Issues.Select(issue => $"{issue.Element}: {issue.Reason}").ToArray());
        }

        var snapshot = draft.DefinitionJson;
        var game = await new StartGameUseCase(gameRepository, worldRepository: worldRepository)
            .ExecuteFromDefinitionAsync(draft.AdventureIdentifier, snapshot, cancellationToken: cancellationToken);
        var session = new PlaytestSession(
            PlaytestSessionId.New(),
            draft.Id,
            draft.Revision,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))),
            game.Id.Value,
            game.CreatedAt);
        await authoringRepository.AddPlaytestSessionAsync(session, cancellationToken);
        return new(true, game, session, validation, []);
    }

    private static StartPlaytestUseCaseResult Failure(AdventureDraftId draftId, long revision, string reason)
    {
        var validation = new AuthoringValidationResult(draftId, revision, [new AuthoringValidationIssue("draft", reason, AuthoringIssueSeverity.Blocking)]);
        return new(false, null, null, validation, [reason]);
    }
}