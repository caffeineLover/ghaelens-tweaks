/*
 * Registers server-side chat commands for player-facing Ghaelen Tweaks
 * utilities.
 */

using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GhaelenTweaks;

internal static class GhaelenTweaksChatCommands
{
	private const string CommandName = "gtweak";
	private const string SchematicsSubcommand = "schematics";



	//// Registers the server-side `/gtweak` command tree for utility
	//// commands that read or report player-facing mod state.
	////
	//// The owning ModSystem calls this during server startup. Commands are
	//// registered only on the server because the schematic list is stored in
	//// authoritative server-side player mod data, not in client-owned state.
	////
	internal static void StartServerSide(ICoreServerAPI api)
	{
		api.ChatCommands
			.GetOrCreate(CommandName)
			.WithDescription("Ghaelen Tweaks commands")
			.RequiresPlayer()
			.RequiresPrivilege(Privilege.chat)
			.BeginSubCommand(SchematicsSubcommand)
			.WithDescription("List your memorized Better Ruins schematics")
			.RequiresPlayer()
			.RequiresPrivilege(Privilege.chat)
			.HandleWith(args => HandleSchematicsCommand(api, args))
			.EndSubCommand();
	}



	//// Handles `/gtweak schematics` by resolving the caller to a server
	//// player and delegating the actual chat output to the list formatter.
	////
	//// The command builder already requires a player caller, but this guard
	//// keeps the handler defensive if the command is invoked directly by API
	//// code or if Vintage Story changes command validation order.
	////
	private static TextCommandResult HandleSchematicsCommand(
		ICoreServerAPI api,
		TextCommandCallingArgs args)
	{
		if (args.Caller.Player is not IServerPlayer player)
		{
			return TextCommandResult.Error("This command can only be used by a player.");
		}

		SendMemorizedSchematics(api, player, args.Caller.FromChatGroupId);
		return TextCommandResult.Success();
	}



	//// Builds and sends the memorized schematic list for one server player.
	////
	//// The command intentionally reads the persisted server knowledge rather
	//// than the synced client preview cache so the output reflects the same
	//// authoritative data that crafting validation uses.
	////
	private static void SendMemorizedSchematics(
		ICoreServerAPI api,
		IServerPlayer player,
		int groupId)
	{
		string[] schematicCodes = BetterRuinsBlueprintKnowledge.GetLearnedSchematicCodes(player);
		if (schematicCodes.Length == 0)
		{
			player.SendMessage(
				groupId,
				"You have not memorized any Better Ruins schematics.",
				EnumChatType.CommandSuccess);
			return;
		}

		// Mention a disabled feature without hiding the saved data. That gives
		// players useful confirmation that their memorized list still exists
		// even though virtual blueprint crafting is not currently active.
		StringBuilder message = new();
		if (!GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning)
		{
			message.AppendLine("Better Ruins blueprint learning is currently disabled. Saved memorized schematics:");
		}
		else
		{
			message.AppendLine($"Memorized Better Ruins schematics ({schematicCodes.Length}):");
		}

		// Resolve the item name for readability while preserving the exact
		// stored code so server owners can troubleshoot unusual schematic data.
		foreach (string schematicCode in schematicCodes)
		{
			message
				.Append("- ")
				.Append(FormatSchematic(api, schematicCode))
				.AppendLine();
		}

		player.SendMessage(
			groupId,
			message.ToString().TrimEnd(),
			EnumChatType.CommandSuccess);
	}



	//// Converts a stored schematic item code into player-facing chat text.
	////
	//// Better Ruins is optional and item assets may be missing in a broken or
	//// changed mod setup, so unresolved codes are still shown directly
	//// instead of causing the command to fail.
	////
	private static string FormatSchematic(ICoreServerAPI api, string schematicCode)
	{
		Item? item = api.World.GetItem(new AssetLocation(schematicCode));
		if (item == null)
		{
			return schematicCode;
		}

		string itemName = new ItemStack(item).GetName();
		if (string.IsNullOrWhiteSpace(itemName)
			|| string.Equals(itemName, schematicCode, StringComparison.Ordinal))
		{
			return schematicCode;
		}

		return $"{itemName} ({schematicCode})";
	}



}
