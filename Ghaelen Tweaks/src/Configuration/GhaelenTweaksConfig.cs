/*
 * Defines the serialized configuration contract for Ghaelen Tweaks.
 *
 * The main mod system loads this model from `ghaelentweaks.json`, normalizes
 * it after disk loading and Config Lib updates, and stores the normalized
 * value back through Vintage Story's mod-config API. Runtime systems read the
 * static Current instance as the active in-memory configuration.
 */

using Newtonsoft.Json;

namespace GhaelenTweaks;

public sealed class GhaelenTweaksConfig
{
	//// Holds the active configuration instance used by runtime systems.
	////
	//// The mod system replaces this value during startup and after config
	//// loading failures. It defaults to a fully usable configuration so
	//// systems have sensible values before disk loading completes.
	////
	public static GhaelenTweaksConfig Current { get; set; } = new();

	//// Sets the underground lore-creature detection radius used for cat glow.
	////
	[JsonProperty("underground-radius-for-lore-glow")]
	public float UndergroundRadiusForLoreGlow { get; set; } = 8f;

	//// Sets the underground lore-creature detection radius used for cat yowls.
	////
	[JsonProperty("underground-radius-for-lore-yowl")]
	public float UndergroundRadiusForLoreYowl { get; set; } = 8f;

	//// Sets the above-ground lore-creature detection radius used for cat glow.
	////
	[JsonProperty("above-ground-radius-for-lore-glow")]
	public float AboveGroundRadiusForLoreGlow { get; set; } = 16f;

	//// Sets the above-ground lore-creature detection radius used for cat yowls.
	////
	[JsonProperty("above-ground-radius-for-lore-yowl")]
	public float AboveGroundRadiusForLoreYowl { get; set; } = 16f;

	//// Controls whether eligible hostile creatures take palisade damage.
	////
	[JsonProperty("enable-palisade-damage-to-hostiles")]
	public bool EnablePalisadeDamageToHostiles { get; set; } = true;

	//// Sets the damage applied by each eligible palisade damage pulse.
	////
	[JsonProperty("palisade-damage-amount")]
	public float PalisadeDamageAmount { get; set; } = 1f;

	//// Sets the per-entity cooldown between palisade damage pulses.
	////
	[JsonProperty("palisade-damage-cooldown-seconds")]
	public float PalisadeDamageCooldownSeconds { get; set; } = 5f;

	//// Controls whether the client Harmony patch preserves player crafting
	//// grid ingredients when the inventory GUI closes.
	////
	[JsonProperty("persistent-crafting-grid")]
	public bool PersistentCraftingGrid { get; set; } = true;

	//// Controls whether the tule/thatch handbasket grid recipe remains
	//// enabled after recipes load.
	////
	[JsonProperty("tule-handbasket")]
	public bool TuleHandbasket { get; set; } = true;

	//// Controls whether Better Ruins schematic blueprints can be learned per
	//// player and then treated as virtual crafting ingredients.
	////
	[JsonProperty("betterruins-blueprint-learning")]
	public bool BetterRuinsBlueprintLearning { get; set; } = true;

	//// Controls whether vanilla display cases can be stacked directly on top
	//// of other vanilla display cases.
	////
	[JsonProperty("display-case-stacking")]
	public bool DisplayCaseStacking { get; set; } = true;



	//// Normalizes loaded or externally supplied values into the supported
	//// runtime ranges.
	////
	//// The mod system calls this after disk loading and Config Lib setting
	//// changes. Clamping here keeps the individual runtime systems simple and
	//// prevents negative radii, negative damage, or extreme cooldown values
	//// from leaking into gameplay logic.
	////
	public void Normalize()
	{
		UndergroundRadiusForLoreGlow = ClampRadius(UndergroundRadiusForLoreGlow);
		UndergroundRadiusForLoreYowl = ClampRadius(UndergroundRadiusForLoreYowl);
		AboveGroundRadiusForLoreGlow = ClampRadius(AboveGroundRadiusForLoreGlow);
		AboveGroundRadiusForLoreYowl = ClampRadius(AboveGroundRadiusForLoreYowl);
		PalisadeDamageAmount = ClampDamage(PalisadeDamageAmount);
		PalisadeDamageCooldownSeconds = ClampPalisadeDamageCooldown(PalisadeDamageCooldownSeconds);
	}



	//// Selects the appropriate configured glow radius for the cat's current
	//// underground or above-ground state.
	////
	//// The cat behavior calls this during its client-side scan so the
	//// environment-specific configuration decision stays centralized in the
	//// config model.
	////
	public float GetRadiusForLoreGlow(bool isUnderground)
	{
		return isUnderground ? UndergroundRadiusForLoreGlow : AboveGroundRadiusForLoreGlow;
	}



	//// Selects the appropriate configured yowl radius for the cat's current
	//// underground or above-ground state.
	////
	//// The cat behavior calls this during its server-side scan so sound
	//// triggering uses the same environment split as the glow behavior.
	////
	public float GetRadiusForLoreYowl(bool isUnderground)
	{
		return isUnderground ? UndergroundRadiusForLoreYowl : AboveGroundRadiusForLoreYowl;
	}



	//// Clamps a lore detection radius to the supported non-negative range.
	////
	//// A value of zero is meaningful because it disables the corresponding
	//// glow or yowl scan, so the lower bound preserves zero rather than
	//// forcing a minimum active radius.
	////
	private static float ClampRadius(float radius)
	{
		return radius < 0 ? 0 : radius;
	}



	//// Clamps palisade damage to a non-negative value.
	////
	//// Zero remains valid because it effectively disables damage without
	//// requiring the separate feature toggle to be changed.
	////
	private static float ClampDamage(float damage)
	{
		return damage < 0 ? 0 : damage;
	}



	//// Clamps the palisade damage cooldown to the supported gameplay range.
	////
	//// The server-side palisade system scans once per second, so the minimum
	//// cooldown is one scan interval and the maximum keeps stale per-entity
	//// damage state from persisting longer than intended.
	////
	private static float ClampPalisadeDamageCooldown(float cooldownSeconds)
	{
		if (cooldownSeconds < 1)
		{
			return 1;
		}

		return cooldownSeconds > 10 ? 10 : cooldownSeconds;
	}
}
