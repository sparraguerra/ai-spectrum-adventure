namespace AI.SpectrumAdventure.IntegrationTests;

using System.Xml.Linq;
using AI.SpectrumAdventure.Infrastructure.Speech;
using FluentAssertions;
using Microsoft.Extensions.Options;

public sealed class NarrationSsmlBuilderTests
{
    [Fact]
    public void Build_UsesConfiguredLocaleVoiceAndProsody()
    {
        var options = new AzureSpeechOptions
        {
            Locale = "en-US",
            VoiceName = "en-US-GuyNeural",
            Pitch = "-10%",
            Rate = "-20%"
        };
        var builder = new NarrationSsmlBuilder(Options.Create(options));

        var document = XDocument.Parse(builder.Build("The forest is silent."));

        document.Root!.Name.Should().Be(XName.Get("speak", "http://www.w3.org/2001/10/synthesis"));
        document.Root.Attribute(XNamespace.Xml + "lang")!.Value.Should().Be("en-US");
        var voice = document.Root.Element(document.Root.Name.Namespace + "voice")!;
        voice.Attribute("name")!.Value.Should().Be("en-US-GuyNeural");
        var prosody = voice.Element(voice.Name.Namespace + "prosody")!;
        prosody.Attribute("pitch")!.Value.Should().Be("-10%");
        prosody.Attribute("rate")!.Value.Should().Be("-20%");
        prosody.Value.Should().Be("The forest is silent.");
    }

    [Fact]
    public void Build_EscapesNarrativeTextAsXmlText()
    {
        const string narrative = "The door & <shadows> remain.";
        var builder = new NarrationSsmlBuilder(Options.Create(new AzureSpeechOptions()));

        var document = XDocument.Parse(builder.Build(narrative));

        document.Descendants(XName.Get("prosody", "http://www.w3.org/2001/10/synthesis"))
            .Single()
            .Value
            .Should()
            .Be(narrative);
    }
}
