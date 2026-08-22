/*
 * Owns the server-authoritative parental-controls respawn delay and its
 * client countdown synchronization.
 *
 * Each player's counted deaths increase persistently on death and decay
 * lazily according to real death-free time, including time spent offline.
 * The first counted death is free; later deaths produce an incremental
 * delay.  The server stores compact JSON state in permanent player mod data,
 * and a one-way packet gives the client the current countdown and next delay.
 */

using System.Text;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GhaelenTweaks;

internal static class ParentalControlDeathDelaySystem
{
	private const string ChannelName = "ghaelentweaks-pc-death-delay";
	private const string ModDataKey = "ghaelentweaks:pc-death-delay-state";
	private static ICoreClientAPI? clientApi;
	private static ICoreServerAPI? serverApi;
	private static IServerNetworkChannel? serverChannel;
	private static PlayerDeathDelegate? playerDeathHandler;
	private static PlayerDelegate? playerNowPlayingHandler;
	private static long clientRespawnAvailableAtElapsedMilliseconds;
	private static long clientNextDeathDelaySeconds;
	private static bool clientHasScheduledNextDeathDelay;



	//// Registers the client receiver for the server-owned respawn deadline.
	////
	//// The packet carries remaining duration rather than a wall-clock timestamp
	//// so server and client clock differences cannot change the displayed or
	//// enforced delay.  It also carries the server-calculated next delay.
	////
	internal static void StartClientSide(ICoreClientAPI api)
	{
		clientApi = api;
		clientRespawnAvailableAtElapsedMilliseconds = 0;
		clientNextDeathDelaySeconds = 0;
		clientHasScheduledNextDeathDelay = false;

		api.Network
			.RegisterChannel(ChannelName)
			.RegisterMessageType<ParentalControlDeathDelayPacket>()
			.SetMessageHandler<ParentalControlDeathDelayPacket>(OnClientDelayPacket);
	}



	//// Registers the server channel and player lifecycle handlers.
	////
	//// PlayerDeath assigns and persists the new deadline.  PlayerNowPlaying
	//// resynchronizes a pending deadline after reconnecting or sends a clear
	//// packet when the player is already alive.
	////
	internal static void StartServerSide(ICoreServerAPI api)
	{
		serverApi = api;
		serverChannel = api.Network
			.RegisterChannel(ChannelName)
			.RegisterMessageType<ParentalControlDeathDelayPacket>();

		playerDeathHandler = OnPlayerDeath;
		playerNowPlayingHandler = SyncPlayerDelay;
		api.Event.PlayerDeath += playerDeathHandler;
		api.Event.PlayerNowPlaying += playerNowPlayingHandler;
	}



	//// Releases static player-event, network, and client countdown state.
	////
	//// Vintage Story can reload mods in the same process, so event handlers
	//// must not retain an earlier world or API instance.
	////
	internal static void Dispose()
	{
		if (serverApi != null)
		{
			if (playerDeathHandler != null)
			{
				serverApi.Event.PlayerDeath -= playerDeathHandler;
			}

			if (playerNowPlayingHandler != null)
			{
				serverApi.Event.PlayerNowPlaying -= playerNowPlayingHandler;
			}
		}

		playerDeathHandler = null;
		playerNowPlayingHandler = null;
		serverChannel = null;
		serverApi = null;
		clientApi = null;
		clientRespawnAvailableAtElapsedMilliseconds = 0;
		clientNextDeathDelaySeconds = 0;
		clientHasScheduledNextDeathDelay = false;
	}



