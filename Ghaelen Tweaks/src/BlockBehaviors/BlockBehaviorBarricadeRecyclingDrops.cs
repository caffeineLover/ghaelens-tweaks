/*
 * Implements recycling drops for looted clutter barricades.
 *
 * Barricades are salvaged clutter rather than fresh construction, so axes
 * recover aged firewood and saws recover aged planks. The behavior preserves
 * vanilla drops for non-barricade blocks, unsupported tools, and worlds where
 * the replacement items are unavailable.
 */

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class BlockBehaviorBarricadeRecyclingDrops : BlockBehavior
{
	private static readonly AssetLocation AgedFirewoodCode = new("game", "agedfirewood");
	private static readonly AssetLocation AgedPlankCode = new("game", "plank-aged");
	private const int DropQuantity = 4;



	//// Creates the barricade recycling behavior for the block instance
	//// supplied by Vintage Story.
	////
	//// The behavior does not cache item definitions during construction
	//// because drop resolution depends on the active world accessor.
	////
	public BlockBehaviorBarricadeRecyclingDrops(Block block)
		: base(block)
	{
	}



	//// Replaces vanilla drops for clutter barricades broken with axes or
	//// saws.
	////
	//// Vintage Story invokes this during block breaking. The method first
	//// verifies both the block family and active tool before claiming the
	//// drop calculation, then falls back to vanilla behavior if the aged
	//// material item is unavailable.
	////
	public override ItemStack[] GetDrops(
		IWorldAccessor world,
		BlockPos pos,
		IPlayer byPlayer,
		ref float dropChanceMultiplier,
		ref EnumHandling handling)
	{
		EnumTool? tool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible?.Tool;
		if (!IsBarricadeBlock() || tool is not (EnumTool.Axe or EnumTool.Saw))
		{
			return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
		}

		Item? dropItem = world.GetItem(tool == EnumTool.Saw ? AgedPlankCode : AgedFirewoodCode);
		if (dropItem == null)
		{
			return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
		}

		// Claim the drop calculation only after the replacement item is
		// resolved, so missing assets cannot suppress vanilla drops.
		handling = EnumHandling.PreventDefault;
		return new[] { new ItemStack(dropItem, DropQuantity) };
	}



	//// Identifies the clutter barricade block family supported by this
	//// recycling behavior.
	////
	//// The prefix check keeps the behavior compatible with individual
	//// barricade variants without listing every code path.
	////
	private bool IsBarricadeBlock()
	{
		return block.Code?.Path.StartsWith("clutter-barricade", StringComparison.Ordinal) == true;
	}
}
