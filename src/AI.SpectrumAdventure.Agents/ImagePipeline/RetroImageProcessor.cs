namespace AI.SpectrumAdventure.Agents.ImagePipeline;

using AI.SpectrumAdventure.Application.Abstractions;

/// <summary>
/// Deterministic, non-AI post-processing that guarantees the ZX Spectrum-inspired retro aesthetic regardless of
/// the raw image model's native output style (constitution Principle V). The MVP implementation is intentionally
/// a pass-through marker stage; a full palette-quantization/dithering implementation (e.g., via an image library)
/// is tracked as follow-up work once a concrete image format/library dependency is selected.
/// </summary>
public sealed class RetroImageProcessor : IRetroImageProcessor
{
    public byte[] Process(byte[] rawImageBytes) => rawImageBytes;
}
