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
    public async Task ExecuteAsync_ExpandsUnknownBoundaryPersistsDestinationAndSupportsRepeatTraversal()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var useCase = CreateUseCase(context);

        var result = await useCase.ExecuteAsync(game, "South");

        result.Success.Should().BeTrue();
        var destinationId = game.Player.CurrentLocationId;
        game.GetLocation(destinationId).BaseDescription.Should().NotBeNullOrWhiteSpace();
        (await new EfWorldRepository(context).FindAsync(game.WorldId!.Value))!.Locations.Should().Contain(location => location.Id == destinationId);

        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, destinationId, AdventureWorldFactory.ForestEntrance));
        MovementRules.Validate("South", game).Success.Should().BeTrue();
    }

    private static ExploreUnknownDirectionUseCase CreateUseCase(AdventureDbContext context)
    {
        var generator = new WorldGenerator();
        return new ExploreUnknownDirectionUseCase(new EfWorldRepository(context), generator, generator, generator, new WorldGenerationRules(), new WorldConstraintValidator());
    }
}