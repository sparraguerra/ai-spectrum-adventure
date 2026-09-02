namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record ApplyWorldEventResult(bool Success, string Narrative);

public sealed class ApplyWorldEventUseCase(IWorldRepository worldRepository)
{
    public async Task<ApplyWorldEventResult> ExecuteAsync(Game game, WorldEvent worldEvent, CancellationToken cancellationToken = default)
    {
        if (game.WorldId is not { } worldId) return new(false, "The world cannot respond right now.");
        var world = await worldRepository.FindAsync(worldId, cancellationToken);
        if (world is null) return new(false, "The world cannot respond right now.");

        try
        {
            world.Apply(worldEvent);
            await worldRepository.SaveWorldAndGameAsync(world, game, cancellationToken);
            return new(true, Describe(worldEvent));
        }
        catch (InvalidOperationException)
        {
            return new(false, "That consequence cannot take hold.");
        }
    }

    private static string Describe(WorldEvent worldEvent) => worldEvent switch
    {
        ConnectionStateChangedWorldEvent { Availability: ConnectionAvailability.Blocked } => "A familiar path is now blocked.",
        ConnectionStateChangedWorldEvent => "A familiar path has changed.",
        LocationStateChangedWorldEvent => "The place has changed.",
        NpcMovedWorldEvent => "Someone has moved on.",
        PuzzleStateChangedWorldEvent => "A puzzle consequence settles into the world.",
        _ => "The world has changed."
    };
}