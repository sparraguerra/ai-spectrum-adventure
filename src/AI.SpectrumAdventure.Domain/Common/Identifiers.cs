namespace AI.SpectrumAdventure.Domain.Common;

public readonly record struct GameId(Guid Value)
{
    public static GameId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public readonly record struct LocationId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ItemId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct NpcId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct PuzzleId(string Value)
{
    public override string ToString() => Value;
}
