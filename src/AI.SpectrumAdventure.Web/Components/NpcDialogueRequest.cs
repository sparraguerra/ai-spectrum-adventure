namespace AI.SpectrumAdventure.Web.Components;

using AI.SpectrumAdventure.Domain.Common;

public sealed record NpcDialogueRequest(NpcId NpcId, string Utterance);
