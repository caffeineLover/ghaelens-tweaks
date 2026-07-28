/*
 * Owns the persistent and synced player knowledge for Better Ruins
 * schematic blueprints.
 *
 * The server is authoritative: reading a physical Better Ruins schematic
 * stores the schematic's canonical item code in that player's permanent
 * mod data. The client receives a read-only copy over a small mod network
 * channel so the crafting-grid Harmony patch can show the same recipe
 * preview that the server will accept.
 *
 * This file deliberately does not patch crafting and does not add behavior
 * to items. Those responsibilities stay in the feature's Harmony patch and
 * collectible behavior files.
 */

using System.Text;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GhaelenTweaks.BetterRuins;

internal static class BetterRuinsBlueprintKnowledge
{
	internal const string BetterRuinsModId = "betterruins";
	private const string ChannelName = "ghaelentweaks-betterruins-blueprints";
	private const string ModDataKey = "ghaelentweaks:betterruins-blueprint-knowledge";
	private const string SchematicPathPrefix = "br-schematic-";
	private static readonly HashSet<string> ClientLearnedSchematicCodes = new(StringComparer.Ordinal);
	private static ICoreServerAPI? serverApi;
	private static IServerNetworkChannel? serverChannel;
	private static PlayerDelegate? playerNowPlayingHandler;
	private static bool clientFeatureEnabled;



	//// Registers the client-side network receiver used by crafting preview.
	////
	//// The server sends the full learned schematic set whenever the player
	//// joins and whenever a new schematic is learned. The client never sends
	//// knowledge changes back to the server, which keeps blueprint learning
	//// authoritative on the server.
	////
	internal static void StartClientSide(ICoreClientAPI api)
	{
		ClientLearnedSchematicCodes.Clear();
		clientFeatureEnabled = false;

		api.Network
			.RegisterChannel(ChannelName)
			.RegisterMessageType<BetterRuinsBlueprintKnowledgePacket>()
			.SetMessageHandler<BetterRuinsBlueprintKnowledgePacket>(OnClientKnowledgePacket);
	}



	//// Registers the server-side network channel and player join hook used
	//// to send each player their own learned schematic set.
	////
	//// The join hook waits for PlayerNowPlaying so the game client has
	//// finished loading before the first crafting-preview packet is sent.
	////
	internal static void StartServerSide(ICoreServerAPI api)
	{
		serverApi = api;
		serverChannel = api.Network
			.RegisterChannel(ChannelName)
			.RegisterMessageType<BetterRuinsBlueprintKnowledgePacket>();

		playerNowPlayingHandler = SendLearnedBlueprints;
		api.Event.PlayerNowPlaying += playerNowPlayingHandler;
	}



	//// Releases static references held by the knowledge sync layer.
	////
	//// Vintage Story can unload and reload mods inside the same process.
	//// Removing the player event handler and clearing the client cache avoids
	//// leaking stale world state into a later world or reconnect.
	////
	internal static void Dispose()
	{
		if (serverApi != null && playerNowPlayingHandler != null)
		{
			serverApi.Event.PlayerNowPlaying -= playerNowPlayingHandler;
		}

		playerNowPlayingHandler = null;
		serverChannel = null;
		serverApi = null;
		clientFeatureEnabled = false;
		ClientLearnedSchematicCodes.Clear();
	}



	//// Records a schematic as learned for the supplied server player.
	////
	//// The collectible behavior calls this after confirming that a player
	//// right-clicked a Better Ruins schematic. The method leaves the item in
	//// the player's inventory, persists only the schematic code, and syncs
	//// the updated set back to that player.
	////
	internal static bool TryLearnSchematic(
		IServerPlayer player,
		ItemStack itemStack,
		out string schematicName,
		out bool alreadyLearned)
	{
		schematicName = itemStack.GetName();
		alreadyLearned = false;

		if (!GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning)
		{
			return false;
		}

		if (!TryGetSchematicCode(itemStack, out string schematicCode))
		{
			return false;
		}

		HashSet<string> learnedCodes = ReadServerLearnedSet(player);
		if (!learnedCodes.Add(schematicCode))
		{
			alreadyLearned = true;
			return true;
		}

		SaveServerLearnedSet(player, learnedCodes);
		SendLearnedBlueprints(player);
		return true;
	}



