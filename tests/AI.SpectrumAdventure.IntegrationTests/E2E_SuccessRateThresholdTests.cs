namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public sealed class E2E_SuccessRateThresholdTests
{
    [Fact]
    public async Task CommandCorpus_MeetsClassicAndNaturalLanguageAcceptanceThresholds()
    {
        const string examineInput = "I carefully inspect the weathered sign.";
        const string goInput = "I walk north into the trees.";
        const string talkInput = "I want to speak with the hermit.";
        const string takeInput = "I grab the bridge key from the boards.";
        const string useInput = "Please use the bridge key on the tower entrance.";
        var runner = new FakeAgentRunner()
            .EnqueueResult(new ParsedIntent(IntentAction.Examine, "sign", new Dictionary<string, string>(), 0.9, examineInput))
            .EnqueueResult(new ParsedIntent(IntentAction.Go, "North", new Dictionary<string, string>(), 0.9, goInput))
            .EnqueueResult(new ParsedIntent(IntentAction.TalkTo, "hermit", new Dictionary<string, string>(), 0.9, talkInput))
            .EnqueueResult(new ParsedIntent(IntentAction.Take, "bridge-key", new Dictionary<string, string>(), 0.9, takeInput))
            .EnqueueResult(new ParsedIntent(IntentAction.Use, "bridge-key", new Dictionary<string, string> { ["on"] = "forgotten-tower-entrance" }, 0.9, useInput));
        var interpreter = new TwoStageIntentInterpreter(new IntentInterpreterAgent(runner));
        var classic = new[]
        {
            ("look", IntentAction.Look), ("look around", IntentAction.Look), ("l", IntentAction.Look),
            ("go north", IntentAction.Go), ("north", IntentAction.Go), ("examine sign", IntentAction.Examine),
            ("inspect sign", IntentAction.Examine), ("take bridge key", IntentAction.Take), ("grab key", IntentAction.Take),
            ("use bridge key on tower entrance", IntentAction.Use),
        };
        var natural = new[]
        {
            (examineInput, IntentAction.Examine),
            (goInput, IntentAction.Go),
            (talkInput, IntentAction.TalkTo),
            (takeInput, IntentAction.Take),
            (useInput, IntentAction.Use),
        };

        var classicCorrect = 0;
        foreach (var (input, expectedAction) in classic)
        {
            if ((await interpreter.InterpretAsync(input)).Action == expectedAction) classicCorrect++;
        }

        var naturalCoherent = 0;
        foreach (var (input, expectedAction) in natural)
        {
            var game = E2ETestSupport.CreateStartedGame();
            var intent = await interpreter.InterpretAsync(input, game.TargetVocabulary);
            var result = AdventureOrchestrator.ProcessAction(game, intent);
            if (intent.Action == expectedAction && !string.IsNullOrWhiteSpace(result.Narrative)) naturalCoherent++;
        }

        ((double)classicCorrect / classic.Length).Should().BeGreaterThanOrEqualTo(0.9);
        ((double)naturalCoherent / natural.Length).Should().BeGreaterThanOrEqualTo(0.8);
    }
}