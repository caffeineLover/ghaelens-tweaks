/*
 * Implements the placed-block interaction that upgrades prepared dirt road
 * surfaces into vanilla stone paths.
 *
 * The main mod system registers this behavior against the configured packed
 * dirt and rammed earth blocks. This behavior owns only the right-click
 * conversion rule, including class-based loose-stone costs, client hand
 * animation, server-side block exchange, and interaction help.
 */

using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GhaelenTweaks;

public sealed class BlockBehaviorStonePathConversion : BlockBehavior
{
	private static readonly HashSet<string> TwoStoneClasses = new(StringComparer.Ordinal)
	{
		"commoner",
		"mason",
		"miner"
	};

	private static readonly HashSet<string> ThreeStoneClasses = new(StringComparer.Ordinal)
	{
		"artisan",
		"clockmaker",
		"homesteader",
		"tinker"
	};

	private static readonly AssetLocation StonePathBlockCode = new("game", "stonepath-free");
	private static readonly AssetLocation StonePathPlaceSound = new("survival", "sounds/block/gravel");



	//// Creates the block behavior instance owned by Vintage Story's block
	//// behavior system.
	////
	//// Construction only records the block supplied by the engine through
	//// the base class. The main mod system performs behavior registration
	//// during startup.
	////
	public BlockBehaviorStonePathConversion(Block block)
		: base(block)
	{
	}



	//// Handles player right-click conversion from prepared soil blocks to a
	//// vanilla loose-stone path.
	////
	//// Vintage Story invokes this method for placed blocks that include this
	//// behavior. The method performs inexpensive item and stack-size checks
	//// before claiming the interaction, mirrors vanilla client feedback on
	//// the client side, and applies the authoritative block change only on
	//// the server side.
	////
	public override bool OnBlockInteractStart(
		IWorldAccessor world,
		IPlayer byPlayer,
		BlockSelection blockSel,
		ref EnumHandling handling)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

		// Ignore all non-loose-stone interactions so other block behavior and
		// vanilla interactions can continue normally.
		if (!IsLooseStoneStack(slot.Itemstack))
		{
			return false;
		}

		int requiredStones = GetRequiredStoneCount(byPlayer);

		// Do not claim the interaction unless the active stack can actually
		// pay the class-adjusted conversion cost.
		if (slot.StackSize < requiredStones)
		{
			return false;
		}

		handling = EnumHandling.PreventDefault;

		// The client cannot exchange blocks authoritatively, but it should
		// still show the held-item interaction animation once the server-side
		// operation is known to be valid.
		if (world.Side != EnumAppSide.Server)
		{
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}

		Block? stonePathBlock = world.GetBlock(StonePathBlockCode);
		if (stonePathBlock == null)
		{
			return false;
		}

		// Consume the exact class-based cost before exchanging the block so
		// inventory state and world state stay in sync for the authoritative
		// server interaction.
		slot.TakeOut(requiredStones);
		slot.MarkDirty();

		world.BlockAccessor.ExchangeBlock(stonePathBlock.BlockId, blockSel.Position);
		world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
		world.PlaySoundAt(StonePathPlaceSound, blockSel.Position, -0.5, byPlayer, true, 32f);

		return true;
	}



	//// Provides Vintage Story's contextual placed-block help for the road
	//// conversion interaction.
	////
	//// The returned interaction advertises loose stones as the required held
	//// item and leaves localization text in the asset language files.
	////
	public override WorldInteraction[] GetPlacedBlockInteractionHelp(
		IWorldAccessor world,
		BlockSelection selection,
		IPlayer forPlayer,
		ref EnumHandling handling)
	{
		return new[]
		{
			new WorldInteraction
			{
				ActionLangCode = "ghaelentweaks:blockhelp-make-stone-path",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = new[]
				{
					new ItemStack(world.GetItem(new AssetLocation("game", "stone-granite")))
				}
			}
		};
	}



	//// Identifies loose-stone item stacks that can be consumed for the path
	//// conversion recipe.
	////
	//// The check intentionally matches the vanilla `game:stone-*` family so
	//// all rock variants work without maintaining a separate allowlist.
	////
	private static bool IsLooseStoneStack(ItemStack? stack)
	{
		return stack?.Class == EnumItemClass.Item
		       && stack.Collectible?.Code?.Domain == "game"
		       && stack.Collectible.Code.Path.StartsWith("stone-", StringComparison.Ordinal);
	}



	//// Calculates how many loose stones the interacting player must spend
	//// for the prepared-soil path conversion.
	////
	//// The cost uses the character class stored on the player entity by
	//// Vintage Story. Unknown or missing classes pay the full default cost.
	////
	private static int GetRequiredStoneCount(IPlayer player)
	{
		string? characterClass = player.Entity?.WatchedAttributes.GetString("characterClass");
		if (characterClass == null)
		{
			return 4;
		}

		if (TwoStoneClasses.Contains(characterClass))
		{
			return 2;
		}

		return ThreeStoneClasses.Contains(characterClass) ? 3 : 4;
	}
}
