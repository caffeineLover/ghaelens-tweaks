using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

internal sealed class PalisadeDamageSystem : IDisposable
{
	private const int ScanIntervalMilliseconds = 1000;
	private const float EntityScanHorizontalRange = 64f;
	private const float EntityScanVerticalRange = 16f;
	private const double ChargingPlayerRange = 20;
	private const double ClosePlayerRange = 6;
	private const double MinChargingMovement = 0.2;
	private const double MinPlayerDistanceClosed = 0.1;
	private const int RecentChargingMemoryTicks = 3;

	private readonly ICoreAPI api;
	private readonly Dictionary<long, PositionSnapshot> previousPositions = new();
	private readonly Dictionary<long, int> recentChargingPredators = new();
	private readonly Dictionary<long, int> lastDamagedEntities = new();
	private readonly long tickListenerId;
	private int scanSequence;

	public PalisadeDamageSystem(ICoreAPI api)
	{
		this.api = api;
		tickListenerId = api.Event.RegisterGameTickListener(OnServerTick, ScanIntervalMilliseconds);
	}

	public void Dispose()
	{
		api.Event.UnregisterGameTickListener(tickListenerId);
		previousPositions.Clear();
		recentChargingPredators.Clear();
		lastDamagedEntities.Clear();
	}

	private void OnServerTick(float deltaTime)
	{
		var config = GhaelenTweaksConfig.Current;
		if (!config.EnablePalisadeDamage || config.PalisadeDamageAmount <= 0)
		{
			previousPositions.Clear();
			recentChargingPredators.Clear();
			lastDamagedEntities.Clear();
			return;
		}

		IPlayer[] players = api.World.AllOnlinePlayers;
		if (players.Length == 0)
		{
			previousPositions.Clear();
			recentChargingPredators.Clear();
			lastDamagedEntities.Clear();
			return;
		}

		scanSequence++;
		var scannedEntityIds = new HashSet<long>();
		foreach (IPlayer player in players)
		{
			Entity? playerEntity = player.Entity;
			if (playerEntity?.Alive != true)
			{
				continue;
			}

			Entity[] nearbyEntities = api.World.GetEntitiesAround(
				playerEntity.Pos.XYZ,
				EntityScanHorizontalRange,
				EntityScanVerticalRange,
				IsPotentialPalisadeVictim);

			foreach (Entity entity in nearbyEntities)
			{
				if (!scannedEntityIds.Add(entity.EntityId))
				{
					continue;
				}

				ProcessEntity(entity, players, config.PalisadeDamageAmount, config.PalisadeDamageCooldownSeconds);
			}
		}

		PrunePositionCache(scannedEntityIds);
	}

	private void ProcessEntity(Entity entity, IPlayer[] players, float damage, float cooldownSeconds)
	{
		try
		{
			BlockPos? palisadePos = GetNearbyPalisadePos(entity);
			if (palisadePos != null
				&& ShouldDamageEntity(entity, players)
				&& IsDamageCooldownReady(entity, cooldownSeconds))
			{
				Block palisadeBlock = api.World.BlockAccessor.GetBlock(palisadePos);
				ReceivePalisadeDamage(entity, palisadeBlock, palisadePos, damage);
				lastDamagedEntities[entity.EntityId] = scanSequence;
			}
		}
		finally
		{
			previousPositions[entity.EntityId] = PositionSnapshot.From(entity);
		}
	}

	private static bool IsPotentialPalisadeVictim(Entity entity)
	{
		return entity.Alive
			&& (GhaelenTweaksEntityPredicates.IsLoreCreature(entity)
				|| GhaelenTweaksEntityPredicates.IsHostileMundaneAdultPredator(entity));
	}

	private bool ShouldDamageEntity(Entity entity, IPlayer[] players)
	{
		if (GhaelenTweaksEntityPredicates.IsLoreCreature(entity))
		{
			return true;
		}

		return GhaelenTweaksEntityPredicates.IsHostileMundaneAdultPredator(entity)
			&& IsChargingTowardPlayer(entity, players);
	}

