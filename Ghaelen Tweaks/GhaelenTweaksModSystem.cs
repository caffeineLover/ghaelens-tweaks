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

	public override void Start(ICoreAPI api)
	{
		base.Start(api);

		api.RegisterBlockBehaviorClass("StonePathConversion", typeof(BlockBehaviorStonePathConversion));
		api.RegisterBlockBehaviorClass("PalisadeFirewoodDrops", typeof(BlockBehaviorPalisadeFirewoodDrops));
		api.RegisterBlockBehaviorClass("BarricadeRecyclingDrops", typeof(BlockBehaviorBarricadeRecyclingDrops));
		api.RegisterEntityBehaviorClass("catloreguardian", typeof(EntityBehaviorCatLoreGuardian));

		LoadConfig(api);

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



	public override void Dispose()
	{
		palisadeDamageSystem?.Dispose();
		palisadeDamageSystem = null;
		harmony?.UnpatchAll("ghaelentweaks.persistent-crafting-grid");
		harmony = null;
		base.Dispose();
	}



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



	private static void OnConfigLibSettingEvent(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (data is not ITreeAttribute tree)
		{
			return;
		}

		bool changed = true;
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