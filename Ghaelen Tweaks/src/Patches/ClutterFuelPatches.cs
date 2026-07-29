/*
 * Adds firepit fuel properties for selected clutter item-stack variants.
 *
 * Vintage Story stores the specific clutter shape, such as barricade1 or
 * rubble-wood1, on the item stack rather than in the block code. This patch
 * keeps fuel handling narrow to configured wooden clutter types without making
 * every game:clutter item burnable.
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
	private const int AgedWoodBurnTemperature = 700;
	private const int AgedFirewoodBurnDurationSeconds = 24;

	// Direct firepit burn duration is scaled from the same aged firewood
	// recovery value used by the axe recycling recipes.
	private static readonly ClutterFuelDefinition[] FuelDefinitions =
	{
		new("barricade", recoveredFirewood: 4),
		new("rubble-wood", recoveredFirewood: 2),
		new("table-ruined", recoveredFirewood: 4),
		new("crate/crate-small-stacked", recoveredFirewood: 6),
		new("crate/crate-large-rot", recoveredFirewood: 6),
		new("chestrubble", recoveredFirewood: 3)
	};



	//// Applies the clutter fuel lookup patch.
	////
	//// The main mod system calls this on both client and server. The patch is
	//// read-only and affects only combustible-property lookup for item stacks.
	////
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
	////
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



		//// Creates one fuel rule for a family of wooden clutter item-stack
		//// variants.
		////
		//// The type prefix is matched against the clutter `type` stack
		//// attribute. The recovered firewood count deliberately mirrors the
		//// axe recipe quantity so direct firepit burning and craft-then-burn
		//// use the same total fuel value.
		////
		public ClutterFuelDefinition(string typePrefix, int recoveredFirewood)
		{
			TypePrefix = typePrefix;
			Properties = new CombustibleProperties
			{
				BurnTemperature = AgedWoodBurnTemperature,
				BurnDuration = AgedFirewoodBurnDurationSeconds * recoveredFirewood
			};
		}



		public string TypePrefix { get; }



		public CombustibleProperties Properties { get; }



		//// Returns whether a clutter stack's `type` attribute belongs to this
		//// fuel rule.
		////
		//// Prefix matching keeps variant families compact, while still
		//// requiring the outer patch to verify that the stack is game:clutter.
		////
		public bool Matches(string clutterType)
		{
			return clutterType.StartsWith(TypePrefix, StringComparison.Ordinal);
		}



	}
}
