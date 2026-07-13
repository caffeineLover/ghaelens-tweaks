using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace GhaelenTweaks;

public sealed class EntityBehaviorCatLoreGuardian(Entity behaviorEntity) : EntityBehavior(behaviorEntity)
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

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);

		originalGlowLevel = entity.Properties.Client?.GlowLevel ?? 0;
		originalLightHsv = entity.LightHsv;
	}



	public override string PropertyName()
	{
		return "catloreguardian";
	}



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
		if (entity.World.Side == EnumAppSide.Client)
		{
			float glowRadius = Math.Max(0, config.GetRadiusForLoreGlow(isUnderground));
			SetGlow(GetLoreProximity(glowRadius));
		}

		if (entity.World.Side == EnumAppSide.Server)
		{
			float yowlRadius = Math.Max(0, config.GetRadiusForLoreYowl(isUnderground));
			bool loreIsInYowlRange = yowlRadius > 0 && HasNearbyLoreCreature(yowlRadius);
			if (loreIsInYowlRange && !loreWasInYowlRange)
			{
				entity.World.PlaySoundAt(YowlSound, entity.Pos.X, entity.Pos.Y, entity.Pos.Z, null, true, 16f, 1f);
			}

			loreWasInYowlRange = loreIsInYowlRange;
		}
	}



	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		base.OnEntityReceiveDamage(damageSource, ref damage);

		if (!GhaelenTweaksConfig.Current.CatImperviousToLoreCreatures)
		{
			return;
		}

		if (damageSource.Source != EnumDamageSource.Fall
			&& !IsLoreCreature(damageSource.GetCauseEntity())
			&& !IsLoreCreature(damageSource.SourceEntity))
		{
			return;
		}

		damage = 0;
		damageSource.CauseEntity = null;
		damageSource.SourceEntity = null;
	}



	private bool HasNearbyLoreCreature(float radius)
	{
		return GetNearestLoreCreature(radius) != null;
	}



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



	private Entity? GetNearestLoreCreature(float radius)
	{
		return entity.World.GetNearestEntity(
			entity.Pos.XYZ,
			radius,
			radius,
			candidate => candidate != entity && candidate.Alive && GhaelenTweaksEntityPredicates.IsLoreCreature(candidate));
	}



	private bool IsUnderground()
	{
		return entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight) <= 0;
	}



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



	private static float Clamp01(float value)
	{
		if (value < 0)
		{
			return 0;
		}

		return value > 1 ? 1 : value;
	}



	private static bool IsLoreCreature(Entity? candidate)
	{
		return GhaelenTweaksEntityPredicates.IsLoreCreature(candidate);
	}
}
