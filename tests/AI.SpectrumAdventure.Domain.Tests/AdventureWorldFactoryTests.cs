namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Puzzles;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Application.Worlds;
using FluentAssertions;
using Xunit;

public class AdventureWorldFactoryTests
{
    private static Game CreateGame() => AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

    [Fact]
    public void CreateNewGame_ProducesExactlyTheFourMvpLocations()
    {
        var game = CreateGame();

        game.Locations.Select(l => l.Id).Should().BeEquivalentTo(
        [
            AdventureWorldFactory.ForestEntrance,
            AdventureWorldFactory.DarkForest,
            AdventureWorldFactory.OldBridge,
            AdventureWorldFactory.ForgottenTower,
        ]);
    }

    [Fact]
    public void Player_StartsAtForestEntrance()
    {
        var game = CreateGame();

        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
    }

    [Fact]
    public void ForestEntrance_HasNorthExitToDarkForest_AndTheSign()
    {
        var game = CreateGame();
        var forestEntrance = game.GetLocation(AdventureWorldFactory.ForestEntrance);

        forestEntrance.FindExit("North")!.DestinationId.Should().Be(AdventureWorldFactory.DarkForest);
        forestEntrance.ObjectIds.Should().Contain(AdventureWorldFactory.Sign);
    }

    [Fact]
    public void DarkForest_HasNpcAndThreeExits_AndTowerExitIsGatedByTowerUnlockedFlag()
    {
        var game = CreateGame();
        var darkForest = game.GetLocation(AdventureWorldFactory.DarkForest);

        darkForest.NpcIds.Should().Contain(AdventureWorldFactory.Hermit);
        darkForest.Exits.Should().HaveCount(3);
        var towerExit = darkForest.FindExit("West");
        towerExit!.RequiredCondition!.RequiredFlagKey.Should().Be(WorldFlags.TowerUnlocked);
    }

    [Fact]
    public void OldBridge_ContainsTheBridgeKey()
    {
        var game = CreateGame();

        game.GetLocation(AdventureWorldFactory.OldBridge).ObjectIds.Should().Contain(AdventureWorldFactory.BridgeKey);
    }

    [Fact]
    public void ForgottenTowerEntrance_IsInaccessibleUntilPuzzleSolved()
    {
        var game = CreateGame();

        game.HasFlag(WorldFlags.TowerUnlocked).Should().BeFalse();
        game.Puzzle.Solved.Should().BeFalse();
    }

    [Fact]
    public void Puzzle_RequiresBridgeKeyAndTowerClue()
    {
        var game = CreateGame();

        game.Puzzle.RequiredItemId.Should().Be(AdventureWorldFactory.BridgeKey);
        game.Puzzle.RequiredClueKey.Should().Be(AdventureWorldFactory.TowerClueKey);
    }

    [Fact]
    public void Hermit_OnlyKnowsTheTowerClue()
    {
        var game = CreateGame();
        var hermit = game.GetNpc(AdventureWorldFactory.Hermit);

        hermit.Knows(AdventureWorldFactory.TowerClueKey).Should().BeTrue();
        hermit.Knows("something-unrelated").Should().BeFalse();
    }

