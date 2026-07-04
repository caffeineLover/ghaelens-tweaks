using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class BlockBehaviorPalisadeFirewoodDrops : BlockBehavior
{
	private static readonly AssetLocation FirewoodCode = new("game", "firewood");
	private static readonly AssetLocation PlankCode = new("game", "plank-oak");

	public BlockBehaviorPalisadeFirewoodDrops(Block block)
		: base(block)
	{
	}

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

		handling = EnumHandling.PreventDefault;
		return new[] { new ItemStack(dropItem, GetDropQuantity()) };
	}

	private static EnumTool? GetActiveTool(IPlayer player)
	{
		return player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible?.Tool;
	}

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
