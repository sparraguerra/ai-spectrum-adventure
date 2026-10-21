namespace AI.SpectrumAdventure.Agents.Intent;

using System.Text.RegularExpressions;
using AI.SpectrumAdventure.Contracts;

internal static class IntentActionVocabulary
{
    private static readonly IReadOnlyDictionary<IntentAction, IReadOnlyList<string>> SynonymsByAction =
        new Dictionary<IntentAction, IReadOnlyList<string>>
        {
            [IntentAction.Look] = Array.AsReadOnly(new[] { "look", "look around", "l" }),
            [IntentAction.Examine] = Array.AsReadOnly(new[] { "examine", "look at", "inspect", "x", "check", "check out", "study" }),
            [IntentAction.Go] = Array.AsReadOnly(new[] { "go", "walk", "travel", "head", "move", "run" }),
            [IntentAction.Take] = Array.AsReadOnly(new[] { "take", "grab", "pick", "pick up", "get", "collect" }),
            [IntentAction.Drop] = Array.AsReadOnly(new[] { "drop", "leave", "put down" }),
            [IntentAction.Open] = Array.AsReadOnly(new[] { "open", "pry open", "force open" }),
            [IntentAction.Use] = Array.AsReadOnly(new[] { "use", "unlock", "insert", "apply" }),
            [IntentAction.TalkTo] = Array.AsReadOnly(new[] { "talk to", "talk", "speak to", "speak with", "speak", "ask", "converse", "chat" }),
            [IntentAction.Back] = Array.AsReadOnly(new[] { "back", "return", "go back", "go to previous location" }),
        };

    public static IReadOnlyDictionary<IntentAction, IReadOnlyList<string>> All => SynonymsByAction;

    public static IReadOnlyList<string> For(IntentAction action) =>
        SynonymsByAction.TryGetValue(action, out var synonyms) ? synonyms : [];

    public static string ToRegexAlternation(IntentAction action) =>
        string.Join("|", For(action)
            .OrderByDescending(synonym => synonym.Length)
            .Select(Regex.Escape));
}
