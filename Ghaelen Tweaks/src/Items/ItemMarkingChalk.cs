/*
 * Implements the marking chalk held-item interaction.
 *
 * Marking chalk places thin decor glyphs on valid solid block faces without
 * replacing the target block or occupying the neighboring block space. The
 * selected glyph is stored on the item stack like vanilla tool modes, while
 * the active stick's remaining uses are tracked as stack attributes so dyed
 * chalk can remain stackable.
 */

using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace GhaelenTweaks;

public sealed class ItemMarkingChalk : Item
{
	private const string ModeAttribute = "markingChalkMode";
	private const string UsesLeftAttribute = "markingChalkUsesLeft";
	private static readonly AssetLocation DrawSound = new("game", "sounds/player/chalkdraw");
	private SkillItem[] toolModes = Array.Empty<SkillItem>();

	private static readonly MarkingChalkMode[] ModeDefinitions =
	{
		new("arrow", "arrow", "ghaelentweaks:marking-chalk-mode-arrow", "markingchalk-arrow.svg"),
		new("x", "x", "ghaelentweaks:marking-chalk-mode-x", "markingchalk-x.svg"),
		new("dot", "dot", "ghaelentweaks:marking-chalk-mode-dot", "markingchalk-dot.svg"),
		new("ladder", "ladder", "ghaelentweaks:marking-chalk-mode-ladder", "markingchalk-ladder.svg"),
		new("stairs", "stairs", "ghaelentweaks:marking-chalk-mode-stairs", "markingchalk-stairs.svg"),
		new("danger", "danger", "ghaelentweaks:marking-chalk-mode-danger", "markingchalk-danger.svg"),
		new("exit", "exit", "ghaelentweaks:marking-chalk-mode-exit", "markingchalk-exit.svg")
	};



