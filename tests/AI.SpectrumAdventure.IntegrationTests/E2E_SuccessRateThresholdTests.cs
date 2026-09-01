namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public sealed class E2E_SuccessRateThresholdTests
{
    [Fact]
    public async Task CommandCorpus_MeetsClassicAndNaturalLanguageAcceptanceThresholds()
    {
        var interpreter = new TwoStageIntentInterpreter();
        var classic = new[]
        {
            ("look", IntentAction.Look), ("look around", IntentAction.Look), ("l", IntentAction.Look),
            ("go north", IntentAction.Go), ("north", IntentAction.Go), ("examine sign", IntentAction.Examine),
            ("inspect sign", IntentAction.Examine), ("take bridge key", IntentAction.Take), ("grab key", IntentAction.Take),
            ("use bridge key on tower entrance", IntentAction.Use),
        };
        var natural = new[]
        {
            ("I carefully inspect the weathered sign.", IntentAction.Examine),
            ("I walk north into the trees.", IntentAction.Go),
            ("I want to speak with the hermit.", IntentAction.TalkTo),
            ("I grab the bridge key from the boards.", IntentAction.Take),
            ("Please use the bridge key on the tower entrance.", IntentAction.Use),
        };

        var classicCorrect = 0;
        foreach (var (input, expectedAction) in classic)
        {
            if ((await interpreter.InterpretAsync(input)).Action == expectedAction) classicCorrect++;
        }

        var naturalCoherent = 0;
        foreach (var (input, expectedAction) in natural)
        {
            var intent = await interpreter.InterpretAsync(input);
            var result = AdventureOrchestrator.ProcessAction(E2ETestSupport.CreateStartedGame(), intent);
            if (intent.Action == expectedAction && !string.IsNullOrWhiteSpace(result.Narrative)) naturalCoherent++;
        }

        ((double)classicCorrect / classic.Length).Should().BeGreaterThanOrEqualTo(0.9);
        ((double)naturalCoherent / natural.Length).Should().BeGreaterThanOrEqualTo(0.8);
    }
}