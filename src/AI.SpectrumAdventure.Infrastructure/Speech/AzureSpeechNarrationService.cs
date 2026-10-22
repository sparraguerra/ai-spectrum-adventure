namespace AI.SpectrumAdventure.Infrastructure.Speech;

using System.Security.Cryptography;
using System.Text;
using AI.SpectrumAdventure.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public sealed class AzureSpeechNarrationService : ISceneNarrationService, IDisposable
{
    private const string AudioContentType = "audio/mpeg";
    private readonly ISpeechSynthesisClient speechClient;
    private readonly AzureSpeechOptions options;
    private readonly MemoryCache cache;

    public AzureSpeechNarrationService(
        ISpeechSynthesisClient speechClient,
        IOptions<AzureSpeechOptions> options)
    {
        this.speechClient = speechClient;
        this.options = options.Value;
        cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = Math.Max(1, this.options.CacheSizeLimit)
        });
    }

    public async Task<SceneNarrationAudio> SynthesizeAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = CreateCacheKey(text);
        if (cache.TryGetValue(cacheKey, out SceneNarrationAudio? cachedAudio) && cachedAudio is not null)
        {
            return cachedAudio;
        }

        var audioBytes = await speechClient.SynthesizeAsync(text, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var audio = new SceneNarrationAudio(audioBytes, AudioContentType);
        cache.Set(
            cacheKey,
            audio,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, options.CacheLifetimeMinutes))
            });
        return audio;
    }

    public void Dispose() => cache.Dispose();

    private string CreateCacheKey(string text)
    {
        var keyMaterial = string.Join('\n', options.Locale, options.VoiceName, options.Pitch, options.Rate, text);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial)));
    }
}