	//// Checks whether a recipe may treat the supplied schematic code as
	//// already known for the given player.
	////
	//// Server calls read the player's permanent mod data. Client calls read
	//// the server-synced cache, which is intentionally empty until the server
	//// sends the initial packet for the current world.
	////
	internal static bool HasLearned(IPlayer? player, string schematicCode)
	{
		if (player == null)
		{
			return false;
		}

		if (!TryNormalizeSchematicCode(schematicCode, out string normalizedCode))
		{
			return false;
		}

		if (player is IServerPlayer serverPlayer)
		{
			return GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning
				&& ReadServerLearnedSet(serverPlayer).Contains(normalizedCode);
		}

		return clientFeatureEnabled && ClientLearnedSchematicCodes.Contains(normalizedCode);
	}



	//// Checks the client-side cache for item tooltip display.
	////
	//// The tooltip runs without a direct player parameter, so it uses only
	//// the synced client cache. Server-side persistence is still the source
	//// of truth for crafting and for future client sync packets.
	////
	internal static bool HasClientLearned(string schematicCode)
	{
		return clientFeatureEnabled
			&& TryNormalizeSchematicCode(schematicCode, out string normalizedCode)
			&& ClientLearnedSchematicCodes.Contains(normalizedCode);
	}



	//// Returns the persisted schematic codes learned by the supplied server
	//// player.
	////
	//// Chat commands use this server-side view so the listed knowledge is
	//// authoritative and matches what recipe validation will read.
	internal static string[] GetLearnedSchematicCodes(IServerPlayer player)
	{
		return ReadServerLearnedSet(player)
			.OrderBy(code => code, StringComparer.Ordinal)
			.ToArray();
	}



	//// Returns whether the server has told this client that blueprint
	//// learning is enabled for the current connection.
	////
	//// Tooltips use this to avoid displaying stale local information before
	//// the server has sent the authoritative feature state.
	////
	internal static bool IsClientFeatureEnabled()
	{
		return clientFeatureEnabled;
	}



	//// Sends the current server-side knowledge state to every online player.
	////
	//// Config Lib setting changes call this after the feature toggle changes
	//// so connected clients immediately update their crafting preview state.
	////
	internal static void SyncAllOnlinePlayers()
	{
		if (serverApi == null)
		{
			return;
		}

		foreach (IPlayer player in serverApi.World.AllOnlinePlayers)
		{
			if (player is IServerPlayer serverPlayer)
			{
				SendLearnedBlueprints(serverPlayer);
			}
		}
	}



	//// Extracts the canonical schematic item code from a Better Ruins
	//// schematic item stack.
	////
	//// Only real Better Ruins item stacks qualify. Wildcard recipe codes are
	//// deliberately excluded elsewhere because a learned wildcard would be
	//// too broad to represent one player-read blueprint.
	////
	internal static bool TryGetSchematicCode(ItemStack? itemStack, out string schematicCode)
	{
		schematicCode = string.Empty;

		AssetLocation? collectibleCode = itemStack?.Collectible?.Code;
		if (collectibleCode == null || !IsBetterRuinsSchematicLocation(collectibleCode))
		{
			return false;
		}

		schematicCode = collectibleCode.ToShortString();
		return true;
	}



	//// Extracts a canonical schematic code from an exact, non-consuming
	//// Better Ruins recipe ingredient.
	////
	//// The crafting patch uses this to distinguish blueprint gate ingredients
	//// from ordinary Better Ruins items and from wildcard schematic-copy
	//// recipes, which cannot be satisfied by one learned blueprint code.
	////
	internal static bool TryGetExactSchematicIngredientCode(
		IRecipeIngredient? ingredient,
		out string schematicCode)
	{
		schematicCode = string.Empty;

		if (ingredient == null
			|| ingredient.Type != EnumItemClass.Item
			|| ingredient.ConsumeProperties.Consume
			|| ingredient.MatchingType != EnumRecipeMatchType.Exact)
		{
			return false;
		}

		AssetLocation? resolvedCode = ingredient.ResolvedItemStack?.Collectible?.Code;
		if (resolvedCode != null && IsBetterRuinsSchematicLocation(resolvedCode))
		{
			schematicCode = resolvedCode.ToShortString();
			return true;
		}

		AssetLocation? ingredientCode = ingredient.Code;
		if (ingredientCode == null || !IsBetterRuinsSchematicLocation(ingredientCode))
		{
			return false;
		}

		schematicCode = ingredientCode.ToShortString();
		return true;
	}



	//// Receives the authoritative learned-schematic state from the server.
	////
	//// The packet replaces the whole client cache rather than applying a
	//// delta. That keeps reconnects, config toggles, and future server-side
	//// cleanup straightforward.
	////
	private static void OnClientKnowledgePacket(BetterRuinsBlueprintKnowledgePacket packet)
	{
		ClientLearnedSchematicCodes.Clear();
		clientFeatureEnabled = packet.FeatureEnabled;

		if (!packet.FeatureEnabled || packet.LearnedSchematicCodes == null)
		{
			return;
		}

		foreach (string schematicCode in packet.LearnedSchematicCodes)
		{
			if (TryNormalizeSchematicCode(schematicCode, out string normalizedCode))
			{
				ClientLearnedSchematicCodes.Add(normalizedCode);
			}
		}
	}