	//// Determines whether the server may honor a player's respawn request.
	////
	//// The Harmony server prefix calls this before vanilla consumes a return
	//// point or starts teleporting.  An early request is rejected and the
	//// authoritative remaining duration is resynchronized to that client.
	////
	internal static bool IsServerRespawnAllowed(IServerPlayer player)
	{
		if (!GhaelenTweaksConfig.Current.PcUseDeathDelay)
		{
			ClearServerDelay(player);
			return true;
		}

		ParentalControlDeathDelayState state = ReadState(player);
		long nowMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		long remainingMilliseconds = state.RespawnAvailableAtUtcMilliseconds - nowMilliseconds;

		if (remainingMilliseconds <= 0)
		{
			if (state.RespawnAvailableAtUtcMilliseconds != 0)
			{
				state.RespawnAvailableAtUtcMilliseconds = 0;
				SaveState(player, state);
				SendDelayPacket(
					player,
					0,
					CalculateNextDeathDelaySeconds(state, nowMilliseconds));
			}

			return true;
		}

		SendDelayPacket(
			player,
			remainingMilliseconds,
			CalculateNextDeathDelaySeconds(state, nowMilliseconds));
		long remainingSeconds = DivideRoundingUp(remainingMilliseconds, 1000);
		player.SendLocalisedMessage(
			GlobalConstants.GeneralChatGroup,
			"ghaelentweaks:pc-death-delay-countdown",
			remainingSeconds);
		return false;
	}



	//// Returns the remaining client-side countdown duration in milliseconds.
	////
	//// The death-dialog patch uses this only for display and button state.
	//// The server independently enforces its persisted UTC deadline.
	////
	internal static long GetClientRemainingMilliseconds()
	{
		if (clientApi == null || !clientHasScheduledNextDeathDelay)
		{
			return 0;
		}

		return Math.Max(0, clientRespawnAvailableAtElapsedMilliseconds - clientApi.ElapsedMilliseconds);
	}



	//// Returns the server-calculated delay that one more death would receive.
	////
	//// A null result means no active death-dialog schedule should be shown.
	//// The client displays this value but never uses it for enforcement.
	////
	internal static long? GetClientNextDeathDelaySeconds()
	{
		return clientHasScheduledNextDeathDelay ? clientNextDeathDelaySeconds : null;
	}



	//// Reports whether the current client player is waiting on the death screen.
	////
	//// Keeping the API lookup in this system avoids requiring the reflective
	//// death-dialog patch to access the dialog's protected client API field.
	////
	internal static bool IsClientPlayerDead()
	{
		return clientApi?.World.Player?.Entity?.Alive == false;
	}



	//// Clears the local deadline after the client observes that it is alive.
	////
	internal static void ClearClientDelay()
	{
		clientRespawnAvailableAtElapsedMilliseconds = 0;
		clientNextDeathDelaySeconds = 0;
		clientHasScheduledNextDeathDelay = false;
	}



	//// Applies a runtime death-delay configuration change to connected players.
	////
	//// Disabling the feature immediately clears active deadlines without
	//// erasing counted-death history.  Numeric changes refresh the displayed
	//// next-death schedule but do not rewrite an already assigned deadline.
	////
	internal static void ApplyConfigChange()
	{
		if (!GhaelenTweaksConfig.Current.PcUseDeathDelay)
		{
			ClearClientDelay();
		}

		if (serverApi == null)
		{
			return;
		}

		foreach (IPlayer player in serverApi.World.AllOnlinePlayers)
		{
			if (player is not IServerPlayer serverPlayer)
			{
				continue;
			}

			if (GhaelenTweaksConfig.Current.PcUseDeathDelay)
			{
				SyncPlayerDelay(serverPlayer);
			}
			else
			{
				ClearServerDelay(serverPlayer);
			}
		}
	}



	//// Counts the death and persists the dying player's current respawn delay.
	////
	//// Expired deaths are removed before the new death is counted.  The first
	//// counted death is free, and each death beyond it contributes one
	//// configured delay increment.  The new death restarts the cooldown.
	////
	private static void OnPlayerDeath(IServerPlayer player, DamageSource damageSource)
	{
		if (!GhaelenTweaksConfig.Current.PcUseDeathDelay)
		{
			SendClearDelayPacket(player);
			return;
		}

		long nowMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		ParentalControlDeathDelayState state = ReadState(player);
		int deathCount = GetDecayedDeathCount(state, nowMilliseconds);

		if (deathCount < int.MaxValue)
		{
			deathCount++;
		}

		long delaySeconds = CalculateDelaySeconds(deathCount);
		state.DeathCount = deathCount;
		state.LastDeathAtUtcMilliseconds = nowMilliseconds;
		state.RespawnAvailableAtUtcMilliseconds = AddSaturating(
			nowMilliseconds,
			MultiplySaturating(delaySeconds, 1000));
		SaveState(player, state);
		SendDelayPacket(
			player,
			MultiplySaturating(delaySeconds, 1000),
			CalculateNextDeathDelaySeconds(state, nowMilliseconds));

		if (delaySeconds > 0)
		{
			player.SendLocalisedMessage(
				GlobalConstants.GeneralChatGroup,
				"ghaelentweaks:pc-death-delay-started",
				delaySeconds);
		}
	}



