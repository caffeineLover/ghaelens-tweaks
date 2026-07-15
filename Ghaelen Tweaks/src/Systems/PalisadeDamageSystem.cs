/*
 * Implements the server-side palisade damage system.
 *
 * The main mod system creates this service only on the server. It registers a
 * periodic tick listener that scans near online players, identifies lore
 * creatures and charging adult predators near palisade blocks, applies
 * piercing block damage, and maintains short-lived per-entity movement and
 * cooldown state between scans.
 */

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



	//// Creates the palisade damage system and registers its server tick
	//// listener.
	////
	//// The owning mod system constructs this service only on the server side.
	//// The returned listener id is retained so Dispose can unregister the
	//// periodic callback during mod unloading.
	////
	public PalisadeDamageSystem(ICoreAPI api)
	{
		this.api = api;
		tickListenerId = api.Event.RegisterGameTickListener(OnServerTick, ScanIntervalMilliseconds);
	}



	//// Unregisters the server tick listener and clears all per-entity scan
	//// state owned by this service.
	////
	//// The main mod system calls this from Dispose. Clearing the dictionaries
	//// prevents stale movement and cooldown state from surviving a mod-system
	//// lifetime boundary.
	////
	public void Dispose()
	{
		api.Event.UnregisterGameTickListener(tickListenerId);
		previousPositions.Clear();
		recentChargingPredators.Clear();
		lastDamagedEntities.Clear();
	}



	//// Performs one server-side palisade damage scan.
	////
	//// Vintage Story invokes this through the registered game tick listener.
	//// The scan is centered around online players so the system avoids a
	//// world-wide entity search while still covering threats near active
	//// gameplay.
	////
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

		// Scan around each online player, but process every entity id only
		// once so overlapping player search areas cannot double-damage a
		// creature during the same scan.
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



	//// Evaluates one potential palisade victim and applies damage when all
	//// gameplay requirements are satisfied.
	////
	//// The previous-position snapshot is updated in a finally block so
	//// movement tracking remains fresh even if a particular entity cannot be
	//// damaged during this scan.
	////
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



	//// Filters entity searches down to creatures that may interact with
	//// palisade damage.
	////
	//// Lore creatures are always eligible for later damage checks. Mundane
	//// predators still need charging behavior validation before damage is
	//// applied.
	////
	private static bool IsPotentialPalisadeVictim(Entity entity)
	{
		return entity.Alive
			&& (GhaelenTweaksEntityPredicates.IsLoreCreature(entity)
				|| GhaelenTweaksEntityPredicates.IsHostileMundaneAdultPredator(entity));
	}



	//// Determines whether the creature type and current behavior justify
	//// palisade damage.
	////
	//// Lore creatures are damaged whenever they contact or stand beside
	//// palisades. Mundane adult predators are damaged only when movement
	//// history indicates a charge toward a player.
	////
	private bool ShouldDamageEntity(Entity entity, IPlayer[] players)
	{
		if (GhaelenTweaksEntityPredicates.IsLoreCreature(entity))
		{
			return true;
		}

		return GhaelenTweaksEntityPredicates.IsHostileMundaneAdultPredator(entity)
			&& IsChargingTowardPlayer(entity, players);
	}



	//// Determines whether a mundane predator is actively charging toward the
	//// nearest online player.
	////
	//// The system compares the current horizontal distance to the previous
	//// scan's horizontal distance, then remembers recent charging state for a
	//// few ticks so close-range contact remains dangerous even if movement
	//// becomes too small to measure cleanly.
	////
	private bool IsChargingTowardPlayer(Entity entity, IPlayer[] players)
	{
		double currentDistance = double.MaxValue;
		IPlayer? nearestPlayer = null;

		// Find the closest living player horizontally because palisade contact
		// is a ground-level defensive mechanic and vertical distance should
		// not dominate the charge test.
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
		double previousDistance = HorizontalDistance(
			previousPosition.X,
			previousPosition.Z,
			nearestPlayerEntity.Pos.X,
			nearestPlayerEntity.Pos.Z);
		bool movedTowardPlayer = previousDistance - currentDistance >= MinPlayerDistanceClosed;

		if (movedTowardPlayer)
		{
			recentChargingPredators[entity.EntityId] = scanSequence;
		}

		return movedTowardPlayer;
	}



	//// Checks whether a predator was classified as charging during a recent
	//// scan.
	////
	//// This short memory smooths the transition from approach movement to
	//// close contact, where a blocked predator may stop moving even though it
	//// is still pressing against a palisade near the player.
	////
	private bool WasRecentlyCharging(Entity entity)
	{
		return recentChargingPredators.TryGetValue(entity.EntityId, out int lastChargingScan)
			&& scanSequence - lastChargingScan <= RecentChargingMemoryTicks;
	}



	//// Determines whether an entity's per-creature palisade damage cooldown
	//// has elapsed.
	////
	//// The scan runs once per second, so configured seconds map directly to
	//// scan counts after rounding up to at least one tick.
	////
	private bool IsDamageCooldownReady(Entity entity, float cooldownSeconds)
	{
		if (!lastDamagedEntities.TryGetValue(entity.EntityId, out int lastDamagedScan))
		{
			return true;
		}

		int cooldownTicks = Math.Max(1, (int)Math.Ceiling(cooldownSeconds));
		return scanSequence - lastDamagedScan >= cooldownTicks;
	}



	//// Finds a palisade at the entity's feet or directly beneath it.
	////
	//// Checking the block below catches creatures standing on palisade stakes
	//// or wall collision shapes whose effective contact point is below the
	//// entity's current block position.
	////
	private BlockPos? GetNearbyPalisadePos(Entity entity)
	{
		BlockPos entityPos = entity.Pos.AsBlockPos;
		return GetPalisadePosAtOrNear(entityPos) ?? GetPalisadePosAtOrNear(entityPos.DownCopy(1));
	}



	//// Finds a palisade at a center position or one of its horizontal
	//// neighbors.
	////
	//// The horizontal-neighbor check lets palisade walls hurt creatures that
	//// are pressing alongside the defensive line rather than occupying the
	//// exact same block position.
	////
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



	//// Returns the supplied position when it contains a palisade block.
	////
	//// This helper keeps the world block lookup in one place for the nearby
	//// position search routines.
	////
	private BlockPos? GetPalisadePos(BlockPos pos)
	{
		Block block = api.World.BlockAccessor.GetBlock(pos);
		return IsPalisadeBlock(block) ? pos : null;
	}



	//// Identifies palisade wall and stake block families by code path.
	////
	//// Prefix matching covers the vanilla shape and material variants without
	//// needing to list every individual palisade block code.
	////
	private static bool IsPalisadeBlock(Block block)
	{
		string? path = block.Code?.Path;
		return path != null
			&& (path.StartsWith("palisadewall-", StringComparison.Ordinal)
				|| path.StartsWith("palisadestakes-", StringComparison.Ordinal));
	}



	//// Applies block-sourced piercing damage from the palisade to the target
	//// entity.
	////
	//// The damage source points at the palisade block and position so Vintage
	//// Story and other mods can attribute the hit to environmental block
	//// contact rather than an attacking player or creature.
	////
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



	//// Removes cached movement and cooldown state for entities that were not
	//// encountered during the current scan.
	////
	//// The scan is player-centered, so entities naturally leave the active
	//// area. Pruning prevents stale dictionary entries from accumulating and
	//// ensures old movement history is not reused if an entity later returns.
	////
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



	//// Calculates horizontal distance in the X/Z plane.
	////
	//// Palisade contact and predator approach are ground-plane mechanics, so
	//// the charging heuristics intentionally ignore vertical displacement.
	////
	private static double HorizontalDistance(double x1, double z1, double x2, double z2)
	{
		double dx = x1 - x2;
		double dz = z1 - z2;
		return Math.Sqrt((dx * dx) + (dz * dz));
	}



	private readonly struct PositionSnapshot
	{
		//// Gets the stored X coordinate from the entity position snapshot.
		////
		public double X { get; }

		//// Gets the stored Z coordinate from the entity position snapshot.
		////
		public double Z { get; }



		//// Records an entity's horizontal position for movement comparison
		//// during a later scan.
		////
		//// The palisade system needs only X/Z movement, so Y is deliberately
		//// excluded from the snapshot.
		////
		public PositionSnapshot(double x, double z)
		{
			X = x;
			Z = z;
		}



		//// Captures the current horizontal position from a Vintage Story
		//// entity.
		////
		//// The server tick scan calls this after processing each candidate so
		//// the next scan can compare movement toward the nearest player.
		////
		public static PositionSnapshot From(Entity entity)
		{
			return new PositionSnapshot(entity.Pos.X, entity.Pos.Z);
		}
	}
}
