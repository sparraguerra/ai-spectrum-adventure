namespace AI.SpectrumAdventure.Infrastructure.Speech;

public sealed class AzureSpeechOptions
{
    public const string SectionName = "AzureSpeechTTS";

    public string Key { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string Locale { get; set; } = "en-US";

    public string VoiceName { get; set; } = "en-US-GuyNeural";

    public string Pitch { get; set; } = "-10%";

    public string Rate { get; set; } = "-20%";

    public int CacheSizeLimit { get; set; } = 128;

    public int CacheLifetimeMinutes { get; set; } = 30;
}
