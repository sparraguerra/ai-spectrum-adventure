namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class UnknownBoundaryExplorationTests
{
    [Fact]
    public async Task ExecuteAsync_DoesNotExpandUnknownBoundaryWhenCurrentLocationHasDefinedExits()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var useCase = CreateUseCase(context);
        var initialLocationId = game.Player.CurrentLocationId;

        var result = await useCase.ExecuteAsync(game, "South");

        result.Success.Should().BeFalse();
        result.Narrative.Should().Be("There is no path in that direction.");
        game.Player.CurrentLocationId.Should().Be(initialLocationId);
    }

    private static ExploreUnknownDirectionUseCase CreateUseCase(AdventureDbContext context)
    {
        var generator = new WorldGenerator();
        return new ExploreUnknownDirectionUseCase(new EfWorldRepository(context), generator, generator, generator, new WorldGenerationRules(), new WorldConstraintValidator());
    }
}