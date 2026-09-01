namespace AI.SpectrumAdventure.Contracts;

/// <summary>Correlates asynchronous image generation with the game whose Blazor circuit should be updated.</summary>
public sealed record ImageGenerationRequest(Guid GameId, VisualSceneSpec Scene);