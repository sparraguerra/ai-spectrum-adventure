namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class IntentRecognitionFlowTests
{
    [Fact]
    public async Task NaturalLanguageIntentIsRuleValidatedBeforeChangingGameState()
    {
        const string takeInput = "Would you grab the key for me?";
        const string noActionInput = "The key is mentioned in the old story.";
        var runner = new FakeAgentRunner()
            .EnqueueResult(new ParsedIntent(IntentAction.Take, "key", new Dictionary<string, string>(), 0.94, takeInput))
            .EnqueueResult(ParsedIntent.Unknown(noActionInput));
        var interpreter = new TwoStageIntentInterpreter(new IntentInterpreterAgent(runner));
        var game = AdventureWorldFactory.CreateNewGameFromJson(
            GameId.New(),
            DateTimeOffset.UtcNow,
            """
            {
              "id": "intent-recognition-flow",
              "startingLocationId": "start",
              "locations": [
                { "id": "start", "objectIds": ["key"] }
              ],
              "items": [
                { "id": "key", "name": "Key", "states": ["Collectible"] }
              ],
              "targetVocabulary": [
                { "id": "key", "synonyms": ["the key"] }
              ]
            }
            """);

        var takeIntent = await interpreter.InterpretAsync(takeInput, game.TargetVocabulary);
        takeIntent.Action.Should().Be(IntentAction.Take);
        takeIntent.Target.Should().Be("key");
        var takeResult = AdventureOrchestrator.ProcessAction(game, takeIntent);

        takeResult.Success.Should().BeTrue(takeResult.Reason);
        game.Player.Inventory.Contains(new ItemId("key")).Should().BeTrue();

        var currentLocationId = game.Player.CurrentLocationId;
        var unknownIntent = await interpreter.InterpretAsync(noActionInput, game.TargetVocabulary);
        var unknownResult = AdventureOrchestrator.ProcessAction(game, unknownIntent);

        unknownResult.Success.Should().BeFalse();
        unknownResult.Narrative.Should().Contain("clarify");
        game.Player.CurrentLocationId.Should().Be(currentLocationId);
        game.Player.Inventory.Contains(new ItemId("key")).Should().BeTrue();
        runner.CallCount.Should().Be(2);
    }
}
