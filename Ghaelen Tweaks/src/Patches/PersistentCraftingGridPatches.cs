/*
 * Applies the client-side Harmony patch that keeps player crafting-grid
 * ingredients in place when the inventory GUI closes.
 *
 * The patch targets Vintage Story's vanilla inventory dialog close method and
 * skips only the crafting-grid evacuation loop when the feature is enabled.
 * Vanilla inventory close calls, GUI cleanup, network packets, and output-slot
 * recalculation remain owned by the base game.
 */

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;

namespace GhaelenTweaks;

internal static class PersistentCraftingGridPatches
{
	private const string TargetTypeName = "Vintagestory.Client.NoObf.GuiDialogInventory";
	private const string TargetMethodName = "OnGuiClosed";
	private static ILogger? logger;



	//// Applies the persistent crafting-grid transpiler to the vanilla
	//// inventory dialog close method.
	////
	//// The main mod system calls this only on the client side during startup.
	//// The method resolves targets by name because the patched GUI type lives
	//// in the client assembly, and logs a warning instead of throwing when the
	//// expected method shape is unavailable.
	////
	internal static void Apply(Harmony harmony, ILogger patchLogger)
	{
		logger = patchLogger;

		try
		{
			Type? targetType = AccessTools.TypeByName(TargetTypeName);
			if (targetType == null)
			{
				patchLogger.Warning(
					$"Persistent crafting-grid patch target was not found: {TargetTypeName}.{TargetMethodName}.");
				return;
			}

			MethodInfo? targetMethod = AccessTools.Method(targetType, TargetMethodName, Type.EmptyTypes);
			if (targetMethod == null)
			{
				patchLogger.Warning(
					$"Persistent crafting-grid patch target was not found: {TargetTypeName}.{TargetMethodName}().");
				return;
			}

			MethodInfo? transpiler = AccessTools.Method(
				typeof(PersistentCraftingGridPatches),
				nameof(TranspileGuiDialogInventoryOnGuiClosed));
			if (transpiler == null)
			{
				patchLogger.Warning("Persistent crafting-grid transpiler method was not found.");
				return;
			}

			harmony.Patch(targetMethod, transpiler: new HarmonyMethod(transpiler));
			patchLogger.Notification("Persistent crafting-grid feature initialized.");
		}
		catch (Exception exception)
		{
			patchLogger.Warning(
				$"Persistent crafting-grid feature could not patch {TargetTypeName}.{TargetMethodName}(): {exception.Message}");
		}
	}



	//// Rewrites `GuiDialogInventory.OnGuiClosed()` so the crafting-grid
	//// evacuation loop is skipped when the feature is enabled.
	////
	//// Verified against Vintage Story 1.22.3. Vanilla survival close behavior
	//// first transfers every non-empty crafting-grid slot to ordinary player
	//// inventory, drops leftovers, then closes the crafting and backpack
	//// inventories and runs slot-grid GUI cleanup. This transpiler skips only
	//// that transfer/drop evacuation block.
	////
	private static IEnumerable<CodeInstruction> TranspileGuiDialogInventoryOnGuiClosed(
		IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		List<CodeInstruction> codes = instructions.ToList();
		int dropCallIndex = codes.FindIndex(IsDropAllInventoryItemsCall);
		if (dropCallIndex < 0)
		{
			logger?.Warning(
				"Persistent crafting-grid patch could not find IPlayerInventoryManager.DropAllInventoryItems in GuiDialogInventory.OnGuiClosed().");
			return codes;
		}

		int getEnumeratorIndex = FindPreviousCall(codes, dropCallIndex, "GetEnumerator");
		if (getEnumeratorIndex < 0)
		{
			logger?.Warning(
				"Persistent crafting-grid patch could not find the crafting-grid evacuation loop in GuiDialogInventory.OnGuiClosed().");
			return codes;
		}

		int evacuationStartIndex = FindPreviousCraftingInventoryLoad(codes, getEnumeratorIndex);
		if (evacuationStartIndex < 0)
		{
			logger?.Warning(
				"Persistent crafting-grid patch could not find the craftingInv field load in GuiDialogInventory.OnGuiClosed().");
			return codes;
		}

		int skipTargetIndex = dropCallIndex + 1;
		if (skipTargetIndex >= codes.Count)
		{
			logger?.Warning(
				"Persistent crafting-grid patch found DropAllInventoryItems but no following close instructions.");
			return codes;
		}

		MethodInfo? isEnabledMethod = AccessTools.Method(
			typeof(PersistentCraftingGridPatches),
			nameof(IsPersistentCraftingGridEnabled));
		if (isEnabledMethod == null)
		{
			logger?.Warning("Persistent crafting-grid config check method was not found.");
			return codes;
		}

		Label skipEvacuation = generator.DefineLabel();
		codes[skipTargetIndex].labels.Add(skipEvacuation);

		// Preserve labels that originally pointed at the first evacuation
		// instruction by moving them to the injected config check.
		List<Label> movedLabels = codes[evacuationStartIndex].labels;
		codes[evacuationStartIndex].labels = new List<Label>();

		codes.InsertRange(evacuationStartIndex, new[]
		{
			new CodeInstruction(OpCodes.Call, isEnabledMethod).WithLabels(movedLabels),
			new CodeInstruction(OpCodes.Brtrue, skipEvacuation)
		});

		return codes;
	}



	//// Reads the active config flag used by the injected Harmony branch.
	////
	//// The transpiler emits a call to this method so the feature can be
	//// enabled or disabled at runtime through the normal configuration path
	//// without needing to unpatch and repatch the GUI method.
	////
	private static bool IsPersistentCraftingGridEnabled()
	{
		return GhaelenTweaksConfig.Current.PersistentCraftingGrid;
	}



	//// Identifies the vanilla call that drops leftover crafting-grid items
	//// during inventory close.
	////
	//// The transpiler uses this call as the end marker for the evacuation
	//// block because it is the behavior the feature must skip to preserve
	//// crafting-grid ingredients.
	////
	private static bool IsDropAllInventoryItemsCall(CodeInstruction instruction)
	{
		return instruction.opcode == OpCodes.Callvirt
			&& instruction.operand is MethodInfo method
			&& method.Name == nameof(IPlayerInventoryManager.DropAllInventoryItems)
			&& method.GetParameters().Length == 1
			&& method.GetParameters()[0].ParameterType == typeof(IInventory);
	}



	//// Searches backward for a method call with the supplied name.
	////
	//// The transpiler uses this to find the enumerator call that begins the
	//// vanilla crafting-grid slot loop before locating the field load that
	//// starts the entire evacuation block.
	////
	private static int FindPreviousCall(List<CodeInstruction> codes, int startIndex, string methodName)
	{
		for (int index = startIndex - 1; index >= 0; index--)
		{
			if (codes[index].operand is MethodInfo method && method.Name == methodName)
			{
				return index;
			}
		}

		return -1;
	}



	//// Locates the instruction that loads the vanilla crafting inventory
	//// before the close-time evacuation loop.
	////
	//// When the field load is preceded by `ldarg.0`, the returned index
	//// includes that receiver load so the injected branch skips the whole
	//// object-access sequence rather than leaving an unmatched stack value.
	////
	private static int FindPreviousCraftingInventoryLoad(List<CodeInstruction> codes, int getEnumeratorIndex)
	{
		for (int index = getEnumeratorIndex - 1; index >= 0; index--)
		{
			if (codes[index].operand is FieldInfo field && field.Name == "craftingInv")
			{
				return index > 0 && codes[index - 1].opcode == OpCodes.Ldarg_0 ? index - 1 : index;
			}
		}

		return -1;
	}
}
