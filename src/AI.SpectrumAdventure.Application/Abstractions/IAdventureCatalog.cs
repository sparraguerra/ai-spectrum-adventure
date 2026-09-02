namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Domain.Authoring;

public interface IAdventureCatalog
{
    Task<IReadOnlyCollection<AdventureCatalogItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<string> GetDefinitionJsonAsync(string adventureId, CancellationToken cancellationToken = default);

    Task<AdventureCatalogDefinition> GetDefinitionAsync(string adventureId, AdventureVersionId? versionId = null, CancellationToken cancellationToken = default);
}

public sealed record AdventureCatalogDefinition(string AdventureId, string DefinitionJson, AdventureVersionId? VersionId);