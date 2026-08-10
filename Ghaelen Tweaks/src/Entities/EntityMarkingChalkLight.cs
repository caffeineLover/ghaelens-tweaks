/*
 * Provides invisible dynamic-light anchors for temporal marking chalk decor.
 *
 * Vintage Story decor is stored on block faces and can render glow, but decor
 * placement does not behave like placing a normal light-emitting block.  This
 * entity is spawned next to temporal chalk decor so the client receives a
 * persistent configurable colored dynamic light tied to the chalk mark's
 * lifetime.
 */

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class EntityMarkingChalkLight : Entity
{
	internal const string EntityCodePath = "markingchalklight";
	private const string TargetXAttribute = "markingChalkLightTargetX";
	private const string TargetYAttribute = "markingChalkLightTargetY";
	private const string TargetZAttribute = "markingChalkLightTargetZ";
	private const string DecorIndexAttribute = "markingChalkLightDecorIndex";
	private const string HueAttribute = "markingChalkLightHue";
	private const string SaturationAttribute = "markingChalkLightSaturation";
	private const string ValueAttribute = "markingChalkLightValue";
	private const float CleanupIntervalSeconds = 1f;
	private float cleanupSeconds;



	//// Gets or stores the dynamic-light HSV value synced to the client.
	////
	public override byte[] LightHsv
	{
		get
		{
			return new[]
			{
				(byte)GameMath.Clamp(WatchedAttributes.GetInt(HueAttribute, 0), 0, 255),
				(byte)GameMath.Clamp(WatchedAttributes.GetInt(SaturationAttribute, 0), 0, 255),
				(byte)GameMath.Clamp(
					WatchedAttributes.GetInt(ValueAttribute, GhaelenTweaksConfig.Current.TemporalMarkingChalkLightLevel),
					0,
					32)
			};
		}
		set
		{
			if (value == null || value.Length < 3)
			{
				return;
			}

			WatchedAttributes.SetInt(HueAttribute, value[0]);
			WatchedAttributes.SetInt(SaturationAttribute, value[1]);
			WatchedAttributes.SetInt(ValueAttribute, value[2]);
		}
	}



	//// Prevents the invisible light marker from being selected or interacted
	//// with in normal play.
	////
	public override bool IsInteractable => false;



	//// Keeps the marker fixed at the chalk face instead of participating in
	//// entity gravity.
	////
	public override bool ApplyGravity => false;



	//// Prevents incidental entity damage from removing the light while the
	//// chalk decor still exists.
	////
	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		return false;
	}



	//// Stores the block face decor this light marker belongs to.
	////
	internal void Configure(BlockPos targetPos, int decorIndex, byte[] lightHsv)
	{
		WatchedAttributes.SetInt(TargetXAttribute, targetPos.X);
		WatchedAttributes.SetInt(TargetYAttribute, targetPos.InternalY);
		WatchedAttributes.SetInt(TargetZAttribute, targetPos.Z);
		WatchedAttributes.SetInt(DecorIndexAttribute, decorIndex);
		LightHsv = lightHsv;
	}



	//// Checks whether this marker belongs to the supplied block decor entry.
	////
	internal bool Matches(BlockPos targetPos, int decorIndex)
	{
		return WatchedAttributes.GetInt(TargetXAttribute) == targetPos.X
		       && WatchedAttributes.GetInt(TargetYAttribute) == targetPos.InternalY
		       && WatchedAttributes.GetInt(TargetZAttribute) == targetPos.Z
		       && WatchedAttributes.GetInt(DecorIndexAttribute, -1) == decorIndex;
	}



	//// Periodically removes the marker if its temporal chalk decor no longer
	//// exists.
	////
	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (World.Side != EnumAppSide.Server || !Alive)
		{
			return;
		}

		cleanupSeconds += dt;
		if (cleanupSeconds < CleanupIntervalSeconds)
		{
			return;
		}

		cleanupSeconds = 0f;
		ApplyConfiguredLightLevel();

		if (!HasMatchingTemporalDecor())
		{
			Die(EnumDespawnReason.Removed);
		}
	}



	//// Refreshes persisted light markers when the configured light level
	//// changes after the marker was spawned.
	////
	private void ApplyConfiguredLightLevel()
	{
		int configuredLightLevel = GameMath.Clamp(GhaelenTweaksConfig.Current.TemporalMarkingChalkLightLevel, 0, 32);
		if (WatchedAttributes.GetInt(ValueAttribute, -1) == configuredLightLevel)
		{
			return;
		}

		WatchedAttributes.SetInt(ValueAttribute, configuredLightLevel);
	}



	//// Checks whether the referenced decor entry is still a temporal chalk
	//// overlay.
	////
	private bool HasMatchingTemporalDecor()
	{
		BlockPos targetPos = GetTargetPos();
		int decorIndex = WatchedAttributes.GetInt(DecorIndexAttribute, -1);
		if (decorIndex < 0)
		{
			return false;
		}

		Block decorBlock = World.BlockAccessor.GetDecor(targetPos, decorIndex);

		return decorBlock != null && ItemMarkingChalk.IsTemporalMarkingChalkDecor(decorBlock);
	}



	//// Recreates the stored block position for the decor lookup.
	////
	private BlockPos GetTargetPos()
	{
		return new BlockPos(
			WatchedAttributes.GetInt(TargetXAttribute),
			WatchedAttributes.GetInt(TargetYAttribute),
			WatchedAttributes.GetInt(TargetZAttribute));
	}



}
