namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Application.Games;

public interface IAdventureCatalog
{
    Task<IReadOnlyCollection<AdventureCatalogItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<string> GetDefinitionJsonAsync(string adventureId, CancellationToken cancellationToken = default);
}