    [Fact]
    public void CreateNewGame_LoadsForgottenTowerTargetVocabulary()
    {
        var game = CreateGame();

        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("weathered sign", "sign"));
        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("rusted bridge key", "bridge-key"));
        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("the hermit", "hermit"));
        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("tower", "forgotten-tower"));
    }

    [Fact]
    public void CreateNewGame_LoadsHarborTargetVocabulary()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow, "tide-beneath-the-harbor");

        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("rusted buoy", "rusted-buoy"));
        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("bronze piece", "bronze-counterweight"));
        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("sluice gate", "submerged-sluice"));
        game.TargetVocabulary.Should().Contain(new KeyValuePair<string, string>("lighthouse keeper", "ines-the-lighthouse-keeper"));
    }

    [Fact]
    public void CreateNewGameFromJson_LoadsMultiplePuzzleDefinitionsWithAlternativesAndChains()
    {
        const string json = """
                        {
                            "id": "crystal-caves",
                            "title": "Crystal Caves",
                            "startingLocationId": "entrance",
                            "locations": [{ "id": "entrance", "name": "Entrance", "description": "A cave entrance." }],
                            "items": [{ "id": "lamp", "name": "Lamp", "description": "A lamp.", "states": ["visible", "collectible"] }],
                            "npcs": [],
                            "puzzles": [
                                {
                                    "id": "crystal-door",
                                    "solutions": [
                                        { "id": "light-the-way", "conditions": [{ "type": "itemPossessed", "referenceId": "lamp" }] },
                                        { "id": "know-the-way", "conditions": [{ "type": "clueKnown", "referenceId": "crystal-riddle" }] }
                                    ],
                                    "outcomes": [{ "id": "reveal-vault", "type": "followOnPuzzle", "referenceId": "vault-lock" }],
                                    "chainLinks": ["vault-lock"]
                                },
                                {
                                    "id": "vault-lock",
                                    "solutions": [{ "id": "use-lamp", "conditions": [{ "type": "itemPossessed", "referenceId": "lamp" }] }]
                                }
                            ]
                        }
                        """;

        var game = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UtcNow, json);

        game.Puzzles.Should().HaveCount(2);
        var crystalDoor = game.GetPuzzle(new PuzzleId("crystal-door"));
        crystalDoor.Solutions.Should().HaveCount(2);
        crystalDoor.ChainLinks.Should().ContainSingle(link => link.NextPuzzleId == new PuzzleId("vault-lock"));
        crystalDoor.Outcomes.Should().ContainSingle(outcome => outcome.Type == PuzzleOutcomeType.FollowOnPuzzle && outcome.ReferenceId == "vault-lock");
    }

    [Fact]
    public void CreateNewGameFromJson_CreatesACustomAdventureDefinition()
    {
        const string json = """
                {
                    "id": "crystal-mine",
                    "title": "Crystal Mine",
                    "startingLocationId": "mine-entrance",
                    "locations": [
                        {
                            "id": "mine-entrance",
                            "name": "Mine Entrance",
                            "description": "A cold mine entrance glitters under moonlight.",
                            "exits": [
                                { "direction": "Down", "to": "crystal-cavern" }
                            ],
                            "objectIds": ["lamp"]
                        },
                        {
                            "id": "crystal-cavern",
                            "name": "Crystal Cavern",
                            "description": "Blue crystals hum in the dark.",
                            "exits": [
                                { "direction": "Up", "to": "mine-entrance" }
                            ]
                        }
                    ],
                    "items": [
                        {
                            "id": "lamp",
                            "name": "Brass Lamp",
                            "description": "A small brass lamp with a steady flame.",
                            "states": ["visible", "collectible"]
                        }
                    ],
                    "npcs": [],
                    "puzzle": {
                        "id": "cavern-gate",
                        "requiredItemId": "lamp",
                        "requiredClueKey": "mine-clue"
                    }
                }
                """;

        var game = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UtcNow, json);

        game.Player.CurrentLocationId.Should().Be(new LocationId("mine-entrance"));
        game.GetLocation(new LocationId("mine-entrance")).ObjectIds.Should().Contain(new ItemId("lamp"));
        game.GetItem(new ItemId("lamp")).Name.Should().Be("Brass Lamp");
        game.Puzzle.RequiredClueKey.Should().Be("mine-clue");
    }

    [Fact]
    public void CreateNewGameFromJson_PreservesAuthoredLocationTypeAndEnvironmentAcrossSnapshot()
    {
        const string json = """
                {
                    "id": "crystal-caves",
                    "title": "Crystal Caves",
                    "startingLocationId": "entrance",
                    "locations": [{
                        "id": "entrance",
                        "name": "Entrance",
                        "description": "A cave entrance.",
                        "type": "Cavern",
                        "environment": { "terrain": "Stone", "climate": "cold", "lighting": "dark", "features": ["crystals"] }
                    }],
                    "items": [{ "id": "key", "name": "Key", "description": "A key.", "states": ["Collectible"] }],
                    "npcs": [],
                    "puzzle": { "id": "gate", "requiredItemId": "key", "requiredClueKey": "" }
                }
                """;

        var game = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UnixEpoch, json);
        var reloaded = Game.FromSnapshot(game.ToSnapshot());
        var location = reloaded.GetLocation(new LocationId("entrance"));

        location.Type.Should().Be(LocationType.Cavern);
        location.Environment.Terrain.Should().Be(TerrainKind.Stone);
        location.Environment.Climate.Should().Be("cold");
        location.Environment.Lighting.Should().Be("dark");
        location.Environment.Features.Should().Equal("crystals");

        var worldLocation = WorldBootstrapFactory.Create(reloaded, WorldId.New()).GetLocation(location.Id);
        worldLocation.Type.Should().Be(LocationType.Cavern);
        worldLocation.Environment.Terrain.Should().Be(TerrainKind.Stone);
        worldLocation.Environment.Features.Should().Equal("crystals");
    }

    [Fact]
    public void CreateNewGameFromJson_PreservesNpcGoalsRelationshipsAndLocationAcrossSnapshot()
    {
        const string json = """
                {
                    "id": "npc-adventure",
                    "title": "NPC Adventure",
                    "startingLocationId": "harbor",
                      "locations": [{ "id": "harbor", "name": "Harbor", "description": "A harbor." }],
                    "items": [],
                    "npcs": [{
                        "id": "keeper",
                        "name": "Keeper",
                        "personality": "Careful and kind.",
                        "knowledgeBoundary": ["harbor-secret"],
                        "goals": ["Protect the lighthouse"],
                        "relationships": ["Trusts the harbor master"],
                        "locationId": "harbor"
                    }],
                    "puzzle": { "id": "harbor-gate", "requiredItemId": "key", "requiredClueKey": "" }
                }
                """;

        var game = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UnixEpoch, json);
        var npc = Game.FromSnapshot(game.ToSnapshot()).GetNpc(new NpcId("keeper"));

        game.GetLocation(new LocationId("harbor")).NpcIds.Should().Contain(new NpcId("keeper"));
        npc.WorldLocationId.Should().Be(new LocationId("harbor"));
        npc.Goals.Should().ContainSingle().Which.Should().Be("Protect the lighthouse");
        npc.Relationships.Should().ContainSingle().Which.Should().Be("Trusts the harbor master");
        npc.KnowledgeBoundary.Should().Contain("harbor-secret");
    }

    [Fact]
    public void AddGeneratedLocation_PreservesTypeAndEnvironmentFromWorldLocation()
    {
        var game = CreateGame();
        var regionId = new RegionId("generated-region");
        var environment = new EnvironmentalProperties(TerrainKind.Marsh, "humid", "foggy", ["reeds"]);
        var generated = new WorldLocation(new LocationId("reed-cavern"), regionId, LocationType.Cavern, "reed-cavern", "A cavern in the marsh.", environment);
        var connection = new WorldConnection(
            new ConnectionId("dark-forest:North"),
            AdventureWorldFactory.DarkForest,
            generated.Id,
            ConnectionDirection.North,
            ConnectionVisibility.Discovered);

        game.AddGeneratedLocation(generated, connection);
        var reloaded = Game.FromSnapshot(game.ToSnapshot()).GetLocation(generated.Id);

        reloaded.Type.Should().Be(LocationType.Cavern);
        reloaded.Environment.Terrain.Should().Be(TerrainKind.Marsh);
        reloaded.Environment.Features.Should().Equal("reeds");
    }

    [Fact]
    public void CreateNewGameFromJson_RejectsConflictingNpcLocationDeclarations()
    {
        const string json = """
                {
                    "id": "npc-placement",
                    "title": "NPC Placement",
                    "startingLocationId": "harbor",
                    "locations": [
                        { "id": "harbor", "npcIds": ["keeper"] },
                        { "id": "tower" }
                    ],
                    "items": [],
                    "npcs": [{ "id": "keeper", "name": "Keeper", "personality": "Careful", "knowledgeBoundary": [], "locationId": "tower" }],
                    "puzzle": { "id": "gate", "requiredItemId": "key", "requiredClueKey": "" }
                }
                """;

        FluentActions.Invoking(() => AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UnixEpoch, json))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*authoritative location*");
    }
}
