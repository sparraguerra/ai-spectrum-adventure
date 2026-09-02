namespace AI.SpectrumAdventure.Domain.Tests.TestDoubles;

using System.Text.Json;
using AI.SpectrumAdventure.Domain.Authoring;

internal sealed class AdventureDraftBuilder
{
    private string identifier = "test-adventure";
    private string title = "Test Adventure";
    private string startingLocationId = "start";
    private string definitionJson = "{\"locations\":[{\"id\":\"start\"}]}";

    internal AdventureDraftBuilder WithIdentifier(string value) { identifier = value; return this; }
    internal AdventureDraftBuilder WithTitle(string value) { title = value; return this; }
    internal AdventureDraftBuilder WithStartingLocation(string value) { startingLocationId = value; return this; }
    internal AdventureDraftBuilder WithDefinition(object value) { definitionJson = JsonSerializer.Serialize(value); return this; }
    internal AdventureDraft Build() => new(AdventureDraftId.New(), identifier, title, startingLocationId, definitionJson);
}