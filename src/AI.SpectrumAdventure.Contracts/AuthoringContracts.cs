namespace AI.SpectrumAdventure.Contracts;

using System.Text.Json;

public sealed record AdventureDraftRequest(string Id, string Title, string StartingLocationId, object Definition);

public sealed record AuthoringProposalContext(
	Guid DraftId,
	string AdventureIdentifier,
	string Title,
	string StartingLocationId,
	string DefinitionJson,
	string Request);

public sealed record AuthoringPatchOperation(string Operation, string Path, JsonElement? Value = null);

public sealed record AuthoringProposalPatch(IReadOnlyList<AuthoringPatchOperation> Operations);

public sealed record AuthoringProposalContract(Guid ProposalId, Guid DraftId, string ContentType, object Patch, string Status);

public sealed record AuthoringProposalValidation(bool IsValid, IReadOnlyList<string> Issues);

public static class AuthoringProposalContractValidator
{
	private static readonly HashSet<string> ContentTypes = ["location", "npc", "lore", "puzzle", "adventure"];
	private static readonly HashSet<string> Operations = ["add", "replace", "remove"];

	public static AuthoringProposalValidation Validate(AuthoringProposalContract proposal)
	{
		var issues = new List<string>();
		if (proposal.ProposalId == Guid.Empty) issues.Add("proposalId is required.");
		if (proposal.DraftId == Guid.Empty) issues.Add("draftId is required.");
		if (!ContentTypes.Contains(proposal.ContentType)) issues.Add("contentType is not supported.");
		if (!string.Equals(proposal.Status, "pending", StringComparison.OrdinalIgnoreCase)) issues.Add("AI proposals must be pending.");
		try
		{
			var patch = proposal.Patch is AuthoringProposalPatch typed ? typed : JsonSerializer.Deserialize<AuthoringProposalPatch>(JsonSerializer.Serialize(proposal.Patch));
			if (patch?.Operations is null || patch.Operations.Count == 0) issues.Add("patch.operations must contain at least one operation.");
			else foreach (var operation in patch.Operations)
			{
				if (!Operations.Contains(operation.Operation)) issues.Add($"Unsupported patch operation '{operation.Operation}'.");
				if (string.IsNullOrWhiteSpace(operation.Path) || !operation.Path.StartsWith('/')) issues.Add("Patch paths must be JSON pointers.");
				if (operation.Path is "/publish" or "/status" or "/persistence") issues.Add($"Patch path '{operation.Path}' is not authorable.");
				if (operation.Operation != "remove" && operation.Value is null) issues.Add($"Operation '{operation.Operation}' requires a value.");
			}
		}
		catch (JsonException) { issues.Add("patch does not match the structured proposal schema."); }
		return new AuthoringProposalValidation(issues.Count == 0, issues);
	}
}

public sealed record AuthoringValidationIssueContract(string Element, string Reason, bool Blocking);

public sealed record AuthoringValidationResultContract(bool IsValid, IReadOnlyList<AuthoringValidationIssueContract> Issues);

public sealed record PublishAdventureResult(bool Published, Guid? VersionId, long? VersionSequence, IReadOnlyList<string> Issues);

public sealed record PlaytestResult(bool Started, Guid? GameId, IReadOnlyList<string> Issues);