namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Authoring;

public interface IAdventureValidator
{
    AuthoringValidationResult Validate(AdventureDraft draft);
}