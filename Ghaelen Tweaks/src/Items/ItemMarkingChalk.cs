/*
 * Implements the marking chalk held-item interaction.
 *
 * Marking chalk places thin decor glyphs on valid solid block faces without
 * replacing the target block or occupying the neighboring block space. The
 * selected glyph is stored on the item stack like vanilla tool modes. Fresh
 * chalk sticks can stack, while a stick that has been used is split into its
 * own stack so its remaining-use attribute describes exactly one item.
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
	private const string MarkingChalkDecorPrefix = "markingchalk-";
	private const string TemporalMarkingChalkItemPrefix = "temporal-marking-chalk-";
	private const string TemporalMarkingChalkDecorPrefix = "markingchalk-temporal-";
	private const string MarkingChalkPaintDecorPrefix = "markingchalk-paint-";
	private const string TemporalMarkingChalkPaintDecorPrefix = "markingchalk-paint-temporal-";
	private const string ArrowUpCellVariant = "1-1";
	private const int FullFacePaintUseCost = 4;
	private const int TemporalChalkLightLevel = 2;
	private const int EraseSearchRadius = 2;
	private static readonly AssetLocation DrawSound = new("game", "sounds/player/chalkdraw");
	private static readonly AssetLocation MarkingChalkLightEntityCode =
		new("ghaelentweaks", EntityMarkingChalkLight.EntityCodePath);
	private SkillItem[] toolModes = Array.Empty<SkillItem>();

	private static readonly MarkingChalkMode[] ModeDefinitions =
	{
		new(
			"arrow-up",
			ArrowUpCellVariant,
			"ghaelentweaks:marking-chalk-mode-arrow-up",
			"markingchalk-arrow-up.svg",
			ArrowDirection: MarkingChalkArrowDirection.Up),
		// In-game surfacelayer rendering mirrors horizontal arrow cells on tested wall faces.  Keep right and left
		// mapped to opposite source cells so the placed mark matches the selected toolbar icon.
		new(
			"arrow-right",
			"4-1",
			"ghaelentweaks:marking-chalk-mode-arrow-right",
			"markingchalk-arrow-right.svg",
			ArrowDirection: MarkingChalkArrowDirection.Right),
		new(
			"arrow-down",
			"3-1",
			"ghaelentweaks:marking-chalk-mode-arrow-down",
			"markingchalk-arrow-down.svg",
			ArrowDirection: MarkingChalkArrowDirection.Down),
		new(
			"arrow-left",
			"2-1",
			"ghaelentweaks:marking-chalk-mode-arrow-left",
			"markingchalk-arrow-left.svg",
			ArrowDirection: MarkingChalkArrowDirection.Left),
		new("x", "5-1", "ghaelentweaks:marking-chalk-mode-x", "markingchalk-x.svg"),
		new("dot", "6-1", "ghaelentweaks:marking-chalk-mode-dot", "markingchalk-dot.svg"),
		new("ladder", "1-2", "ghaelentweaks:marking-chalk-mode-ladder", "markingchalk-ladder.svg"),
		new("stairs", "2-2", "ghaelentweaks:marking-chalk-mode-stairs", "markingchalk-stairs.svg"),
		new("danger", "3-2", "ghaelentweaks:marking-chalk-mode-danger", "markingchalk-danger.svg"),
		new("exit", "4-2", "ghaelentweaks:marking-chalk-mode-exit", "markingchalk-exit.svg"),
		new("erase", null, "ghaelentweaks:marking-chalk-mode-erase", "markingchalk-erase.svg", true),
		new(
			"paint-face",
			null,
			"ghaelentweaks:marking-chalk-mode-paint-face",
			"markingchalk-paint-face.svg",
			PaintsFullFace: true,
			UseCost: FullFacePaintUseCost)
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
		HeldPriorityInteract = true;

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



	//// Handles right-click drawing or erasing against a selected block face.
	////
	//// The client claims the interaction immediately so the server receives
	//// the use event and the player sees the normal held-item animation. The
	//// server performs all permission, erase, surface, decor placement, and
	//// use-count work.
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
		if (ShouldErase(byEntity, slot.Itemstack))
		{
			if (TryEraseMark(blockAccessor, blockSel, out ErasedMark erasedMark))
			{
				RemoveTemporalLightMarker(byEntity.World, blockSel.Position, erasedMark.DecorIndex);
				RestoreUses(slot, byPlayer, GetRefundUseCost(erasedMark.DecorBlock));
				PlayChalkSound(byEntity, blockSel, byPlayer);
			}

			return;
		}

		if (!SuitablePosition(blockAccessor, blockSel))
		{
			(byPlayer as IServerPlayer)?.SendIngameError(
				"markingchalk-invalid-surface",
				Lang.Get("ghaelentweaks:marking-chalk-invalid-surface"));
			return;
		}

		MarkingChalkMode mode = GetSelectedMode(slot.Itemstack);
		int useCost = GetUseCost(mode);
		if (GetUsesLeft(slot.Itemstack) < useCost)
		{
			(byPlayer as IServerPlayer)?.SendIngameError(
				"markingchalk-not-enough-uses",
				Lang.Get("ghaelentweaks:marking-chalk-not-enough-uses", useCost));
			return;
		}

		Block? decorBlock = ResolveDecorBlock(byEntity, slot.Itemstack, blockSel, mode);
		if (decorBlock == null)
		{
			return;
		}

		int decorIndex = ResolveDecorIndex(byEntity, blockSel, mode);
		List<int>? staleDecorIndexes = FindOtherMarkingChalkDecorsAtSubposition(blockAccessor, blockSel, decorIndex);
		if (!blockAccessor.SetDecor(decorBlock, blockSel.Position, decorIndex))
		{
			return;
		}

		foreach (int staleDecorIndex in staleDecorIndexes ?? Enumerable.Empty<int>())
		{
			RemoveTemporalLightMarker(byEntity.World, blockSel.Position, staleDecorIndex);
		}

		if (IsTemporalChalk(slot.Itemstack))
		{
			SpawnTemporalLightMarker(byEntity.World, blockSel, decorIndex, GetChalkColor(slot.Itemstack));
		}
		else
		{
			RemoveTemporalLightMarker(byEntity.World, blockSel.Position, decorIndex);
		}

		RemoveDecorIndexes(blockAccessor, blockSel.Position, staleDecorIndexes);
		ConsumeUses(slot, byPlayer, useCost);
		PlayChalkSound(byEntity, blockSel, byPlayer);
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



	//// Adds held-item help for drawing, erasing, and mode selection.
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
				ActionLangCode = "ghaelentweaks:heldhelp-erase-marking-chalk",
				HotKeyCode = "shift",
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

		ItemStack? itemStack = inSlot.Itemstack;
		if (itemStack == null || (itemStack.StackSize > 1 && !itemStack.Attributes.HasAttribute(UsesLeftAttribute)))
		{
			return;
		}

		dsc.AppendLine(Lang.Get(
			"ghaelentweaks:marking-chalk-uses-left",
			GetUsesLeft(itemStack),
			GetMaxUses()));
	}



	//// Resolves the decor block matching this chalk color and selected glyph.
	////
	private Block? ResolveDecorBlock(
		EntityAgent byEntity,
		ItemStack itemStack,
		BlockSelection blockSel,
		MarkingChalkMode mode)
	{
		string color = GetChalkColor(itemStack);
		string blockPathPrefix = ResolveDecorBlockPathPrefix(itemStack, mode);

		string? cell = ResolveDecorCellVariant(blockSel, mode);
		if (cell == null && !mode.PaintsFullFace)
		{
			return null;
		}

		AssetLocation blockCode = mode.PaintsFullFace
			? new AssetLocation("ghaelentweaks", $"{blockPathPrefix}{color}")
			: new AssetLocation("ghaelentweaks", $"{blockPathPrefix}{color}-{cell}");

		Block? block = byEntity.World.GetBlock(blockCode);
		if (block == null || block.IsMissing)
		{
			byEntity.World.Logger.Warning($"Missing marking chalk decor block {blockCode}.");
			return null;
		}

		return block;
	}



	//// Resolves the decor block family used by ordinary, temporal, and
	//// full-face chalk marks.
	////
	private static string ResolveDecorBlockPathPrefix(ItemStack itemStack, MarkingChalkMode mode)
	{
		if (mode.PaintsFullFace)
		{
			return IsTemporalChalk(itemStack)
				? TemporalMarkingChalkPaintDecorPrefix
				: MarkingChalkPaintDecorPrefix;
		}

		return IsTemporalChalk(itemStack)
			? TemporalMarkingChalkDecorPrefix
			: MarkingChalkDecorPrefix;
	}



	//// Resolves the actual spritesheet cell needed for the selected face.
	////
	//// Wall marks use the tested cells directly.  Floors and ceilings use the
	//// up-arrow cell plus decor rotation so left/right/down modes can be
	//// oriented from the player's viewpoint instead of the world's fixed floor
	//// UV axes.
	////
	private static string? ResolveDecorCellVariant(BlockSelection blockSel, MarkingChalkMode mode)
	{
		if (mode.PaintsFullFace)
		{
			return null;
		}

		return IsFloorOrCeilingFace(blockSel.Face) && mode.ArrowDirection != MarkingChalkArrowDirection.None
			? ArrowUpCellVariant
			: mode.CellVariant;
	}



	//// Resolves the decor face, subcell, and optional rotation for placement.
	////
	//// `BlockSelection.ToDecorIndex()` preserves the clicked subcell but has
	//// no rotation.  For horizontal drawing planes, arrow modes add rotation
	//// bits so the mark points forward, right, back, or left relative to the
	//// player at placement time.
	////
	private static int ResolveDecorIndex(EntityAgent byEntity, BlockSelection blockSel, MarkingChalkMode mode)
	{
		if (mode.PaintsFullFace)
		{
			return new DecorBits(blockSel.Face);
		}

		DecorBits decorBits = new(blockSel.ToDecorIndex());
		if (!IsFloorOrCeilingFace(blockSel.Face) || mode.ArrowDirection == MarkingChalkArrowDirection.None)
		{
			return decorBits;
		}

		BlockFacing playerFacing = ResolvePlayerForwardFacing(byEntity, blockSel);
		BlockFacing desiredFacing = ResolveHorizontalArrowFacing(playerFacing, mode.ArrowDirection);
		decorBits.Rotation = ResolveHorizontalArrowRotation(desiredFacing);

		return decorBits;
	}



	//// Resolves the player's actual horizontal aim direction for horizontal
	//// plane arrow placement.
	////
	//// The selected floor or ceiling point is the most direct evidence of the
	//// camera ray.  Use the horizontal eye-to-hit vector when possible, and
	//// fall back to yaw only for near-vertical clicks where that projection is
	//// too small to classify.
	////
	private static BlockFacing ResolvePlayerForwardFacing(EntityAgent byEntity, BlockSelection blockSel)
	{
		Vec3d eyePosition = byEntity.Pos.XYZ.AddCopy(byEntity.LocalEyePos);
		Vec3d aimVector = blockSel.FullPosition.SubCopy(eyePosition);
		if (aimVector.X * aimVector.X + aimVector.Z * aimVector.Z > 0.0001)
		{
			return BlockFacing.FromVector(aimVector.X, 0.0, aimVector.Z);
		}

		return BlockFacing.HorizontalFromYaw(byEntity.Pos.Yaw).Opposite;
	}



	//// Resolves a tool-mode arrow to a world-facing direction on floors and
	//// ceilings.
	////
	//// These modes are intentionally viewpoint-relative on horizontal planes:
	//// up means away from the player, left means the player's left, and so on.
	////
	private static BlockFacing ResolveHorizontalArrowFacing(
		BlockFacing playerFacing,
		MarkingChalkArrowDirection arrowDirection)
	{
		return arrowDirection switch
		{
			MarkingChalkArrowDirection.Right => playerFacing.GetCW(),
			MarkingChalkArrowDirection.Down => playerFacing.Opposite,
			MarkingChalkArrowDirection.Left => playerFacing.GetCCW(),
			_ => playerFacing
		};
	}



	//// Converts a desired world direction into Vintage Story surfacelayer
	//// rotation bits for a floor or ceiling mark.
	////
	//// Vintage Story stores this as `DecorBits.Rotation` and passes it to the
	//// surfacelayer tesselator as `decorRotationData`.  For horizontal arrow
	//// marks, the working mapping mirrors the desired horizontal angle index.
	//// The tempting `index + 1` formula is identical for east and west, but
	//// reverses north and south.
	////
	private static int ResolveHorizontalArrowRotation(BlockFacing desiredFacing)
	{
		return GameMath.Mod(1 - desiredFacing.HorizontalAngleIndex, 4);
	}



	//// Checks whether the selected face is a floor or ceiling drawing plane.
	////
	private static bool IsFloorOrCeilingFace(BlockFacing face)
	{
		return face == BlockFacing.UP || face == BlockFacing.DOWN;
	}



	//// Finds older marking chalk entries that occupy the same face subcell
	//// with a different rotation.
	////
	//// Decor rotation is part of the storage key.  Without this cleanup,
	//// redrawing a horizontal arrow in another direction could leave two
	//// chalk marks stacked in the same 16x16 decor cell.
	////
	private static List<int>? FindOtherMarkingChalkDecorsAtSubposition(
		IBlockAccessor blockAccessor,
		BlockSelection blockSel,
		int decorIndex)
	{
		Dictionary<int, Block>? decors = blockAccessor.GetSubDecors(blockSel.Position);
		if (decors == null || decors.Count == 0)
		{
			return null;
		}

		DecorBits targetBits = new(decorIndex);
		List<int>? staleDecorIndexes = null;
		foreach ((int existingDecorIndex, Block decorBlock) in decors)
		{
			DecorBits existingBits = new(existingDecorIndex);
			if (existingDecorIndex == decorIndex
			    || existingBits.Face != targetBits.Face
			    || existingBits.SubPosition != targetBits.SubPosition
			    || !IsMarkingChalkDecor(decorBlock))
			{
				continue;
			}

			staleDecorIndexes ??= new List<int>();
			staleDecorIndexes.Add(existingDecorIndex);
		}

		return staleDecorIndexes;
	}



	//// Removes decor entries that were superseded by a successful placement.
	////
	private static void RemoveDecorIndexes(IBlockAccessor blockAccessor, BlockPos position, List<int>? decorIndexes)
	{
		if (decorIndexes == null || decorIndexes.Count == 0)
		{
			return;
		}

		Block airBlock = blockAccessor.GetBlock(0);
		foreach (int decorIndex in decorIndexes)
		{
			blockAccessor.SetDecor(airBlock, position, decorIndex);
		}
	}



	//// Erases the nearest marking chalk decor on the selected face.
	////
	//// Marking chalk uses the same sub-face decor grid as cave art, so normal
	//// face-only decor breaking is too imprecise.  This searches nearby
	//// subcells and removes only Ghaelen Tweaks chalk decor, leaving other
	//// mods' decor and vanilla cave art untouched.
	////
	private static bool TryEraseMark(IBlockAccessor blockAccessor, BlockSelection blockSel, out ErasedMark erasedMark)
	{
		erasedMark = default;

		Dictionary<int, Block>? decors = blockAccessor.GetSubDecors(blockSel.Position);
		if (decors == null || decors.Count == 0)
		{
			return false;
		}

		int targetSubPosition = new DecorBits(blockSel.ToDecorIndex()).SubPosition;
		int bestDistance = int.MaxValue;
		int? bestDecorIndex = null;
		Block? bestDecorBlock = null;
		int? fallbackPaintDecorIndex = null;
		Block? fallbackPaintDecorBlock = null;

		foreach ((int decorIndex, Block decorBlock) in decors)
		{
			DecorBits decorBits = new(decorIndex);
			if (decorBits.Face != blockSel.Face.Index || !IsMarkingChalkDecor(decorBlock))
			{
				continue;
			}

			if (decorBits.SubPosition == 0 && IsPaintMarkingChalkDecor(decorBlock))
			{
				fallbackPaintDecorIndex ??= decorIndex;
				fallbackPaintDecorBlock ??= decorBlock;
				continue;
			}

			int distance = SubpositionGridDistance(targetSubPosition, decorBits.SubPosition);
			if (distance > EraseSearchRadius || distance >= bestDistance)
			{
				continue;
			}

			bestDistance = distance;
			bestDecorIndex = decorIndex;
			bestDecorBlock = decorBlock;
		}

		if (!bestDecorIndex.HasValue && fallbackPaintDecorIndex.HasValue)
		{
			bestDecorIndex = fallbackPaintDecorIndex;
			bestDecorBlock = fallbackPaintDecorBlock;
		}

		if (!bestDecorIndex.HasValue
		    || bestDecorBlock == null
		    || !blockAccessor.BreakDecor(blockSel.Position, blockSel.Face, bestDecorIndex.Value))
		{
			return false;
		}

		erasedMark = new ErasedMark(bestDecorIndex.Value, bestDecorBlock);
		return true;
	}



	//// Checks whether a decor block belongs to this tweak's chalk overlay
	//// family.
	////
	private static bool IsMarkingChalkDecor(Block decorBlock)
	{
		AssetLocation? code = decorBlock.Code;

		return code != null
		       && code.Domain == "ghaelentweaks"
		       && code.Path.StartsWith(MarkingChalkDecorPrefix, StringComparison.Ordinal);
	}



	//// Checks whether a decor block is a temporal chalk mark that should own
	//// a hidden light marker.
	////
	internal static bool IsTemporalMarkingChalkDecor(Block decorBlock)
	{
		AssetLocation? code = decorBlock.Code;
		if (code == null || code.Domain != "ghaelentweaks")
		{
			return false;
		}

		return code.Path.StartsWith(TemporalMarkingChalkDecorPrefix, StringComparison.Ordinal)
		       || code.Path.StartsWith(TemporalMarkingChalkPaintDecorPrefix, StringComparison.Ordinal);
	}



	//// Checks whether a decor block is a full-face chalk paint mark.
	////
	private static bool IsPaintMarkingChalkDecor(Block decorBlock)
	{
		AssetLocation? code = decorBlock.Code;

		return code != null
		       && code.Domain == "ghaelentweaks"
		       && code.Path.StartsWith(MarkingChalkPaintDecorPrefix, StringComparison.Ordinal);
	}



	//// Checks whether a held chalk stack belongs to the temporal item family.
	////
	private static bool IsTemporalChalk(ItemStack itemStack)
	{
		AssetLocation? code = itemStack.Collectible.Code;

		return code != null
		       && code.Domain == "ghaelentweaks"
		       && code.Path.StartsWith(TemporalMarkingChalkItemPrefix, StringComparison.Ordinal);
	}



	//// Gets the color variant on a chalk stack, falling back to white for
	//// malformed or legacy stacks.
	////
	private static string GetChalkColor(ItemStack itemStack)
	{
		return itemStack.Collectible.Variant.TryGetValue("color", out string? colorVariant)
			? colorVariant
			: "white";
	}



	//// Spawns the hidden marker entity that supplies dynamic colored light
	//// for temporal chalk decor.
	////
	private static void SpawnTemporalLightMarker(
		IWorldAccessor world,
		BlockSelection blockSel,
		int decorIndex,
		string color)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}

		RemoveTemporalLightMarker(world, blockSel.Position, decorIndex);

		EntityProperties? entityType = world.GetEntityType(MarkingChalkLightEntityCode);
		if (entityType == null)
		{
			world.Logger.Warning($"Missing marking chalk light entity type {MarkingChalkLightEntityCode}.");
			return;
		}

		Entity entity = world.ClassRegistry.CreateEntity(entityType);
		if (entity is not EntityMarkingChalkLight lightEntity)
		{
			world.Logger.Warning($"Entity type {MarkingChalkLightEntityCode} did not create a chalk light marker.");
			return;
		}

		Vec3d position = ResolveTemporalLightPosition(blockSel);
		lightEntity.Pos.SetPos(position.X, position.Y, position.Z);
		lightEntity.PositionBeforeFalling.Set(position.X, position.Y, position.Z);
		lightEntity.Configure(blockSel.Position, decorIndex, GetTemporalLightHsv(color));
		world.SpawnEntity(lightEntity);
	}



	//// Removes any hidden temporal chalk light marker tied to the given decor
	//// index.
	////
	private static void RemoveTemporalLightMarker(IWorldAccessor world, BlockPos targetPos, int decorIndex)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}

		Vec3d searchPosition = targetPos.ToVec3d().Add(0.5, 0.5, 0.5);
		foreach (Entity entity in world.GetEntitiesAround(
			         searchPosition,
			         2f,
			         2f,
			         candidate => candidate is EntityMarkingChalkLight lightEntity
			                      && lightEntity.Matches(targetPos, decorIndex)))
		{
			entity.Die(EnumDespawnReason.Removed);
		}
	}



	//// Places temporal light just outside the clicked face so the light reads
	//// as coming from the chalk overlay.
	////
	private static Vec3d ResolveTemporalLightPosition(BlockSelection blockSel)
	{
		Vec3i normal = blockSel.Face.Normali;

		return blockSel.FullPosition.AddCopy(
			normal.X * 0.03,
			normal.Y * 0.03,
			normal.Z * 0.03);
	}



	//// Maps marking chalk color names to Vintage Story dynamic-light HSV.
	////
	private static byte[] GetTemporalLightHsv(string color)
	{
		return color switch
		{
			"blue" => new byte[] { 38, 7, TemporalChalkLightLevel },
			"green" => new byte[] { 22, 7, TemporalChalkLightLevel },
			"orange" => new byte[] { 4, 7, TemporalChalkLightLevel },
			"pink" => new byte[] { 54, 5, TemporalChalkLightLevel },
			"purple" => new byte[] { 46, 7, TemporalChalkLightLevel },
			"red" => new byte[] { 0, 7, TemporalChalkLightLevel },
			"yellow" => new byte[] { 10, 7, TemporalChalkLightLevel },
			_ => new byte[] { 0, 0, TemporalChalkLightLevel }
		};
	}



	//// Measures grid-cell distance between two cave-art-style decor
	//// subpositions.
	////
	private static int SubpositionGridDistance(int targetSubPosition, int candidateSubPosition)
	{
		if (targetSubPosition <= 0 || candidateSubPosition <= 0)
		{
			return targetSubPosition == candidateSubPosition ? 0 : int.MaxValue;
		}

		int targetOffset = targetSubPosition - 1;
		int candidateOffset = candidateSubPosition - 1;
		int xDistance = Math.Abs(targetOffset % 16 - candidateOffset % 16);
		int yDistance = Math.Abs(targetOffset / 16 - candidateOffset / 16);

		return Math.Max(xDistance, yDistance);
	}



	//// Checks whether the current interaction should erase instead of draw.
	////
	//// Shift is Vintage Story's separable mouse-interaction modifier, while
	//// Sneak covers players who describe or trigger the gesture as crouching.
	//// The explicit erase tool mode remains available for players who prefer
	//// to select it instead of holding a modifier.
	////
	private static bool ShouldErase(EntityAgent byEntity, ItemStack itemStack)
	{
		return byEntity.Controls.ShiftKey || byEntity.Controls.Sneak || IsEraseMode(itemStack);
	}



	//// Checks whether the selected tool mode should erase instead of draw.
	////
	private static bool IsEraseMode(ItemStack itemStack)
	{
		return ModeDefinitions[GetModeIndex(itemStack)].Erases;
	}



	//// Gets the selected mode stored on a chalk stack.
	////
	private static MarkingChalkMode GetSelectedMode(ItemStack itemStack)
	{
		return ModeDefinitions[GetModeIndex(itemStack)];
	}



	//// Gets the selected mode index stored on a chalk stack.
	////
	private static int GetModeIndex(ItemStack itemStack)
	{
		return GameMath.Clamp(itemStack.Attributes.GetInt(ModeAttribute, 0), 0, ModeDefinitions.Length - 1);
	}



	//// Plays the chalk interaction sound at the selected block hit position.
	////
	private static void PlayChalkSound(EntityAgent byEntity, BlockSelection blockSel, IPlayer byPlayer)
	{
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



	//// Consumes the selected mark's configured use cost from the active
	//// stick.
	////
	//// Fresh sticks stay stackable until one is used.  The first partial use
	//// splits a single stick into the active slot and moves the untouched
	//// remainder elsewhere in the player inventory, so the visible use count
	//// cannot appear to apply to every stick in the original stack.
	////
	private static void ConsumeUses(ItemSlot slot, IPlayer byPlayer, int useCost)
	{
		ItemStack? itemStack = slot.Itemstack;
		if (itemStack == null)
		{
			return;
		}

		int usesLeft = GetUsesLeft(itemStack) - Math.Max(1, useCost);
		if (usesLeft <= 0)
		{
			slot.TakeOut(1);
			slot.Itemstack?.Attributes.RemoveAttribute(UsesLeftAttribute);
			slot.MarkDirty();
			return;
		}

		if (itemStack.StackSize > 1)
		{
			SplitActiveUsedStick(slot, byPlayer, usesLeft);
			return;
		}

		itemStack.Attributes.SetInt(UsesLeftAttribute, usesLeft);
		slot.MarkDirty();
	}



	//// Splits one partially used chalk stick away from the untouched
	//// remainder of a fresh stack.
	////
	//// The active slot keeps the used stick so the player can continue
	//// drawing with the same selected mode.  The untouched remainder is placed
	//// back in player inventory when possible, or dropped near the player when
	//// no inventory slot can accept it.
	////
	private static void SplitActiveUsedStick(ItemSlot slot, IPlayer byPlayer, int usesLeft)
	{
		ItemStack? sourceStack = slot.Itemstack;
		if (sourceStack == null || sourceStack.StackSize <= 1)
		{
			return;
		}

		ItemStack activeStick = sourceStack.Clone();
		activeStick.StackSize = 1;
		activeStick.Attributes.SetInt(UsesLeftAttribute, usesLeft);

		ItemStack untouchedRemainder = sourceStack.Clone();
		untouchedRemainder.StackSize = sourceStack.StackSize - 1;
		untouchedRemainder.Attributes.RemoveAttribute(UsesLeftAttribute);

		slot.Itemstack = activeStick;
		slot.MarkDirty();

		if (!byPlayer.InventoryManager.TryGiveItemstack(untouchedRemainder, true) && untouchedRemainder.StackSize > 0)
		{
			byPlayer.Entity.World.SpawnItemEntity(untouchedRemainder, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
		}

		byPlayer.InventoryManager.BroadcastHotbarSlot();
	}



	//// Restores the erased mark's use cost to the active stick.
	////
	//// The refund is capped at the configured maximum so erasing old marks or
	//// another player's marks cannot overfill a chalk stick.
	////
	private static void RestoreUses(ItemSlot slot, IPlayer byPlayer, int restoredUses)
	{
		ItemStack? itemStack = slot.Itemstack;
		if (itemStack == null)
		{
			return;
		}

		if (!itemStack.Attributes.HasAttribute(UsesLeftAttribute))
		{
			return;
		}

		int maxUses = GetMaxUses();
		int usesLeft = GetUsesLeft(itemStack);
		int restoredUsesLeft = usesLeft + Math.Max(1, restoredUses);
		if (restoredUsesLeft >= maxUses)
		{
			itemStack.Attributes.RemoveAttribute(UsesLeftAttribute);
			slot.MarkDirty();
			return;
		}

		if (itemStack.StackSize > 1)
		{
			SplitActiveUsedStick(slot, byPlayer, restoredUsesLeft);
			return;
		}

		itemStack.Attributes.SetInt(UsesLeftAttribute, restoredUsesLeft);
		slot.MarkDirty();
	}



	//// Gets the use cost for a selected chalk mode.
	////
	//// The cost is capped at the configured maximum so very low-use configs
	//// can still draw every available mark by spending the whole stick.
	////
	private static int GetUseCost(MarkingChalkMode mode)
	{
		return GameMath.Clamp(mode.UseCost, 0, GetMaxUses());
	}



	//// Gets the erase refund for a removed chalk decor block.
	////
	private static int GetRefundUseCost(Block decorBlock)
	{
		return IsPaintMarkingChalkDecor(decorBlock)
			? GameMath.Clamp(FullFacePaintUseCost, 1, GetMaxUses())
			: 1;
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
		string? CellVariant,
		string LangCode,
		string IconPath,
		bool Erases = false,
		MarkingChalkArrowDirection ArrowDirection = MarkingChalkArrowDirection.None,
		bool PaintsFullFace = false,
		int UseCost = 1);

	private readonly record struct ErasedMark(int DecorIndex, Block DecorBlock);

	private enum MarkingChalkArrowDirection
	{
		None,
		Up,
		Right,
		Down,
		Left
	}
}
