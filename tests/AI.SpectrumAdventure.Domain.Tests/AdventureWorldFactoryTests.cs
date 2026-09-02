namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Puzzles;
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
}