	private bool IsChargingTowardPlayer(Entity entity, IPlayer[] players)
	{
		double currentDistance = double.MaxValue;
		IPlayer? nearestPlayer = null;

		foreach (IPlayer player in players)
		{
			Entity? playerEntity = player.Entity;
			if (playerEntity?.Alive != true)
			{
				continue;
			}

			double distance = HorizontalDistance(entity.Pos.X, entity.Pos.Z, playerEntity.Pos.X, playerEntity.Pos.Z);
			if (distance < currentDistance)
			{
				currentDistance = distance;
				nearestPlayer = player;
			}
		}

		if (nearestPlayer?.Entity == null || currentDistance > ChargingPlayerRange)
		{
			return false;
		}

		if (currentDistance <= ClosePlayerRange && WasRecentlyCharging(entity))
		{
			recentChargingPredators[entity.EntityId] = scanSequence;
			return true;
		}

		if (!previousPositions.TryGetValue(entity.EntityId, out PositionSnapshot previousPosition))
		{
			return false;
		}

		double movement = HorizontalDistance(previousPosition.X, previousPosition.Z, entity.Pos.X, entity.Pos.Z);
		if (movement < MinChargingMovement)
		{
			return false;
		}

		Entity nearestPlayerEntity = nearestPlayer.Entity;
		double previousDistance = HorizontalDistance(previousPosition.X, previousPosition.Z, nearestPlayerEntity.Pos.X, nearestPlayerEntity.Pos.Z);
		bool movedTowardPlayer = previousDistance - currentDistance >= MinPlayerDistanceClosed;
		if (movedTowardPlayer)
		{
			recentChargingPredators[entity.EntityId] = scanSequence;
		}

		return movedTowardPlayer;
	}

	private bool WasRecentlyCharging(Entity entity)
	{
		return recentChargingPredators.TryGetValue(entity.EntityId, out int lastChargingScan)
			&& scanSequence - lastChargingScan <= RecentChargingMemoryTicks;
	}

	private bool IsDamageCooldownReady(Entity entity, float cooldownSeconds)
	{
		if (!lastDamagedEntities.TryGetValue(entity.EntityId, out int lastDamagedScan))
		{
			return true;
		}

		int cooldownTicks = Math.Max(1, (int)Math.Ceiling(cooldownSeconds));
		return scanSequence - lastDamagedScan >= cooldownTicks;
	}

	private BlockPos? GetNearbyPalisadePos(Entity entity)
	{
		BlockPos entityPos = entity.Pos.AsBlockPos;
		return GetPalisadePosAtOrNear(entityPos) ?? GetPalisadePosAtOrNear(entityPos.DownCopy(1));
	}

	private BlockPos? GetPalisadePosAtOrNear(BlockPos center)
	{
		BlockPos? palisadePos = GetPalisadePos(center);
		if (palisadePos != null)
		{
			return palisadePos;
		}

		return GetPalisadePos(center.NorthCopy(1))
			?? GetPalisadePos(center.EastCopy(1))
			?? GetPalisadePos(center.SouthCopy(1))
			?? GetPalisadePos(center.WestCopy(1));
	}

	private BlockPos? GetPalisadePos(BlockPos pos)
	{
		Block block = api.World.BlockAccessor.GetBlock(pos);
		return IsPalisadeBlock(block) ? pos : null;
	}

	private static bool IsPalisadeBlock(Block block)
	{
		string? path = block.Code?.Path;
		return path != null
			&& (path.StartsWith("palisadewall-", StringComparison.Ordinal)
				|| path.StartsWith("palisadestakes-", StringComparison.Ordinal));
	}

	private static void ReceivePalisadeDamage(Entity entity, Block palisadeBlock, BlockPos palisadePos, float damage)
	{
		entity.ReceiveDamage(
			new DamageSource
			{
				Source = EnumDamageSource.Block,
				Type = EnumDamageType.PiercingAttack,
				SourceBlock = palisadeBlock,
				SourcePos = palisadePos.ToVec3d(),
				DamageTier = 0,
				KnockbackStrength = 0
			},
			damage);
	}

	private void PrunePositionCache(HashSet<long> scannedEntityIds)
	{
		var keysToRemove = new List<long>();
		foreach (long entityId in previousPositions.Keys)
		{
			if (!scannedEntityIds.Contains(entityId))
			{
				keysToRemove.Add(entityId);
			}
		}

		foreach (long entityId in keysToRemove)
		{
			previousPositions.Remove(entityId);
			recentChargingPredators.Remove(entityId);
			lastDamagedEntities.Remove(entityId);
		}
	}

	private static double HorizontalDistance(double x1, double z1, double x2, double z2)
	{
		double dx = x1 - x2;
		double dz = z1 - z2;
		return Math.Sqrt((dx * dx) + (dz * dz));
	}

	private readonly record struct PositionSnapshot(double X, double Z)
	{
		public static PositionSnapshot From(Entity entity)
		{
			return new PositionSnapshot(entity.Pos.X, entity.Pos.Z);
		}
	}
}
