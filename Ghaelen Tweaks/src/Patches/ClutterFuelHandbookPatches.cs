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
	private const string AcaHandbookInfoExtensionsTypeName = "ACulinaryArtillery.Util.HandbookInfoExtensions";
	private const string AddCreatedByInfoMethodName = "addCreatedByInfo";
	private const string AddProcessesIntoInfoMethodName = "addProcessesIntoInfo";
	private const string AcaGetCanSimmerMethodName = "getCanSimmer";



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
			MethodInfo? prefixMethod = AccessTools.Method(
				typeof(ClutterFuelHandbookPatches),
				nameof(PrefixFilterDynamicClutterFuels));

			if (prefixMethod == null)
			{
				patchLogger.Warning("Clutter fuel handbook compatibility patch target was not found.");
				return;
			}

			PatchVanillaHandbookFuelLists(harmony, patchLogger, prefixMethod);
			PatchAcaCanSimmerFuelList(harmony, patchLogger, prefixMethod);
		}
		catch (Exception exception)
		{
			patchLogger.Warning(
				$"Clutter fuel handbook compatibility feature could not patch handbook fuel lists: {exception.Message}");
		}
	}



	//// Patches the vanilla handbook relationship methods that receive the
	//// shared fuel list built from all known item stacks.
	////
	//// This is the broad compatibility boundary for vanilla and most
	//// handbook integrations. The prefix uses first priority so the list is
	//// filtered before other prefixes inspect the same parameter.
	////
	private static void PatchVanillaHandbookFuelLists(
		Harmony harmony,
		ILogger patchLogger,
		MethodInfo prefixMethod)
	{
		Type? handbookBehaviorType = AccessTools.TypeByName(HandbookBehaviorTypeName);
		MethodInfo? createdByMethod = handbookBehaviorType == null
			? null
			: AccessTools.Method(handbookBehaviorType, AddCreatedByInfoMethodName);
		MethodInfo? processesIntoMethod = handbookBehaviorType == null
			? null
			: AccessTools.Method(handbookBehaviorType, AddProcessesIntoInfoMethodName);

		if (createdByMethod == null || processesIntoMethod == null)
		{
			patchLogger.Warning("Clutter fuel handbook compatibility patch target was not found.");
			return;
		}

		harmony.Patch(createdByMethod, prefix: CreateFuelSanitizerHarmonyMethod(prefixMethod));
		harmony.Patch(processesIntoMethod, prefix: CreateFuelSanitizerHarmonyMethod(prefixMethod));
		patchLogger.Notification("Clutter fuel handbook compatibility feature initialized.");
	}



	//// Patches A Culinary Artillery's simmer-capability helper when that mod
	//// is loaded in the same client.
	////
	//// ACA can call this helper from its own prefix before this mod's vanilla
	//// handbook prefix has a chance to run. Sanitizing at the helper boundary
	//// removes that patch-order dependency without taking a compile-time
	//// dependency on ACA's assembly.
	////
	private static void PatchAcaCanSimmerFuelList(
		Harmony harmony,
		ILogger patchLogger,
		MethodInfo prefixMethod)
	{
		Type? acaHandbookInfoExtensionsType = AccessTools.TypeByName(AcaHandbookInfoExtensionsTypeName);
		if (acaHandbookInfoExtensionsType == null)
		{
			return;
		}

		MethodInfo? acaCanSimmerMethod = AccessTools.Method(
			acaHandbookInfoExtensionsType,
			AcaGetCanSimmerMethodName,
			new[] { typeof(List<ItemStack>), typeof(ItemStack) });

		if (acaCanSimmerMethod == null)
		{
			patchLogger.Warning("A Culinary Artillery simmer handbook compatibility patch target was not found.");
			return;
		}

		harmony.Patch(acaCanSimmerMethod, prefix: CreateFuelSanitizerHarmonyMethod(prefixMethod));
		patchLogger.Notification("A Culinary Artillery simmer handbook compatibility feature initialized.");
	}



	//// Creates a Harmony prefix descriptor that runs before ordinary
	//// same-method prefixes.
	////
	//// The compatibility patch must sanitize shared mutable fuel lists before
	//// other handbook patches sort or dereference their static combustible
	//// properties.
	////
	private static HarmonyMethod CreateFuelSanitizerHarmonyMethod(MethodInfo prefixMethod)
	{
		return new HarmonyMethod(prefixMethod)
		{
			priority = Priority.First
		};
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
