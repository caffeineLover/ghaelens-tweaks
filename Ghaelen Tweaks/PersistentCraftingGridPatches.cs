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

	// Verified against Vintage Story 1.22.3:
	// Target: Vintagestory.Client.NoObf.GuiDialogInventory.OnGuiClosed().
	// Vanilla survival close behavior first transfers every non-empty crafting-grid slot to ordinary player
	// inventory, drops leftovers, then closes the crafting/backpack inventories and runs slot-grid GUI cleanup.
	// This patch skips only that transfer/drop evacuation block. The vanilla Close() packets and OnGuiClosed()
	// calls for the crafting grid, output slot, and backpack still run.
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

		List<Label> movedLabels = codes[evacuationStartIndex].labels;
		codes[evacuationStartIndex].labels = new List<Label>();

		codes.InsertRange(evacuationStartIndex, new[]
		{
			new CodeInstruction(OpCodes.Call, isEnabledMethod).WithLabels(movedLabels),
			new CodeInstruction(OpCodes.Brtrue, skipEvacuation)
		});

		return codes;
	}

	private static bool IsPersistentCraftingGridEnabled()
	{
		return GhaelenTweaksConfig.Current.PersistentCraftingGrid;
	}

	private static bool IsDropAllInventoryItemsCall(CodeInstruction instruction)
	{
		return instruction.opcode == OpCodes.Callvirt
			&& instruction.operand is MethodInfo method
			&& method.Name == nameof(IPlayerInventoryManager.DropAllInventoryItems)
			&& method.GetParameters().Length == 1
			&& method.GetParameters()[0].ParameterType == typeof(IInventory);
	}

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
