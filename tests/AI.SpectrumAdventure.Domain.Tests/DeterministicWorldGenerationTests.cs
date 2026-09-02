namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class DeterministicWorldGenerationTests
{
    [Fact]
    public void Create_IdenticalWorldBoundaryInputsProduceIdenticalKeyAndSelection()
    {
        var first = DeterministicGenerationKeyFactory.Create(new WorldSeed("seed"), new GenerationVersion("1"), new LocationId("forest-entrance"), ConnectionDirection.South, 0);
        var second = DeterministicGenerationKeyFactory.Create(new WorldSeed("seed"), new GenerationVersion("1"), new LocationId("forest-entrance"), ConnectionDirection.South, 0);

        second.Should().Be(first);
        DeterministicGenerationKeyFactory.SelectIndex(second, 10).Should().Be(DeterministicGenerationKeyFactory.SelectIndex(first, 10));
    }

    [Fact]
    public void IsTerrainTransitionPermitted_RejectsIncompatibleTerrain()
    {
        AI.SpectrumAdventure.Domain.Worlds.WorldGenerationRules.IsTerrainTransitionPermitted(TerrainKind.Forest, TerrainKind.Water).Should().BeFalse();
    }

    [Fact]
    public void Generate_IdenticalWorldBoundaryInputsProduceIdenticalStructuralCandidates()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        var request = new WorldGenerationRequest(world, AdventureWorldFactory.ForestEntrance, ConnectionDirection.South, 0);
        var key = DeterministicGenerationKeyFactory.Create(world.Seed, world.GenerationVersion, request.SourceLocationId, request.Direction, request.ExpansionOrdinal);
        var generator = new WorldGenerator();

        var firstLocation = generator.Generate(request, key, (Region?)null);
        var secondLocation = generator.Generate(request, key, (Region?)null);

        secondLocation.Id.Should().Be(firstLocation.Id);
        secondLocation.StructuralNameKey.Should().Be(firstLocation.StructuralNameKey);
        generator.Generate(request, key, firstLocation).Should().BeEquivalentTo(generator.Generate(request, key, secondLocation));
    }
}