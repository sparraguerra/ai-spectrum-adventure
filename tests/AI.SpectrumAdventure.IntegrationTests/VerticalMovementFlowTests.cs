namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class VerticalMovementFlowTests
{
    [Fact]
    public async Task HarborAdventure_RecognizesDownAndUpAndTraversesAuthoredExits()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow, "tide-beneath-the-harbor");
        var interpreter = new TwoStageIntentInterpreter(new IntentInterpreterAgent(new FakeAgentRunner()));

        foreach (var command in new[] { "go north", "go east", "go north" })
        {
            var intent = await interpreter.InterpretAsync(command, game.TargetVocabulary);
            AdventureOrchestrator.ProcessAction(game, intent).Success.Should().BeTrue();
        }

        var downIntent = await interpreter.InterpretAsync("Down", game.TargetVocabulary);
        downIntent.Action.Should().Be(IntentAction.Go);
        downIntent.Target.Should().Be("Down");
        var down = AdventureOrchestrator.ProcessAction(game, downIntent);

        down.Success.Should().BeTrue();
        down.CurrentLocationId.Should().Be("sluice-chamber");

        var upIntent = await interpreter.InterpretAsync("go up", game.TargetVocabulary);
        var up = AdventureOrchestrator.ProcessAction(game, upIntent);

        upIntent.Target.Should().Be("Up");
        up.Success.Should().BeTrue();
        up.CurrentLocationId.Should().Be("leaning-lighthouse");
    }
}