	//// Resynchronizes a player's pending delay after the client is ready.
	////
	//// Reconnecting does not bypass an active deadline.  Alive players and
	//// expired deadlines receive a zero packet so stale client state cannot
	//// disable a later death-dialog button.
	////
	private static void SyncPlayerDelay(IServerPlayer player)
	{
		if (!GhaelenTweaksConfig.Current.PcUseDeathDelay)
		{
			ClearServerDelay(player);
			return;
		}

		if (player.Entity?.Alive != false)
		{
			SendClearDelayPacket(player);
			return;
		}

		ParentalControlDeathDelayState state = ReadState(player);
		long nowMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		long remainingMilliseconds = Math.Max(
			0,
			state.RespawnAvailableAtUtcMilliseconds - nowMilliseconds);
		SendDelayPacket(
			player,
			remainingMilliseconds,
			CalculateNextDeathDelaySeconds(state, nowMilliseconds));
	}



	//// Clears one active server deadline while retaining escalation history.
	////
	//// The master switch uses this path so disabling the feature releases a
	//// currently dead player immediately but does not rewrite the historical
	//// count used if the feature is enabled again later.
	////
	private static void ClearServerDelay(IServerPlayer player)
	{
		ParentalControlDeathDelayState state = ReadState(player);
		if (state.RespawnAvailableAtUtcMilliseconds != 0)
		{
			state.RespawnAvailableAtUtcMilliseconds = 0;
			SaveState(player, state);
		}

		SendClearDelayPacket(player);
	}



	//// Updates the client monotonic deadline from a server duration packet.
	////
	private static void OnClientDelayPacket(ParentalControlDeathDelayPacket packet)
	{
		if (clientApi == null)
		{
			return;
		}

		long remainingMilliseconds = Math.Max(0, packet.RemainingMilliseconds);
		clientRespawnAvailableAtElapsedMilliseconds = AddSaturating(
			clientApi.ElapsedMilliseconds,
			remainingMilliseconds);
		clientNextDeathDelaySeconds = Math.Max(0, packet.NextDeathDelaySeconds);
		clientHasScheduledNextDeathDelay = packet.HasScheduledNextDeathDelay;
	}



	//// Calculates counted deaths remaining after death-free cooldowns.
	////
	//// Cooldown periods use UTC wall time and therefore continue while the
	//// player is offline.  The calculation is lazy: persistence is updated on
	//// the next death rather than by a permanent server tick listener.
	////
	private static int GetDecayedDeathCount(
		ParentalControlDeathDelayState state,
		long nowMilliseconds)
	{
		if (state.DeathCount <= 0 || state.LastDeathAtUtcMilliseconds <= 0)
		{
			return 0;
		}

		long elapsedMilliseconds = Math.Max(0, nowMilliseconds - state.LastDeathAtUtcMilliseconds);
		long cooldownMilliseconds = (long)GhaelenTweaksConfig.Current.PcSpawnDelayCooldown * 1000;
		long expiredDeaths = elapsedMilliseconds / cooldownMilliseconds;

		if (expiredDeaths >= state.DeathCount)
		{
			return 0;
		}

		return state.DeathCount - (int)expiredDeaths;
	}



	//// Calculates the delay assigned to the current death.
	////
	//// Saturating arithmetic keeps corrupt or extremely old player state from
	//// wrapping into a negative deadline.
	////
	private static long CalculateDelaySeconds(int deathCount)
	{
		long penalizedDeaths = Math.Max(0, (long)deathCount - 1);
		return MultiplySaturating(
			penalizedDeaths,
			GhaelenTweaksConfig.Current.PcSpawnDelayIncrement);
	}



