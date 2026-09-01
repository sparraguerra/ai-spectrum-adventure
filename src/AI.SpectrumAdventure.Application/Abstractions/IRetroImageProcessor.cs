namespace AI.SpectrumAdventure.Application.Abstractions;

/// <summary>Deterministic, non-AI post-processing enforcing the retro 8-bit/ZX Spectrum aesthetic regardless of
/// the raw image model's output style (constitution Principle V; research.md Decision 5).</summary>
public interface IRetroImageProcessor
{
    byte[] Process(byte[] rawImageBytes);
}
