/*
 * Implements custom drops for palisade blocks when players dismantle them
 * with woodworking tools.
 *
 * The behavior lets axes recover firewood and saws recover oak planks while
 * preserving vanilla drop behavior for all other tools and missing item
 * definitions. The main mod system registers this
 * behavior for palisade block variants through assets.
 */

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class BlockBehaviorPalisadeFirewoodDrops : BlockBehavior
{
	private static readonly AssetLocation FirewoodCode = new("game", "firewood");
	private static readonly AssetLocation PlankCode = new("game", "plank-oak");



	//// Creates the palisade dismantling drop behavior for the block instance
	//// supplied by Vintage Story.
	////
	//// Construction does not resolve drop items because item availability is
	//// world-owned and should be checked through the current world accessor
	//// when drops are requested.
	////
	public BlockBehaviorPalisadeFirewoodDrops(Block block)
		: base(block)
	{
	}



	//// Replaces vanilla palisade drops when the player breaks the block with
	//// an axe or saw.
	////
	//// Vintage Story invokes this during block breaking. The method falls
	//// back to the base behavior whenever the active tool is not one of the
	//// supported woodworking tools or when the target drop item cannot be
	//// resolved in the current world.
	////
	public override ItemStack[] GetDrops(
		IWorldAccessor world,
		BlockPos pos,
		IPlayer byPlayer,
		ref float dropChanceMultiplier,
		ref EnumHandling handling)
	{
		EnumTool? tool = GetActiveTool(byPlayer);
		if (tool is not (EnumTool.Axe or EnumTool.Saw))
		{
			return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
		}

		Item? dropItem = world.GetItem(tool == EnumTool.Saw ? PlankCode : FirewoodCode);
		if (dropItem == null)
		{
			return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
		}

		// Claim the drop calculation only after the replacement item is known
		// to exist, preventing accidental deletion of vanilla drops.
		handling = EnumHandling.PreventDefault;
		return new[] { new ItemStack(dropItem, GetDropQuantity()) };
	}



	//// Reads the tool type from the player's active hotbar stack.
	////
	//// Tool detection is isolated so the drop override can stay focused on
	//// fallback behavior and replacement drop selection.
	////
	private static EnumTool? GetActiveTool(IPlayer player)
	{
		return player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible?.Tool;
	}



	//// Determines how many recovered materials should come from the current
	//// palisade block variant.
	////
	//// Wall pieces encode their size in the block code path. Stakes and any
	//// unrecognized variant return one item so unsupported shapes do not
	//// accidentally become high-yield drops.
	////
	private int GetDropQuantity()
	{
		string? path = block.Code?.Path;
		if (path == null)
		{
			return 1;
		}

		if (path.StartsWith("palisadewall-four-", StringComparison.Ordinal))
		{
			return 4;
		}

		return path.StartsWith("palisadewall-three-", StringComparison.Ordinal) ? 3 : 1;
	}
}
