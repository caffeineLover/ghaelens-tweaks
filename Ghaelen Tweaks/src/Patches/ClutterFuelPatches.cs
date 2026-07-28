/*
 * Adds firepit fuel properties for selected clutter item-stack variants.
 *
 * Vintage Story stores the specific clutter shape, such as barricade1, on the
 * item stack rather than in the block code. This patch keeps fuel handling
 * narrow to configured clutter types without making every game:clutter item
 * burnable.
 */

using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GhaelenTweaks;

internal static class ClutterFuelPatches
{
	private const string ClutterDomain = "game";
	private const string ClutterPath = "clutter";

	private static readonly ClutterFuelDefinition[] FuelDefinitions =
	{
		new(
			"barricade",
			new CombustibleProperties
			{
				BurnTemperature = 700,
				BurnDuration = 24
			})
	};



	//// Applies the clutter fuel lookup patch.
	////
	//// The main mod system calls this on both client and server. The patch is
	//// read-only and affects only combustible-property lookup for item stacks.
	internal static void Apply(Harmony harmony, ILogger patchLogger)
	{
		try
		{
			MethodInfo? targetMethod = AccessTools.Method(
				typeof(CollectibleObject),
				nameof(CollectibleObject.GetCombustibleProperties),
				new[] { typeof(IWorldAccessor), typeof(ItemStack), typeof(BlockPos) });

			MethodInfo? postfixMethod = AccessTools.Method(
				typeof(ClutterFuelPatches),
				nameof(PostfixGetCombustibleProperties));

			if (targetMethod == null || postfixMethod == null)
			{
				patchLogger.Warning("Clutter fuel patch target was not found.");
				return;
			}

			harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
			patchLogger.Notification("Clutter fuel feature initialized.");
		}
		catch (Exception exception)
		{
			patchLogger.Warning($"Clutter fuel feature could not patch combustible-property lookup: {exception.Message}");
		}
	}



	//// Supplies fuel properties for configured game:clutter item-stack types
	//// when vanilla does not already define combustible properties.
	private static void PostfixGetCombustibleProperties(
		CollectibleObject __instance,
		ItemStack itemstack,
		ref CombustibleProperties __result)
	{
		if (__result != null || itemstack == null)
		{
			return;
		}

		AssetLocation? code = __instance.Code;
		if (code?.Domain != ClutterDomain || code.Path != ClutterPath)
		{
			return;
		}

		string? clutterType = itemstack.Attributes.GetString("type");
		if (clutterType == null)
		{
			return;
		}

		foreach (ClutterFuelDefinition definition in FuelDefinitions)
		{
			if (definition.Matches(clutterType))
			{
				__result = definition.Properties.Clone();
				return;
			}
		}
	}



	private sealed class ClutterFuelDefinition
	{
		public ClutterFuelDefinition(string typePrefix, CombustibleProperties properties)
		{
			TypePrefix = typePrefix;
			Properties = properties;
		}



		public string TypePrefix { get; }



		public CombustibleProperties Properties { get; }



		public bool Matches(string clutterType)
		{
			return clutterType.StartsWith(TypePrefix, StringComparison.Ordinal);
		}
	}
}