	//// Calculates the delay that a hypothetical death at the given time would receive.
	////
	//// Applying lazy cooldown recovery before adding the hypothetical death keeps
	//// the displayed schedule aligned with the server's authoritative next-death
	//// calculation, including death-free time spent offline.
	////
	private static long CalculateNextDeathDelaySeconds(
		ParentalControlDeathDelayState state,
		long nowMilliseconds)
	{
		int nextDeathCount = GetDecayedDeathCount(state, nowMilliseconds);
		if (nextDeathCount < int.MaxValue)
		{
			nextDeathCount++;
		}

		return CalculateDelaySeconds(nextDeathCount);
	}



	//// Reads a player's permanent delay state from its compact JSON payload.
	////
	//// Missing or invalid data starts from a clean state and logs a warning
	//// for malformed payloads without preventing the player from respawning.
	////
	private static ParentalControlDeathDelayState ReadState(IServerPlayer player)
	{
		byte[]? storedData = player.GetModdata(ModDataKey);
		if (storedData == null || storedData.Length == 0)
		{
			return new ParentalControlDeathDelayState();
		}

		try
		{
			string json = Encoding.UTF8.GetString(storedData);
			return JsonConvert.DeserializeObject<ParentalControlDeathDelayState>(json)
				?? new ParentalControlDeathDelayState();
		}
		catch (Exception exception)
		{
			serverApi?.Logger.Warning(
				$"Failed to load parental-controls death delay for {player.PlayerName}: {exception.Message}");
			return new ParentalControlDeathDelayState();
		}
	}



	//// Persists one player's authoritative escalation and active deadline.
	////
	private static void SaveState(IServerPlayer player, ParentalControlDeathDelayState state)
	{
		string json = JsonConvert.SerializeObject(state);
		player.SetModdata(ModDataKey, Encoding.UTF8.GetBytes(json));
	}



	//// Sends the active wait and server-calculated next-death schedule to one client.
	////
	private static void SendDelayPacket(
		IServerPlayer player,
		long remainingMilliseconds,
		long nextDeathDelaySeconds)
	{
		serverChannel?.SendPacket(
			new ParentalControlDeathDelayPacket
			{
				RemainingMilliseconds = Math.Max(0, remainingMilliseconds),
				NextDeathDelaySeconds = Math.Max(0, nextDeathDelaySeconds),
				HasScheduledNextDeathDelay = true
			},
			player);
	}



	//// Clears all death-delay presentation state on one connected client.
	////
	private static void SendClearDelayPacket(IServerPlayer player)
	{
		serverChannel?.SendPacket(new ParentalControlDeathDelayPacket(), player);
	}



	//// Divides positive values while rounding any partial unit upward.
	////
	private static long DivideRoundingUp(long value, long divisor)
	{
		return value <= 0 ? 0 : 1 + ((value - 1) / divisor);
	}



	//// Multiplies non-negative values without overflowing a signed long.
	////
	private static long MultiplySaturating(long left, long right)
	{
		if (left <= 0 || right <= 0)
		{
			return 0;
		}

		return left > long.MaxValue / right ? long.MaxValue : left * right;
	}



	//// Adds non-negative values without overflowing a signed long.
	////
	private static long AddSaturating(long left, long right)
	{
		if (right <= 0)
		{
			return left;
		}

		return left > long.MaxValue - right ? long.MaxValue : left + right;
	}
}

internal sealed class ParentalControlDeathDelayState
{
	[JsonProperty("death-count")]
	public int DeathCount { get; set; }

	//// Migrates the 0.6.x persisted counter without writing its old name again.
	////
	[JsonProperty("accumulated-increments")]
	private int LegacyAccumulatedIncrements
	{
		set => DeathCount = Math.Max(DeathCount, value);
	}

	[JsonProperty("last-death-at-utc-milliseconds")]
	public long LastDeathAtUtcMilliseconds { get; set; }

	[JsonProperty("respawn-available-at-utc-milliseconds")]
	public long RespawnAvailableAtUtcMilliseconds { get; set; }
}

[ProtoContract]
internal sealed class ParentalControlDeathDelayPacket
{
	[ProtoMember(1)]
	public long RemainingMilliseconds { get; set; }

	[ProtoMember(2)]
	public long NextDeathDelaySeconds { get; set; }

	[ProtoMember(3)]
	public bool HasScheduledNextDeathDelay { get; set; }
}
