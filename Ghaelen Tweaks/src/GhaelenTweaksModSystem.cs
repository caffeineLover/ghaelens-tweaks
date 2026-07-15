/*
 * Provides the main Vintage Story mod-system entry point for Ghaelen Tweaks.
 *
 * This class registers block and entity behaviors, loads and normalizes mod
 * configuration, listens for optional Config Lib setting events, applies the
 * client-only persistent crafting grid Harmony patch, and creates the
 * server-only palisade damage system. Individual features live in dedicated
 * behavior, patch, and system classes.
 */

using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GhaelenTweaks;

public sealed class GhaelenTweaksModSystem : ModSystem
{
	private const string ConfigFileName = "ghaelentweaks.json";
	private const string ConfigLibSettingChangedEvent = "configlib:ghaelentweaks:setting-changed";
	private const string ConfigLibSettingLoadedEvent = "configlib:ghaelentweaks:setting-loaded";
	private PalisadeDamageSystem? palisadeDamageSystem;
	private Harmony? harmony;



	//// Initializes all Ghaelen Tweaks runtime registrations for the current
	//// Vintage Story side.
	////
	//// Vintage Story invokes this once during mod startup. Common behavior
	//// classes and config loading are registered on both sides, while the
	//// persistent crafting-grid patch is client-only and the palisade damage
	//// system is server-only.
	////
	public override void Start(ICoreAPI api)
	{
		base.Start(api);

		// Register behavior classes before config-dependent systems start so
		// asset-declared blocks and entities can resolve their behavior names.
		api.RegisterBlockBehaviorClass("StonePathConversion", typeof(BlockBehaviorStonePathConversion));
		api.RegisterBlockBehaviorClass("PalisadeFirewoodDrops", typeof(BlockBehaviorPalisadeFirewoodDrops));
		api.RegisterBlockBehaviorClass("BarricadeRecyclingDrops", typeof(BlockBehaviorBarricadeRecyclingDrops));
		api.RegisterEntityBehaviorClass("catloreguardian", typeof(EntityBehaviorCatLoreGuardian));

		LoadConfig(api);

		// Config Lib is optional. Listening for its event names is safe when
		// the library is absent because ordinary Vintage Story event bus
		// listeners simply remain unused.
		api.Event.RegisterEventBusListener(OnConfigLibSettingEvent, filterByEventName: ConfigLibSettingChangedEvent);
		api.Event.RegisterEventBusListener(OnConfigLibSettingEvent, filterByEventName: ConfigLibSettingLoadedEvent);

		if (api.Side == EnumAppSide.Client)
		{
			harmony = new Harmony("ghaelentweaks.persistent-crafting-grid");
			PersistentCraftingGridPatches.Apply(harmony, api.Logger);
		}

		if (api.Side == EnumAppSide.Server)
		{
			palisadeDamageSystem = new PalisadeDamageSystem(api);
		}
	}



	//// Releases side-specific runtime resources owned by the mod system.
	////
	//// Vintage Story invokes this during mod unloading. The palisade damage
	//// system unregisters its server tick listener, and Harmony unpatching
	//// removes this mod's client inventory patch by its unique patch id.
	////
	public override void Dispose()
	{
		palisadeDamageSystem?.Dispose();
		palisadeDamageSystem = null;
		harmony?.UnpatchAll("ghaelentweaks.persistent-crafting-grid");
		harmony = null;
		base.Dispose();
	}



	//// Loads the mod configuration from Vintage Story's mod-config storage
	//// and writes back a normalized copy.
	////
	//// Startup calls this before feature systems read configuration. If disk
	//// loading fails or returns no data, the method logs a warning and uses
	//// defaults so the mod can still load with safe built-in behavior.
	////
	private static void LoadConfig(ICoreAPI api)
	{
		try
		{
			GhaelenTweaksConfig.Current =
				api.LoadModConfig<GhaelenTweaksConfig>(ConfigFileName) ?? new GhaelenTweaksConfig();
		}
		catch (Exception exception)
		{
			api.Logger.Warning($"Failed to load Ghaelen Tweaks config. Falling back to defaults: {exception.Message}");
			GhaelenTweaksConfig.Current = new GhaelenTweaksConfig();
		}

		GhaelenTweaksConfig.Current.Normalize();
		api.StoreModConfig(GhaelenTweaksConfig.Current, ConfigFileName);
	}



	//// Applies optional Config Lib setting updates to the active in-memory
	//// configuration.
	////
	//// Config Lib publishes both load and change events through the Vintage
	//// Story event bus. This handler reads only the setting names owned by
	//// this mod, updates matching properties, and normalizes the config after
	//// recognized changes.
	////
	private static void OnConfigLibSettingEvent(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (data is not ITreeAttribute tree)
		{
			return;
		}

		bool changed = true;

		// Config Lib sends each setting update as a key plus a typed value.
		// Keeping the mapping explicit prevents unknown event data from
		// silently mutating the wrong runtime option.
		switch (tree.GetAsString("setting"))
		{
			case "cat-impervious-to-lore-creatures":
				GhaelenTweaksConfig.Current.CatImperviousToLoreCreatures =
					tree.GetBool("value", GhaelenTweaksConfig.Current.CatImperviousToLoreCreatures);
				break;

			case "underground-radius-for-lore-glow":
				GhaelenTweaksConfig.Current.UndergroundRadiusForLoreGlow =
					tree.GetFloat("value", GhaelenTweaksConfig.Current.UndergroundRadiusForLoreGlow);
				break;

			case "underground-radius-for-lore-yowl":
				GhaelenTweaksConfig.Current.UndergroundRadiusForLoreYowl =
					tree.GetFloat("value", GhaelenTweaksConfig.Current.UndergroundRadiusForLoreYowl);
				break;

			case "above-ground-radius-for-lore-glow":
				GhaelenTweaksConfig.Current.AboveGroundRadiusForLoreGlow =
					tree.GetFloat("value", GhaelenTweaksConfig.Current.AboveGroundRadiusForLoreGlow);
				break;

			case "above-ground-radius-for-lore-yowl":
				GhaelenTweaksConfig.Current.AboveGroundRadiusForLoreYowl =
					tree.GetFloat("value", GhaelenTweaksConfig.Current.AboveGroundRadiusForLoreYowl);
				break;

			case "enable-palisade-damage":
				GhaelenTweaksConfig.Current.EnablePalisadeDamage =
					tree.GetBool("value", GhaelenTweaksConfig.Current.EnablePalisadeDamage);
				break;

			case "palisade-damage-amount":
				GhaelenTweaksConfig.Current.PalisadeDamageAmount =
					tree.GetFloat("value", GhaelenTweaksConfig.Current.PalisadeDamageAmount);
				break;

			case "palisade-damage-cooldown-seconds":
				GhaelenTweaksConfig.Current.PalisadeDamageCooldownSeconds =
					tree.GetFloat("value", GhaelenTweaksConfig.Current.PalisadeDamageCooldownSeconds);
				break;

			case "persistent-crafting-grid":
				GhaelenTweaksConfig.Current.PersistentCraftingGrid =
					tree.GetBool("value", GhaelenTweaksConfig.Current.PersistentCraftingGrid);
				break;

			default:
				changed = false;
				break;
		}

		if (changed)
		{
			GhaelenTweaksConfig.Current.Normalize();
		}
	}
}
