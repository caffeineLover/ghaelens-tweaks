/*
 * Provides the main Vintage Story mod-system entry point for Ghaelen Tweaks.
 *
 * This class registers block and entity behaviors, loads and normalizes mod
 * configuration, listens for optional Config Lib setting events, applies
 * Harmony patches, initializes Better Ruins blueprint knowledge sync, and
 * creates the server-only palisade damage system. Individual features live
 * in dedicated behavior, patch, and system classes.
 */

using HarmonyLib;
using GhaelenTweaks.BetterRuins;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace GhaelenTweaks;

public sealed class GhaelenTweaksModSystem : ModSystem
{
	private const string ConfigFileName = "ghaelentweaks.json";
	private const string ConfigLibSettingChangedEvent = "configlib:ghaelentweaks:setting-changed";
	private const string ConfigLibSettingLoadedEvent = "configlib:ghaelentweaks:setting-loaded";
	private const string HarmonyId = "ghaelentweaks";
	private const string TuleHandbasketRecipeName = "tule-handbasket";
	private PalisadeDamageSystem? palisadeDamageSystem;
	private Harmony? harmony;
	private ICoreAPI? api;



	//// Initializes all Ghaelen Tweaks runtime registrations for the current
	//// Vintage Story side.
	////
	//// Vintage Story invokes this once during mod startup. Common behavior
	//// classes, config loading, and the Better Ruins recipe patch are
	//// registered on both sides, while the persistent crafting-grid patch is
	//// client-only and the palisade damage system is server-only.
	////
	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;

		// Register behavior classes before config-dependent systems start so
		// asset-declared blocks and entities can resolve their behavior names.
		api.RegisterBlockBehaviorClass("StonePathConversion", typeof(BlockBehaviorStonePathConversion));
		api.RegisterBlockBehaviorClass("PalisadeFirewoodDrops", typeof(BlockBehaviorPalisadeFirewoodDrops));
		api.RegisterBlockBehaviorClass(
			"DisplayCaseStackingSupport",
			typeof(BlockBehaviorDisplayCaseStackingSupport));
		api.RegisterCollectibleBehaviorClass(
			"BetterRuinsBlueprintReading",
			typeof(CollectibleBehaviorBetterRuinsBlueprintReading));
		api.RegisterItemClass("ItemMarkingChalk", typeof(ItemMarkingChalk));
		api.RegisterEntity("EntityMarkingChalkLight", typeof(EntityMarkingChalkLight));
		api.RegisterEntityBehaviorClass("catloreguardian", typeof(EntityBehaviorCatLoreGuardian));

		LoadConfig(api);

		// Config Lib is optional. Listening for its event names is safe when
		// the library is absent because ordinary Vintage Story event bus
		// listeners simply remain unused.
		api.Event.RegisterEventBusListener(OnConfigLibSettingEvent, filterByEventName: ConfigLibSettingChangedEvent);
		api.Event.RegisterEventBusListener(OnConfigLibSettingEvent, filterByEventName: ConfigLibSettingLoadedEvent);

		harmony = new Harmony(HarmonyId);
		BetterRuinsBlueprintRecipePatches.Apply(harmony, api.Logger);
		ClutterFuelPatches.Apply(harmony, api.Logger);

		if (api.Side == EnumAppSide.Client)
		{
			ClutterFuelHandbookPatches.Apply(harmony, api.Logger);
			PersistentCraftingGridPatches.Apply(harmony, api.Logger);
		}

