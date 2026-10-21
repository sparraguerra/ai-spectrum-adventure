namespace AI.SpectrumAdventure.Agents.Intent;

using System.Text.RegularExpressions;
using AI.SpectrumAdventure.Contracts;

public static class CommandPatternInterpreter
{
    private static readonly RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly Regex Look = ExactPattern(IntentAction.Look);
    private static readonly Regex Back = ExactPattern(IntentAction.Back);
    private static readonly Regex Go = new(
        $@"^(?:{IntentActionVocabulary.ToRegexAlternation(IntentAction.Go)})\s+(?<dir>north|south|east|west|up|down|n|s|e|w)$",
        PatternOptions);
    private static readonly Regex BareDirection = new(@"^(north|south|east|west|up|down|n|s|e|w)$", PatternOptions);
    private static readonly Regex Examine = TargetPattern(IntentAction.Examine, "(?<target>.+)");
    private static readonly Regex Take = TargetPattern(IntentAction.Take, "(?<target>.+)");
    private static readonly Regex Drop = TargetPattern(IntentAction.Drop, "(?<target>.+)");
    private static readonly Regex Open = TargetPattern(IntentAction.Open, "(?<target>.+)");
    private static readonly Regex Dialogue = TargetPattern(IntentAction.TalkTo, "(?<target>.+?)\\s+\"(?<utterance>.+)\"");
    private static readonly Regex Talk = TargetPattern(IntentAction.TalkTo, "(?<target>.+)");
    private static readonly Regex Use = new(
        $@"^(?:{IntentActionVocabulary.ToRegexAlternation(IntentAction.Use)})\s+(?<item>[^,]+?)(\s+on\s+(?<ontarget>.+))?$",
        PatternOptions);

    public static ParsedIntent? TryInterpret(string rawInput, WorldVocabulary? vocabulary = null)
    {
        var targetVocabulary = vocabulary ?? WorldVocabulary.Empty;
        var trimmed = rawInput.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (Look.IsMatch(trimmed))
        {
            return Intent(IntentAction.Look, null, rawInput);
        }

        if (Back.IsMatch(trimmed))
        {
            return Intent(IntentAction.Back, null, rawInput);
        }

        var goMatch = Go.Match(trimmed);
        if (goMatch.Success && WorldVocabulary.TryResolveDirection(goMatch.Groups["dir"].Value, out var direction))
        {
            return Intent(IntentAction.Go, direction, rawInput);
        }

        if (BareDirection.IsMatch(trimmed) && WorldVocabulary.TryResolveDirection(trimmed, out var bareDirection))
        {
            return Intent(IntentAction.Go, bareDirection, rawInput);
        }

        var examineMatch = Examine.Match(trimmed);
        if (examineMatch.Success && targetVocabulary.TryResolveTarget(examineMatch.Groups["target"].Value, out var examineTarget))
        {
            return Intent(IntentAction.Examine, examineTarget, rawInput);
        }

        var takeMatch = Take.Match(trimmed);
        if (takeMatch.Success && targetVocabulary.TryResolveTarget(takeMatch.Groups["target"].Value, out var takeTarget))
        {
            return Intent(IntentAction.Take, takeTarget, rawInput);
        }

        var dropMatch = Drop.Match(trimmed);
        if (dropMatch.Success && targetVocabulary.TryResolveTarget(dropMatch.Groups["target"].Value, out var dropTarget))
        {
            return Intent(IntentAction.Drop, dropTarget, rawInput);
        }

        var openMatch = Open.Match(trimmed);
        if (openMatch.Success && targetVocabulary.TryResolveTarget(openMatch.Groups["target"].Value, out var openTarget))
        {
            return Intent(IntentAction.Open, openTarget, rawInput);
        }

        var dialogueMatch = Dialogue.Match(trimmed);
        if (dialogueMatch.Success && targetVocabulary.TryResolveTarget(dialogueMatch.Groups["target"].Value, out var dialogueTarget))
        {
            return Intent(IntentAction.TalkTo, dialogueTarget, rawInput, new Dictionary<string, string>
            {
                ["utterance"] = dialogueMatch.Groups["utterance"].Value,
            });
        }

        var talkMatch = Talk.Match(trimmed);
        if (talkMatch.Success && targetVocabulary.TryResolveTarget(talkMatch.Groups["target"].Value, out var talkTarget))
        {
            return Intent(IntentAction.TalkTo, talkTarget, rawInput);
        }

        var useMatch = Use.Match(trimmed);
        if (useMatch.Success && targetVocabulary.TryResolveTarget(useMatch.Groups["item"].Value, out var useItem))
        {
            var parameters = new Dictionary<string, string>();
            var onGroup = useMatch.Groups["ontarget"];
            if (onGroup.Success && targetVocabulary.TryResolveTarget(onGroup.Value, out var onTarget))
            {
                parameters["on"] = onTarget;
            }

            return Intent(IntentAction.Use, useItem, rawInput, parameters);
        }

        return null;
    }

    private static ParsedIntent Intent(IntentAction action, string? target, string rawInput, Dictionary<string, string>? parameters = null) =>
        new(action, target, parameters ?? new Dictionary<string, string>(), Confidence: 1.0, rawInput);

    private static Regex ExactPattern(IntentAction action) =>
        new($@"^(?:{IntentActionVocabulary.ToRegexAlternation(action)})$", PatternOptions);

    private static Regex TargetPattern(IntentAction action, string targetPattern) =>
        new($@"^(?:{IntentActionVocabulary.ToRegexAlternation(action)})\s+{targetPattern}$", PatternOptions);
}
