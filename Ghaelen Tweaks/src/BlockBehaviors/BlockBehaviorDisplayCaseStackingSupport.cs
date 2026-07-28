/*
 * Implements the narrow attachment rule that lets vanilla display cases
 * support another display case above them.
 *
 * Vanilla display cases are non-solid on every side, and their
 * UnstableFalling behavior asks the block below whether its top face can
 * support the new case. This behavior answers only that specific question
 * and deliberately avoids making display cases generally solid support for
 * unrelated blocks.
 */

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

public sealed class BlockBehaviorDisplayCaseStackingSupport : BlockBehavior
{
	private const string DisplayCaseCodePrefix = "displaycase-";
	private const string TallDisplayCaseCodePrefix = "talldisplaycase-";



	//// Creates the display-case stacking behavior for a block instance
	//// supplied by Vintage Story's asset behavior system.
	////
	//// Construction does not resolve any global block references. The
	//// attachment decision uses the block passed by the current placement
	//// check so it works across ordinary and aged display-case variants.
	////
	public BlockBehaviorDisplayCaseStackingSupport(Block block)
		: base(block)
	{
	}



	//// Allows another display case to attach to the top face of this display
	//// case when the Ghaelen Tweaks setting is enabled.
	////
	//// `BlockBehaviorUnstableFalling` calls this through the lower block when
	//// a player places an unstable falling block. Returning true here gives
	//// display cases just enough support to stack without changing their
	//// collision boxes, side solidity, or support behavior for other blocks.
	////
	public override bool CanAttachBlockAt(
		IBlockAccessor world,
		Block attachingBlock,
		BlockPos pos,
		BlockFacing blockFace,
		ref EnumHandling handling,
		Cuboidi? attachmentArea = null)
	{
		if (!GhaelenTweaksConfig.Current.DisplayCaseStacking)
		{
			return false;
		}

		if (blockFace != BlockFacing.UP || !IsDisplayCase(attachingBlock))
		{
			return false;
		}

		// Claim only this display-case-on-display-case support check. Other
		// attachment requests still fall through to vanilla side-solid logic.
		handling = EnumHandling.PreventDefault;
		return true;
	}



	//// Identifies vanilla display-case block variants by their resolved block
	//// code path.
	////
	//// The variants are loaded from `displaycase.json` and
	//// `displaycase-tall.json`, which produce paths such as
	//// `displaycase-generic` and `talldisplaycase-aged1`.
	////
	private static bool IsDisplayCase(Block? candidateBlock)
	{
		string? path = candidateBlock?.Code?.Path;
		if (path == null)
		{
			return false;
		}

		return path.StartsWith(DisplayCaseCodePrefix, StringComparison.Ordinal)
		       || path.StartsWith(TallDisplayCaseCodePrefix, StringComparison.Ordinal);
	}
}
