namespace AI.SpectrumAdventure.Domain.Worlds;

using AI.SpectrumAdventure.Domain.Common;

public sealed class WorldLocation
{
    public LocationId Id { get; }
    public RegionId RegionId { get; }
    public LocationType Type { get; }
    public string StructuralNameKey { get; }
    public string BaseDescription { get; }
    public EnvironmentalProperties Environment { get; }
    public LocationState State { get; private set; }
    public int SceneVersion { get; private set; }

    public WorldLocation(LocationId id, RegionId regionId, LocationType type, string structuralNameKey, string baseDescription, EnvironmentalProperties environment, LocationState state = LocationState.Normal, int sceneVersion = 1)
    {
        if (string.IsNullOrWhiteSpace(structuralNameKey)) throw new ArgumentException("A structural name key is required.", nameof(structuralNameKey));
        if (string.IsNullOrWhiteSpace(baseDescription)) throw new ArgumentException("A base description is required.", nameof(baseDescription));
        if (sceneVersion < 1) throw new ArgumentOutOfRangeException(nameof(sceneVersion));

        Id = id;
        RegionId = regionId;
        Type = type;
        StructuralNameKey = structuralNameKey;
        BaseDescription = baseDescription;
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        State = state;
        SceneVersion = sceneVersion;
    }

    internal void ChangeState(LocationState state)
    {
        if (State != state)
        {
            State = state;
            SceneVersion++;
        }
    }
}