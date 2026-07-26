/*
 * Adds the player-facing "read blueprint" interaction to Better Ruins
 * schematic items.
 *
 * The behavior is attached by a conditional JSON patch only when Better
 * Ruins is loaded. It does not consume the schematic item. Instead, it asks
 * the server-owned blueprint knowledge service to record the schematic code
 * for the interacting player and to sync that updated state back to the
 * client for crafting preview.
 */

using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GhaelenTweaks;

public sealed class CollectibleBehaviorBetterRuinsBlueprintReading : CollectibleBehavior
{



	//// Creates the collectible behavior instance for one resolved schematic
	//// collectible.
	////
	//// Vintage Story constructs collectible behaviors while resolving item
	//// assets. The behavior stores no per-item mutable state; player
	//// knowledge belongs to BetterRuinsBlueprintKnowledge instead.
	////
	public CollectibleBehaviorBetterRuinsBlueprintReading(CollectibleObject collObj)
		: base(collObj)
	{
	}



	//// Handles right-clicking a Better Ruins schematic in hand.
	////
	//// The client side marks the interaction as handled so Vintage Story will
	//// forward the use action to the server. The server side then records the
	//// blueprint for that player without consuming or damaging the item.
	////
	public override void OnHeldInteractStart(
		ItemSlot slot,
		EntityAgent byEntity,
		BlockSelection blockSel,
		EntitySelection entitySel,
		bool firstEvent,
		ref EnumHandHandling handHandling,
		ref EnumHandling handling)
	{
		ItemStack? itemStack = slot.Itemstack;
		if (itemStack == null || !BetterRuinsBlueprintKnowledge.TryGetSchematicCode(itemStack, out _))
		{
			return;
		}

		// Schematic items do not have another right-click use in Better Ruins.
		// Claiming the interaction on both sides guarantees the server sees the
		// first use event and prevents a later behavior from treating the same
		// click as an unrelated item action.
		handHandling = EnumHandHandling.PreventDefault;
		handling = EnumHandling.PreventSubsequent;

		if (!firstEvent || byEntity.World.Side != EnumAppSide.Server)
		{
			return;
		}

		if ((byEntity as EntityPlayer)?.Player is not IServerPlayer player)
		{
			return;
		}

		if (!BetterRuinsBlueprintKnowledge.TryLearnSchematic(
			player,
			itemStack,
			out string schematicName,
			out bool alreadyLearned))
		{
			return;
		}

		string messageCode = alreadyLearned
			? "ghaelentweaks:betterruins-blueprint-already-learned"
			: "ghaelentweaks:betterruins-blueprint-learned";

		player.SendLocalisedMessage(
			GlobalConstants.GeneralChatGroup,
			messageCode,
			schematicName);

		byEntity.World.PlaySoundAt(
			new AssetLocation("sounds/effect/writing"),
			byEntity,
			player);
	}



	//// Adds held interaction help for Better Ruins schematics.
	////
	//// The help is attached directly to the item behavior so it appears
	//// alongside Vintage Story's normal held-item prompts whenever a schematic
	//// is selected in the hotbar.
	////
	public override WorldInteraction[] GetHeldInteractionHelp(
		ItemSlot inSlot,
		ref EnumHandling handling)
	{
		if (!BetterRuinsBlueprintKnowledge.TryGetSchematicCode(inSlot.Itemstack, out _))
		{
			return Array.Empty<WorldInteraction>();
		}

		return new[]
		{
			new WorldInteraction
			{
				ActionLangCode = "ghaelentweaks:heldhelp-read-betterruins-blueprint",
				MouseButton = EnumMouseButton.Right
			}
		};
	}



	//// Adds client-side learned/unread status text to schematic tooltips.
	////
	//// The tooltip reads only the server-synced client cache. Until the server
	//// has sent the feature state for the current world, no status line is
	//// shown so stale local data cannot mislead the player.
	////
	public override void GetHeldItemInfo(
		ItemSlot inSlot,
		StringBuilder dsc,
		IWorldAccessor world,
		bool withDebugInfo)
	{
		if (!BetterRuinsBlueprintKnowledge.IsClientFeatureEnabled()
			|| !BetterRuinsBlueprintKnowledge.TryGetSchematicCode(inSlot.Itemstack, out string schematicCode))
		{
			return;
		}

		string statusCode = BetterRuinsBlueprintKnowledge.HasClientLearned(schematicCode)
			? "ghaelentweaks:betterruins-blueprint-tooltip-learned"
			: "ghaelentweaks:betterruins-blueprint-tooltip-unread";

		dsc.AppendLine(Lang.Get(statusCode));
	}
}
