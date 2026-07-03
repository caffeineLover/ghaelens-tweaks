using System;
using Vintagestory.API.Common.Entities;

namespace GhaelenTweaks;

internal static class GhaelenTweaksEntityPredicates
{
	public static bool IsLoreCreature(Entity? candidate)
	{
		string? path = candidate?.Code?.Path ?? candidate?.Properties?.Code?.Path;
		if (path == null || path.Contains("-hacked", StringComparison.Ordinal))
		{
			return false;
		}

		return path.StartsWith("bell", StringComparison.Ordinal)
			|| path.StartsWith("bowtorn", StringComparison.Ordinal)
			|| path.StartsWith("drifter", StringComparison.Ordinal)
			|| path.StartsWith("eidolon", StringComparison.Ordinal)
			|| path.StartsWith("erel", StringComparison.Ordinal)
			|| path.StartsWith("locust", StringComparison.Ordinal)
			|| path.StartsWith("mechhelper", StringComparison.Ordinal)
			|| path.StartsWith("shiver", StringComparison.Ordinal);
	}

	public static bool IsHostileMundaneAdultPredator(Entity? candidate)
	{
		string? path = candidate?.Code?.Path ?? candidate?.Properties?.Code?.Path;
		if (path == null || !path.Contains("-adult-", StringComparison.Ordinal))
		{
			return false;
		}

		return path.StartsWith("bear-", StringComparison.Ordinal)
			|| path.StartsWith("wolf-", StringComparison.Ordinal)
			|| path.StartsWith("hyena-", StringComparison.Ordinal);
	}
}