		if (api.Side == EnumAppSide.Server)
		{
			palisadeDamageSystem = new PalisadeDamageSystem(api);
		}
	}



	//// Initializes client-only runtime integrations after common startup.
	////
	//// Vintage Story invokes this only for the game client. The Better Ruins
	//// blueprint knowledge channel receives the server-owned learned
	//// schematic set used by client-side crafting preview and tooltip text.
	////
	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);

		BetterRuinsBlueprintKnowledge.StartClientSide(api);
	}



	//// Initializes server-only runtime integrations after common startup.
	////
	//// Vintage Story invokes this only for the game server. The Better Ruins
	//// blueprint knowledge channel sends each player their own persisted
	//// schematic set and resends it whenever a new schematic is learned.
	////
	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);

		GhaelenTweaksChatCommands.StartServerSide(api);
		BetterRuinsBlueprintKnowledge.StartServerSide(api);
	}



	//// Applies configuration-dependent recipe state after Vintage Story has
	//// loaded and resolved recipes.
	////
	//// The tule handbasket and marking chalk recipes are declared as normal
	//// JSON content so they can use the vanilla recipe loader. Once recipes
	//// exist in the world registry, this hook applies the mod config to the
	//// recipe runtime state.
	////
	public override void AssetsFinalize(ICoreAPI api)
	{
		base.AssetsFinalize(api);

		ApplyTuleHandbasketRecipeSetting(api);
		MarkingChalkRecipeSettings.Apply(api);
	}



	//// Releases side-specific runtime resources owned by the mod system.
	////
	//// Vintage Story invokes this during mod unloading. The palisade damage
	//// system unregisters its server tick listener, blueprint sync releases
	//// its player event hook, and Harmony unpatching removes this mod's
	//// recipe and client inventory patches by their shared patch id.
	////
	public override void Dispose()
	{
		palisadeDamageSystem?.Dispose();
		palisadeDamageSystem = null;
		BetterRuinsBlueprintKnowledge.Dispose();
		harmony?.UnpatchAll(HarmonyId);
		harmony = null;
		this.api = null;
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
	private void OnConfigLibSettingEvent(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (data is not ITreeAttribute tree)
		{
			return;
		}

		bool changed = true;

		string settingCode = tree.GetAsString("setting");

		// Config Lib sends each setting update as a key plus a typed value.
		// Keeping the mapping explicit prevents unknown event data from
		// silently mutating the wrong runtime option.
		switch (settingCode)
		{
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

			case "enable-palisade-damage-to-hostiles":
				GhaelenTweaksConfig.Current.EnablePalisadeDamageToHostiles =
					tree.GetBool("value", GhaelenTweaksConfig.Current.EnablePalisadeDamageToHostiles);
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

			case "tule-handbasket":
				GhaelenTweaksConfig.Current.TuleHandbasket =
					tree.GetBool("value", GhaelenTweaksConfig.Current.TuleHandbasket);
				break;

			case "betterruins-blueprint-learning":
				GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning =
					tree.GetBool("value", GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning);
				break;

			case "display-case-stacking":
				GhaelenTweaksConfig.Current.DisplayCaseStacking =
					tree.GetBool("value", GhaelenTweaksConfig.Current.DisplayCaseStacking);
				break;

			case "marking-chalk-uses":
				GhaelenTweaksConfig.Current.MarkingChalkUses =
					tree.GetInt("value", GhaelenTweaksConfig.Current.MarkingChalkUses);
				break;

			case "marking-chalk-dye-batch-size":
				GhaelenTweaksConfig.Current.MarkingChalkDyeBatchSize =
					tree.GetInt("value", GhaelenTweaksConfig.Current.MarkingChalkDyeBatchSize);
				break;

			case "temporal-marking-chalk-light-level":
				GhaelenTweaksConfig.Current.TemporalMarkingChalkLightLevel =
					tree.GetInt("value", GhaelenTweaksConfig.Current.TemporalMarkingChalkLightLevel);
				break;

			default:
				changed = false;
				break;
		}

		if (changed)
		{
			GhaelenTweaksConfig.Current.Normalize();

			if (api != null)
			{
				ApplyTuleHandbasketRecipeSetting(api);
				MarkingChalkRecipeSettings.Apply(api);

				if (settingCode == "betterruins-blueprint-learning")
				{
					BetterRuinsBlueprintKnowledge.SyncAllOnlinePlayers();
				}
			}
		}
	}



	//// Applies the active tule handbasket configuration value to the patched
	//// grid recipe.
	////
	//// Vintage Story's crafting grid checks each candidate GridRecipe's
	//// Enabled flag before matching, so toggling that flag is enough to make
	//// the recipe available or unavailable without rebuilding the recipe
	//// registry or fast-search ingredient cache.
	////
	private static void ApplyTuleHandbasketRecipeSetting(ICoreAPI api)
	{
		foreach (GridRecipe recipe in api.World.GridRecipes)
		{
			if (recipe.Name?.Path != TuleHandbasketRecipeName)
			{
				continue;
			}

			recipe.Enabled = GhaelenTweaksConfig.Current.TuleHandbasket;
		}
	}
}
