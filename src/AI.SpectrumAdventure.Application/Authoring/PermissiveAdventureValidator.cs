namespace AI.SpectrumAdventure.Application.Authoring;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Authoring;

public sealed class PermissiveAdventureValidator : IAdventureValidator
{
    public AuthoringValidationResult Validate(AdventureDraft draft) =>
        new(draft.Id, draft.Revision, []);
}