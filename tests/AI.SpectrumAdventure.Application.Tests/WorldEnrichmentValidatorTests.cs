namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public sealed class WorldEnrichmentValidatorTests
{
    [Fact]
    public void Validate_RejectsProposalForDifferentLocationOrSceneVersion()
    {
        var context = CreateContext();
        var proposal = CreateProposal() with { LocationId = "other-location", SceneVersion = 2 };

        var result = new WorldEnrichmentValidator().Validate(context, proposal);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsForbiddenLoreAndVisualClaims()
    {
        var proposal = CreateProposal() with { LoreClaimKeys = ["forbidden-secret"], VisualCharacteristics = ["castle"] };

        var result = new WorldEnrichmentValidator().Validate(CreateContext(), proposal);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_AcceptsPresentationOnlyProposal()
    {
        var result = new WorldEnrichmentValidator().Validate(CreateContext(), CreateProposal());

        result.IsValid.Should().BeTrue();
    }

    private static WorldEnrichmentContext CreateContext() =>
        new(Guid.NewGuid(), "forest-path", "wilderness", 1, "Forest Path", "A factual path.", ["old-oak"], ["trees", "mist"]);

    private static WorldEnrichmentProposal CreateProposal() =>
        new("forest-path", 1, "Misty Forest Path", "Mist curls around the trees.", "mysterious", ["trees", "mist"], ["old-oak"]);
}