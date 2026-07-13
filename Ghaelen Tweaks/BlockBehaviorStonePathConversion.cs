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

	public BlockBehaviorStonePathConversion(Block block)
		: base(block)
	{
	}

	public override bool OnBlockInteractStart(
		IWorldAccessor world,
		IPlayer byPlayer,
		BlockSelection blockSel,
		ref EnumHandling handling)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!IsLooseStoneStack(slot.Itemstack))
		{
			return false;
		}

		int requiredStones = GetRequiredStoneCount(byPlayer);
		if (slot.StackSize < requiredStones)
		{
			return false;
		}

		handling = EnumHandling.PreventDefault;

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

		slot.TakeOut(requiredStones);
		slot.MarkDirty();

		world.BlockAccessor.ExchangeBlock(stonePathBlock.BlockId, blockSel.Position);
		world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
		world.PlaySoundAt(StonePathPlaceSound, blockSel.Position, -0.5, byPlayer, true, 32f);

		return true;
	}

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



	private static bool IsLooseStoneStack(ItemStack? stack)
	{
		return stack?.Class == EnumItemClass.Item
		       && stack.Collectible?.Code?.Domain == "game"
		       && stack.Collectible.Code.Path.StartsWith("stone-", StringComparison.Ordinal);
	}



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