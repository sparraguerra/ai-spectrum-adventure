namespace AI.SpectrumAdventure.Infrastructure.Speech;

using System.Xml.Linq;
using Microsoft.Extensions.Options;

public sealed class NarrationSsmlBuilder(IOptions<AzureSpeechOptions> speechOptions)
{
    private static readonly XNamespace SynthesisNamespace = "http://www.w3.org/2001/10/synthesis";

    public string Build(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var options = speechOptions.Value;

        return new XElement(
            SynthesisNamespace + "speak",
            new XAttribute("version", "1.0"),
            new XAttribute(XNamespace.Xml + "lang", options.Locale),
            new XElement(
                SynthesisNamespace + "voice",
                new XAttribute("name", options.VoiceName),
                new XElement(
                    SynthesisNamespace + "prosody",
                    new XAttribute("pitch", options.Pitch),
                    new XAttribute("rate", options.Rate),
                    text)))
            .ToString(SaveOptions.DisableFormatting);
    }
}
