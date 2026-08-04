/*
 * Keeps dynamically configured clutter fuels out of generic handbook fuel
 * lists.
 *
 * Runtime firepit logic can ask `GetCombustibleProperties` with the full
 * item stack, but some handbook integrations treat the fuel list as static
 * per collectible. `game:clutter` fuel values depend on the stack's `type`
 * attribute, so this compatibility patch filters those dynamic entries out
 * before handbook code and other handbook patches inspect the list.
 */

using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;

namespace GhaelenTweaks;

internal static class ClutterFuelHandbookPatches
{
	private const string HandbookBehaviorTypeName =
		"Vintagestory.GameContent.CollectibleBehaviorHandbookTextAndExtraInfo";
	private const string AddCreatedByInfoMethodName = "addCreatedByInfo";
	private const string AddProcessesIntoInfoMethodName = "addProcessesIntoInfo";



	//// Applies the client-side handbook fuel-list compatibility patch.
	////
	//// The target type lives in the survival mod assembly rather than the
	//// public API assembly, so reflection keeps this project from taking a
	//// compile-time dependency on VSSurvivalMod.dll.
	////
	internal static void Apply(Harmony harmony, ILogger patchLogger)
	{
		try
		{
			Type? handbookBehaviorType = AccessTools.TypeByName(HandbookBehaviorTypeName);
			MethodInfo? createdByMethod = handbookBehaviorType == null
				? null
				: AccessTools.Method(handbookBehaviorType, AddCreatedByInfoMethodName);
			MethodInfo? processesIntoMethod = handbookBehaviorType == null
				? null
				: AccessTools.Method(handbookBehaviorType, AddProcessesIntoInfoMethodName);
			MethodInfo? prefixMethod = AccessTools.Method(
				typeof(ClutterFuelHandbookPatches),
				nameof(PrefixFilterDynamicClutterFuels));

			if (createdByMethod == null || processesIntoMethod == null || prefixMethod == null)
			{
				patchLogger.Warning("Clutter fuel handbook compatibility patch target was not found.");
				return;
			}

			harmony.Patch(createdByMethod, prefix: new HarmonyMethod(prefixMethod));
			harmony.Patch(processesIntoMethod, prefix: new HarmonyMethod(prefixMethod));
			patchLogger.Notification("Clutter fuel handbook compatibility feature initialized.");
		}
		catch (Exception exception)
		{
			patchLogger.Warning(
				$"Clutter fuel handbook compatibility feature could not patch handbook fuel lists: {exception.Message}");
		}
	}



	//// Removes dynamic clutter fuel stacks from the handbook fuel list before
	//// vanilla and compatibility patches inspect that list.
	////
	//// Both vanilla handbook relationship methods pass a parameter named
	//// `fuels`, so one Harmony prefix can sanitize the shared list before
	//// A Culinary Artillery inspects it for simmering recipes.
	////
	private static void PrefixFilterDynamicClutterFuels(List<ItemStack>? fuels)
	{
		ClutterFuelPatches.RemoveDynamicClutterFuelStacks(fuels);
	}
}
