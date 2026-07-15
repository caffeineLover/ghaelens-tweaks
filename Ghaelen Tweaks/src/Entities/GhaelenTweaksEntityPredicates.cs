/*
 * Provides shared entity classification helpers for Ghaelen Tweaks gameplay
 * systems.
 *
 * Cat lore warnings and palisade damage both need consistent answers about
 * lore creatures and hostile mundane predators. Centralizing the code-path
 * checks avoids each system maintaining its own partial list and keeps hacked
 * friendly locusts excluded from hostile lore-creature behavior.
 */

using System;
using Vintagestory.API.Common.Entities;

namespace GhaelenTweaks;

internal static class GhaelenTweaksEntityPredicates
{



	//// Determines whether an entity belongs to the lore-creature families
	//// targeted by cat warnings and palisade damage.
	////
	//// The check uses the entity code path exposed by Vintage Story and
	//// deliberately excludes hacked variants so friendly clockmaker locusts
	//// are not treated as hostile lore creatures by this mod.
	////
	public static bool IsLoreCreature(Entity? candidate)
	{
		string? path = candidate?.Code?.Path ?? candidate?.Properties?.Code?.Path;
		if (path == null || path.Contains("-hacked", StringComparison.Ordinal))
		{
			return false;
		}

		// The prefix list matches creature families rather than individual
		// variants so new type suffixes remain covered by the same rule.
		return path.StartsWith("bell", StringComparison.Ordinal)
			|| path.StartsWith("bowtorn", StringComparison.Ordinal)
			|| path.StartsWith("drifter", StringComparison.Ordinal)
			|| path.StartsWith("eidolon", StringComparison.Ordinal)
			|| path.StartsWith("erel", StringComparison.Ordinal)
			|| path.StartsWith("locust", StringComparison.Ordinal)
			|| path.StartsWith("mechhelper", StringComparison.Ordinal)
			|| path.StartsWith("shiver", StringComparison.Ordinal);
	}



	//// Determines whether an entity is an adult mundane predator that can be
	//// damaged by palisades only while charging toward a player.
	////
	//// This helper intentionally excludes juveniles and non-predator animals
	//// so ordinary wildlife does not trigger the defensive palisade mechanic.
	////
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
