/*
 * Implements the Cats-mod entity behavior that turns cats into lore-creature
 * guardians.
 *
 * The behavior runs on both sides: the client side applies proximity-based
 * glow and light values, while the server side detects newly nearby lore
 * creatures and plays the warning yowl. It does not change ordinary cat
 * damage handling.
 */

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace GhaelenTweaks;

public sealed class EntityBehaviorCatLoreGuardian : EntityBehavior
{
	private const float ScanIntervalSeconds = 0.25f;
	private const int MinLoreGlowLevel = 25;
	private const int MaxLoreGlowLevel = 220;
	private const byte LoreLightHue = 21;
	private const byte LoreLightSaturation = 7;
	private const byte MinLoreLightValue = 2;
	private const byte MaxLoreLightValue = 12;

	private static readonly AssetLocation YowlSound = new("ghaelentweaks", "sounds/creature/cat/angry-cat");

	private float scanAccumulator;
	private bool loreWasInYowlRange;
	private bool glowApplied;
	private int originalGlowLevel;
	private int lastAppliedGlowLevel = -1;
	private byte lastAppliedLightValue;
	private byte[]? originalLightHsv;



	//// Creates the cat lore guardian behavior for a cat entity.
	////
	//// Vintage Story constructs entity behaviors from JSON-declared behavior
	//// registrations. The base EntityBehavior keeps the entity reference used
	//// by later tick, damage, and light-manipulation callbacks.
	////
	public EntityBehaviorCatLoreGuardian(Entity behaviorEntity)
		: base(behaviorEntity)
	{
	}