	//// Sends one player's server-owned schematic knowledge to that player.
	////
	//// The packet also carries the server feature toggle so the client does
	//// not preview virtual blueprint recipes when the server has disabled
	//// the feature.
	////
	private static void SendLearnedBlueprints(IServerPlayer player)
	{
		if (serverChannel == null)
		{
			return;
		}

		bool featureEnabled = GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning;
		string[] learnedCodes = featureEnabled
			? ReadServerLearnedSet(player).OrderBy(code => code, StringComparer.Ordinal).ToArray()
			: Array.Empty<string>();

		serverChannel.SendPacket(
			new BetterRuinsBlueprintKnowledgePacket
			{
				FeatureEnabled = featureEnabled,
				LearnedSchematicCodes = learnedCodes
			},
			player);
	}



	//// Loads the player's permanent schematic knowledge from server mod data.
	////
	//// The data is stored as a compact JSON string array to avoid relying on
	//// generic serializer behavior for player mod-data persistence.
	////
	private static HashSet<string> ReadServerLearnedSet(IServerPlayer player)
	{
		HashSet<string> learnedCodes = new(StringComparer.Ordinal);
		byte[]? storedData = player.GetModdata(ModDataKey);
		if (storedData == null || storedData.Length == 0)
		{
			return learnedCodes;
		}

		try
		{
			string json = Encoding.UTF8.GetString(storedData);
			string[]? savedCodes = JsonConvert.DeserializeObject<string[]>(json);
			if (savedCodes == null)
			{
				return learnedCodes;
			}

			foreach (string savedCode in savedCodes)
			{
				if (TryNormalizeSchematicCode(savedCode, out string normalizedCode))
				{
					learnedCodes.Add(normalizedCode);
				}
			}
		}
		catch (Exception exception)
		{
			serverApi?.Logger.Warning(
				$"Failed to load Better Ruins blueprint knowledge for {player.PlayerName}: {exception.Message}");
		}

		return learnedCodes;
	}



	//// Persists the player's learned schematic set to server mod data.
	////
	//// Codes are sorted before saving so the serialized value is stable and
	//// easy to inspect if a world save needs manual troubleshooting.
	////
	private static void SaveServerLearnedSet(
		IServerPlayer player,
		HashSet<string> learnedCodes)
	{
		string[] sortedCodes = learnedCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray();
		string json = JsonConvert.SerializeObject(sortedCodes);
		player.SetModdata(ModDataKey, Encoding.UTF8.GetBytes(json));
	}



	//// Normalizes a schematic code into the full Better Ruins item-code
	//// string used by persistence, packets, and recipe matching.
	////
	//// The method accepts both full `betterruins:path` values and bare paths
	//// because Vintage Story recipe JSON often omits the domain inside assets
	//// owned by the same mod.
	////
	private static bool TryNormalizeSchematicCode(
		string? schematicCode,
		out string normalizedCode)
	{
		normalizedCode = string.Empty;
		if (string.IsNullOrWhiteSpace(schematicCode))
		{
			return false;
		}

		AssetLocation location = schematicCode.Contains(':')
			? new AssetLocation(schematicCode)
			: new AssetLocation(BetterRuinsModId, schematicCode);

		if (!IsBetterRuinsSchematicLocation(location))
		{
			return false;
		}

		normalizedCode = location.ToShortString();
		return true;
	}



	//// Checks whether an asset location identifies one concrete Better Ruins
	//// schematic item.
	////
	//// Wildcard and placeholder paths are rejected here so a single stored
	//// player knowledge entry always maps to one real blueprint item.
	////
	private static bool IsBetterRuinsSchematicLocation(AssetLocation? location)
	{
		return location != null
			&& string.Equals(location.Domain, BetterRuinsModId, StringComparison.Ordinal)
			&& location.Path.StartsWith(SchematicPathPrefix, StringComparison.Ordinal)
			&& !location.Path.Contains('*')
			&& !location.Path.Contains('{')
			&& !location.Path.Contains('}');
	}
}

[ProtoContract]
internal sealed class BetterRuinsBlueprintKnowledgePacket
{
	[ProtoMember(1)]
	public bool FeatureEnabled { get; set; }

	[ProtoMember(2)]
	public string[]? LearnedSchematicCodes { get; set; } = Array.Empty<string>();
}