	//// Initializes client-side tool-mode icons and labels.
	////
	//// The server never renders the mode picker, but it still needs the same
	//// mode definitions for bounds checks when a stack attribute stores an
	//// out-of-date index.
	////
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);

		ICoreClientAPI? capi = api as ICoreClientAPI;
		toolModes = new SkillItem[ModeDefinitions.Length];

		for (int index = 0; index < ModeDefinitions.Length; index++)
		{
			MarkingChalkMode mode = ModeDefinitions[index];
			SkillItem skillItem = new()
			{
				Code = new AssetLocation(mode.Code),
				Name = Lang.Get(mode.LangCode)
			};

			if (capi != null)
			{
				AssetLocation iconCode = new("ghaelentweaks", $"textures/icons/{mode.IconPath}");
				skillItem.WithIcon(capi, capi.Gui.LoadSvgWithPadding(iconCode, 48, 48, 5, -1));
				skillItem.TexturePremultipliedAlpha = false;
			}

			toolModes[index] = skillItem;
		}
	}



	//// Releases client-side mode icon textures.
	////
	public override void OnUnloaded(ICoreAPI api)
	{
		foreach (SkillItem toolMode in toolModes)
		{
			toolMode.Dispose();
		}

		toolModes = Array.Empty<SkillItem>();
		base.OnUnloaded(api);
	}



	//// Handles right-click drawing against a selected block face.
	////
	//// The client claims the interaction immediately so the server receives
	//// the use event and the player sees the normal held-item animation. The
	//// server performs all permission, surface, decor placement, and use
	//// consumption work.
	////
	public override void OnHeldInteractStart(
		ItemSlot slot,
		EntityAgent byEntity,
		BlockSelection blockSel,
		EntitySelection entitySel,
		bool firstEvent,
		ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction)
		{
			return;
		}

		if (slot.Itemstack == null || blockSel?.Position == null || blockSel.Face == null)
		{
			return;
		}

		handling = EnumHandHandling.PreventDefaultAction;

		if (byEntity.World.Side == EnumAppSide.Client)
		{
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return;
		}

		if (!firstEvent || (byEntity as EntityPlayer)?.Player is not IPlayer byPlayer)
		{
			return;
		}

		if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}

		IBlockAccessor blockAccessor = byEntity.World.BlockAccessor;
		if (!SuitablePosition(blockAccessor, blockSel))
		{
			(byPlayer as IServerPlayer)?.SendIngameError(
				"markingchalk-invalid-surface",
				Lang.Get("ghaelentweaks:marking-chalk-invalid-surface"));
			return;
		}

		Block? decorBlock = ResolveDecorBlock(byEntity, slot.Itemstack);
		if (decorBlock == null)
		{
			return;
		}

		if (!blockAccessor.SetDecor(decorBlock, blockSel.Position, blockSel.ToDecorIndex()))
		{
			return;
		}

		ConsumeUse(slot);
		byEntity.World.PlaySoundAt(
			DrawSound,
			blockSel.FullPosition.X,
			blockSel.FullPosition.Y,
			blockSel.FullPosition.Z,
			byPlayer,
			true,
			8f,
			1f);
	}



	//// Returns the selectable glyph modes shown by Vintage Story's tool-mode
	//// selector.
	////
	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return toolModes;
	}



	//// Gets the selected glyph mode stored on this chalk stack.
	////
	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (slot.Itemstack == null || toolModes.Length == 0)
		{
			return 0;
		}

		return GameMath.Clamp(slot.Itemstack.Attributes.GetInt(ModeAttribute, 0), 0, toolModes.Length - 1);
	}



	//// Stores the selected glyph mode on this chalk stack.
	////
	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		if (slot.Itemstack == null || toolModes.Length == 0)
		{
			return;
		}

		slot.Itemstack.Attributes.SetInt(ModeAttribute, GameMath.Clamp(toolMode, 0, toolModes.Length - 1));
		slot.MarkDirty();
	}



	//// Adds held-item help for drawing and mode selection.
	////
	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new[]
		{
			new WorldInteraction
			{
				ActionLangCode = "ghaelentweaks:heldhelp-draw-marking-chalk",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "Change tool mode",
				HotKeyCodes = new[] { "toolmodeselect" },
				MouseButton = EnumMouseButton.None
			}
		};
	}



	//// Adds active-stick use count to the item tooltip.
	////
	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

		if (inSlot.Itemstack == null)
		{
			return;
		}

		dsc.AppendLine(Lang.Get(
			"ghaelentweaks:marking-chalk-uses-left",
			GetUsesLeft(inSlot.Itemstack),
			GetMaxUses()));
	}



	//// Resolves the decor block matching this chalk color and selected glyph.
	////
	private Block? ResolveDecorBlock(EntityAgent byEntity, ItemStack itemStack)
	{
		string color = itemStack.Collectible.Variant.TryGetValue("color", out string? colorVariant)
			? colorVariant
			: "white";

		int modeIndex = GameMath.Clamp(itemStack.Attributes.GetInt(ModeAttribute, 0), 0, ModeDefinitions.Length - 1);
		string symbol = GetDecorSymbol(ModeDefinitions[modeIndex], byEntity);
		AssetLocation blockCode = new("ghaelentweaks", $"markingchalk-{color}-{symbol}");

		Block? block = byEntity.World.GetBlock(blockCode);
		if (block == null || block.IsMissing)
		{
			byEntity.World.Logger.Warning($"Missing marking chalk decor block {blockCode}.");
			return null;
		}

		return block;
	}



	//// Converts the selected mode into a decor symbol variant.
	////
	//// Arrow mode is directional. It uses the player's yaw so floor and
	//// ceiling arrows point in a world direction instead of only using the
	//// clicked face.
	////
	private static string GetDecorSymbol(MarkingChalkMode mode, EntityAgent byEntity)
	{
		if (mode.DecorSymbol != "arrow")
		{
			return mode.DecorSymbol;
		}

		BlockFacing direction = BlockFacing.HorizontalFromYaw((float)byEntity.Pos.Yaw);
		return $"arrow-{direction.Code}";
	}



	//// Checks whether the clicked block face is a supported marking surface.
	////
	private static bool SuitablePosition(IBlockAccessor blockAccessor, BlockSelection blockSel)
	{
		Block attachingBlock = blockAccessor.GetBlock(blockSel.Position);
		if (!attachingBlock.SideSolid[blockSel.Face.Index])
		{
			return false;
		}

		EnumBlockMaterial targetMaterial = attachingBlock.GetBlockMaterial(blockAccessor, blockSel.Position, null);
		if (targetMaterial is EnumBlockMaterial.Stone or EnumBlockMaterial.Ore or EnumBlockMaterial.Brick)
		{
			return true;
		}

		string path = attachingBlock.Code?.Path ?? "";
		return IsSupportedTreeTrunk(path, targetMaterial) || IsSupportedPreparedSoil(path, targetMaterial);
	}



	//// Identifies logs and trunk-like blocks without allowing every wooden
	//// block surface.
	////
	private static bool IsSupportedTreeTrunk(string path, EnumBlockMaterial material)
	{
		if (material != EnumBlockMaterial.Wood)
		{
			return false;
		}

		return path.StartsWith("log-", StringComparison.Ordinal)
		       || path.StartsWith("logsection-", StringComparison.Ordinal)
		       || path.StartsWith("logquad-", StringComparison.Ordinal)
		       || path.StartsWith("lognarrow-", StringComparison.Ordinal);
	}



	//// Identifies packed dirt and rammed-earth blocks without allowing every
	//// soil block.
	////
	private static bool IsSupportedPreparedSoil(string path, EnumBlockMaterial material)
	{
		if (material != EnumBlockMaterial.Soil)
		{
			return false;
		}

		return path.StartsWith("packeddirt", StringComparison.Ordinal)
		       || path.StartsWith("drypackeddirt", StringComparison.Ordinal)
		       || path.StartsWith("rammed-", StringComparison.Ordinal);
	}



	//// Consumes one configured chalk use from the active stack.
	////
	//// The stack attribute represents the currently used stick. Once it
	//// reaches zero, one item is removed and the remaining stack, if any,
	//// starts a fresh stick with no use-count attribute.
	////
	private static void ConsumeUse(ItemSlot slot)
	{
		ItemStack? itemStack = slot.Itemstack;
		if (itemStack == null)
		{
			return;
		}

		int usesLeft = GetUsesLeft(itemStack) - 1;
		if (usesLeft <= 0)
		{
			slot.TakeOut(1);
			slot.Itemstack?.Attributes.RemoveAttribute(UsesLeftAttribute);
		}
		else
		{
			itemStack.Attributes.SetInt(UsesLeftAttribute, usesLeft);
		}

		slot.MarkDirty();
	}



	//// Gets the remaining uses on the active stick in a chalk stack.
	////
	private static int GetUsesLeft(ItemStack itemStack)
	{
		int maxUses = GetMaxUses();
		int usesLeft = itemStack.Attributes.GetInt(UsesLeftAttribute, maxUses);

		return GameMath.Clamp(usesLeft, 1, maxUses);
	}



	//// Gets the normalized configured use count for each chalk stick.
	////
	private static int GetMaxUses()
	{
		return Math.Max(1, GhaelenTweaksConfig.Current.MarkingChalkUses);
	}

	private readonly record struct MarkingChalkMode(
		string Code,
		string DecorSymbol,
		string LangCode,
		string IconPath);
}
