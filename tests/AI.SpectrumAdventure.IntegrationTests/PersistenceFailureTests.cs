namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PersistenceFailureTests
{
    [Fact]
    public async Task SaveAsync_OnADisposedContext_ThrowsRatherThanSilentlyLosingData()
    {
        var context = TestDbContextFactory.Create();
        var repository = new EfGameRepository(context);
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        await context.DisposeAsync();

        var act = async () => await repository.SaveAsync(game);

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
