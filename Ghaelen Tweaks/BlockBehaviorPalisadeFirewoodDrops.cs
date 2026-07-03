using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class BlockBehaviorPalisadeFirewoodDrops : BlockBehavior
{
	private static readonly AssetLocation FirewoodCode = new("game", "firewood");

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
		if (!IsAxeOrSaw(byPlayer))
		{
			return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
		}

		Item? firewood = world.GetItem(FirewoodCode);
		if (firewood == null)
		{
			return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
		}

		handling = EnumHandling.PreventDefault;
		return new[] { new ItemStack(firewood, GetFirewoodQuantity()) };
	}

	private static bool IsAxeOrSaw(IPlayer player)
	{
		EnumTool? tool = player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible?.Tool;
		return tool is EnumTool.Axe or EnumTool.Saw;
	}

	private int GetFirewoodQuantity()
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
