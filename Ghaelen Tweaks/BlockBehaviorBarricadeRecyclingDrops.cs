using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class BlockBehaviorBarricadeRecyclingDrops : BlockBehavior
{
	private static readonly AssetLocation AgedFirewoodCode = new("game", "agedfirewood");
	private static readonly AssetLocation AgedPlankCode = new("game", "plank-aged");
	private const int DropQuantity = 4;

	public BlockBehaviorBarricadeRecyclingDrops(Block block)
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

		handling = EnumHandling.PreventDefault;
		return new[] { new ItemStack(dropItem, DropQuantity) };
	}



	private bool IsBarricadeBlock()
	{
		return block.Code?.Path.StartsWith("clutter-barricade", StringComparison.Ordinal) == true;
	}
}
