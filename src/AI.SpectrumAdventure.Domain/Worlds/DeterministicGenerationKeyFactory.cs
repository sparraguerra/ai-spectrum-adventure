namespace AI.SpectrumAdventure.Domain.Worlds;

using System.Security.Cryptography;
using System.Text;
using AI.SpectrumAdventure.Domain.Common;

public static class DeterministicGenerationKeyFactory
{
    public static GenerationKey Create(WorldSeed seed, GenerationVersion version, LocationId sourceLocationId, ConnectionDirection direction, int expansionOrdinal)
    {
        if (expansionOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(expansionOrdinal));

        var input = $"{seed.Value}|{version.Value}|{sourceLocationId.Value}|{direction}|{expansionOrdinal}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new GenerationKey(Convert.ToHexString(hash).ToLowerInvariant());
    }

    public static int SelectIndex(GenerationKey generationKey, int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(generationKey.Value));
        return (int)(BitConverter.ToUInt32(hash, 0) % (uint)count);
    }
}