namespace AI.SpectrumAdventure.Agents.Intent;

using System.Text.RegularExpressions;
using AI.SpectrumAdventure.Contracts;

/// <summary>Deterministic, zero-LLM-cost recognizer for classic adventure-style commands and common synonyms
/// (research.md Decision 3). Returns null when no pattern matches, signaling the caller to fall back.</summary>
public static partial class CommandPatternInterpreter
{
    public static ParsedIntent? TryInterpret(string rawInput)
    {
        var trimmed = rawInput.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (LookPattern().IsMatch(trimmed))
        {
            return Intent(IntentAction.Look, null, rawInput);
        }

        var goMatch = GoPattern().Match(trimmed);
        if (goMatch.Success && WorldVocabulary.TryResolveDirection(goMatch.Groups["dir"].Value, out var direction))
        {
            return Intent(IntentAction.Go, direction, rawInput);
        }

        if (BareDirectionPattern().IsMatch(trimmed) && WorldVocabulary.TryResolveDirection(trimmed, out var bareDirection))
        {
            return Intent(IntentAction.Go, bareDirection, rawInput);
        }

        var examineMatch = ExaminePattern().Match(trimmed);
        if (examineMatch.Success && WorldVocabulary.TryResolveTarget(examineMatch.Groups["target"].Value, out var examineTarget))
        {
            return Intent(IntentAction.Examine, examineTarget, rawInput);
        }

        var takeMatch = TakePattern().Match(trimmed);
        if (takeMatch.Success && WorldVocabulary.TryResolveTarget(takeMatch.Groups["target"].Value, out var takeTarget))
        {
            return Intent(IntentAction.Take, takeTarget, rawInput);
        }

        var openMatch = OpenPattern().Match(trimmed);
        if (openMatch.Success && WorldVocabulary.TryResolveTarget(openMatch.Groups["target"].Value, out var openTarget))
        {
            return Intent(IntentAction.Open, openTarget, rawInput);
        }

        var talkMatch = TalkPattern().Match(trimmed);
        if (talkMatch.Success && WorldVocabulary.TryResolveTarget(talkMatch.Groups["target"].Value, out var talkTarget))
        {
            return Intent(IntentAction.TalkTo, talkTarget, rawInput);
        }

        var useMatch = UsePattern().Match(trimmed);
        if (useMatch.Success && WorldVocabulary.TryResolveTarget(useMatch.Groups["item"].Value, out var useItem))
        {
            var parameters = new Dictionary<string, string>();
            var onGroup = useMatch.Groups["ontarget"];
            if (onGroup.Success && WorldVocabulary.TryResolveTarget(onGroup.Value, out var onTarget))
            {
                parameters["on"] = onTarget;
            }

            return Intent(IntentAction.Use, useItem, rawInput, parameters);
        }

        return null;
    }

    private static ParsedIntent Intent(IntentAction action, string? target, string rawInput, Dictionary<string, string>? parameters = null) =>
        new(action, target, parameters ?? new Dictionary<string, string>(), Confidence: 1.0, rawInput);

    [GeneratedRegex(@"^(look|look\s+around|l)$", RegexOptions.IgnoreCase)]
    private static partial Regex LookPattern();

    [GeneratedRegex(@"^(go|walk|travel|head)\s+(?<dir>north|south|east|west|n|s|e|w)$", RegexOptions.IgnoreCase)]
    private static partial Regex GoPattern();

    [GeneratedRegex(@"^(north|south|east|west|n|s|e|w)$", RegexOptions.IgnoreCase)]
    private static partial Regex BareDirectionPattern();

    [GeneratedRegex(@"^(examine|look\s+at|inspect|x)\s+(?<target>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExaminePattern();

    [GeneratedRegex(@"^(take|grab|pick\s+up|get)\s+(?<target>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TakePattern();

    [GeneratedRegex(@"^(open)\s+(?<target>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex OpenPattern();

    [GeneratedRegex(@"^(talk\s+to|talk|speak\s+to|speak\s+with)\s+(?<target>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TalkPattern();

    [GeneratedRegex(@"^(use)\s+(?<item>[^,]+?)(\s+on\s+(?<ontarget>.+))?$", RegexOptions.IgnoreCase)]
    private static partial Regex UsePattern();
}