	//// Captures the cat's original client glow state when Vintage Story
	//// initializes the behavior.
	////
	//// The behavior restores these values whenever no lore creature is close
	//// enough to justify the warning glow, so it must record them before any
	//// proximity update changes the entity properties.
	////
	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);

		originalGlowLevel = entity.Properties.Client?.GlowLevel ?? 0;
		originalLightHsv = entity.LightHsv;
	}



	//// Returns the behavior name used by Vintage Story's entity behavior
	//// registration and asset data.
	////
	//// The main mod system registers this exact name during startup, and cat
	//// entity assets use it to attach the behavior.
	////
	public override string PropertyName()
	{
		return "catloreguardian";
	}



	//// Performs periodic lore-creature scans for warning glow and yowl
	//// behavior.
	////
	//// Vintage Story invokes this every entity tick. The behavior accumulates
	//// time and scans four times per second to avoid running nearby-entity
	//// searches every frame while still feeling responsive to players.
	////
	public override void OnGameTick(float deltaTime)
	{
		base.OnGameTick(deltaTime);

		scanAccumulator += deltaTime;
		if (scanAccumulator < ScanIntervalSeconds)
		{
			return;
		}

		scanAccumulator = 0;

		var config = GhaelenTweaksConfig.Current;
		bool isUnderground = IsUnderground();

		// Glow is visual state, so it is applied only on the client side.
		// The server-side sound logic below remains independent because it
		// owns authoritative world audio events.
		if (entity.World.Side == EnumAppSide.Client)
		{
			float glowRadius = Math.Max(0, config.GetRadiusForLoreGlow(isUnderground));
			SetGlow(GetLoreProximity(glowRadius));
		}

		if (entity.World.Side == EnumAppSide.Server)
		{
			float yowlRadius = Math.Max(0, config.GetRadiusForLoreYowl(isUnderground));
			bool loreIsInYowlRange = yowlRadius > 0 && HasNearbyLoreCreature(yowlRadius);

			// Yowl only on the transition into danger range so a nearby lore
			// creature produces a warning cue instead of a repeating sound
			// every scan interval.
			if (loreIsInYowlRange && !loreWasInYowlRange)
			{
				entity.World.PlaySoundAt(YowlSound, entity.Pos.X, entity.Pos.Y, entity.Pos.Z, null, true, 16f, 1f);
			}

			loreWasInYowlRange = loreIsInYowlRange;
		}
	}



	//// Checks whether at least one lore creature is close enough for a yowl
	//// warning.
	////
	//// The server-side tick path uses this lightweight wrapper when it only
	//// needs presence rather than distance-based proximity.
	////
	private bool HasNearbyLoreCreature(float radius)
	{
		return GetNearestLoreCreature(radius) != null;
	}



	//// Calculates normalized lore-creature proximity for client glow
	//// intensity.
	////
	//// A null result means no glow should be applied. Otherwise the result is
	//// in the range zero to one, where one means the nearest lore creature is
	//// at the cat's position and zero means it is at the configured radius.
	////
	private float? GetLoreProximity(float radius)
	{
		if (radius <= 0)
		{
			return null;
		}

		Entity? nearestLoreCreature = GetNearestLoreCreature(radius);
		if (nearestLoreCreature == null)
		{
			return null;
		}

		double distance = Math.Sqrt(entity.Pos.SquareDistanceTo(nearestLoreCreature.Pos));
		return 1 - Clamp01((float)(distance / radius));
	}



	//// Searches for the nearest valid lore creature around the cat.
	////
	//// Both glow and yowl checks use this method so the definition of a
	//// detectable lore creature remains consistent across client and server
	//// behavior. The cat itself is excluded to guard against malformed asset
	//// data or future code-path overlap.
	////
	private Entity? GetNearestLoreCreature(float radius)
	{
		return entity.World.GetNearestEntity(
			entity.Pos.XYZ,
			radius,
			radius,
			candidate => candidate != entity && candidate.Alive && GhaelenTweaksEntityPredicates.IsLoreCreature(candidate));
	}



	//// Determines whether the cat is in a fully underground position for
	//// configuration selection.
	////
	//// The behavior uses only sunlight because the gameplay distinction is
	//// exposure to the sky, not whether local torches or glowing blocks make
	//// the area bright.
	////
	private bool IsUnderground()
	{
		return entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight) <= 0;
	}



	//// Applies or restores lore-warning glow based on normalized proximity.
	////
	//// The client-side tick path calls this after calculating proximity. The
	//// method avoids rewriting entity light state when the computed values
	//// match the most recently applied values.
	////
	private void SetGlow(float? proximity)
	{
		if (proximity == null)
		{
			RestoreGlow();
			return;
		}

		float clampedProximity = Clamp01(proximity.Value);
		int glowLevel = Math.Max(
			originalGlowLevel,
			(int)Math.Round(MinLoreGlowLevel + ((MaxLoreGlowLevel - MinLoreGlowLevel) * clampedProximity)));
		byte lightValue = (byte)Math.Round(MinLoreLightValue + ((MaxLoreLightValue - MinLoreLightValue) * clampedProximity));

		if (glowApplied && glowLevel == lastAppliedGlowLevel && lightValue == lastAppliedLightValue)
		{
			return;
		}

		glowApplied = true;
		lastAppliedGlowLevel = glowLevel;
		lastAppliedLightValue = lightValue;

		if (entity.Properties.Client != null)
		{
			entity.Properties.Client.GlowLevel = glowLevel;
		}

		entity.LightHsv = new[] { LoreLightHue, LoreLightSaturation, lightValue };
	}



	//// Restores the cat's original glow and light state after lore danger
	//// clears.
	////
	//// This method runs only after the behavior has applied a custom glow, so
	//// ordinary cats keep their original entity-defined visual state.
	////
	private void RestoreGlow()
	{
		if (!glowApplied)
		{
			return;
		}

		glowApplied = false;
		lastAppliedGlowLevel = -1;
		lastAppliedLightValue = 0;

		if (entity.Properties.Client != null)
		{
			entity.Properties.Client.GlowLevel = originalGlowLevel;
		}

		entity.LightHsv = originalLightHsv;
	}



	//// Clamps a floating-point value into the normalized zero-to-one range
	//// used by glow intensity calculations.
	////
	//// Keeping this helper local to the behavior avoids depending on a larger
	//// math utility for one small gameplay-specific normalization.
	////
	private static float Clamp01(float value)
	{
		if (value < 0)
		{
			return 0;
		}

		return value > 1 ? 1 : value;
	}



}
