namespace AI.SpectrumAdventure.Contracts;

public sealed record LoreDiscoveryResult(string LoreId, string ContentKey, string Content, string Category, string Scope, string TruthClassification, bool IsNewDiscovery, bool IsUncertain);
