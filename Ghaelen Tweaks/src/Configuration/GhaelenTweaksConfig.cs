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

	//// Sets how many marks each marking chalk stick can draw.
	////
	[JsonProperty("marking-chalk-uses")]
	public int MarkingChalkUses { get; set; } = 32;

	//// Sets how many plain marking chalk sticks are dyed by one litre of dye.
	////
	[JsonProperty("marking-chalk-dye-batch-size")]
	public int MarkingChalkDyeBatchSize { get; set; } = 16;

	//// Sets the dynamic light level emitted by temporal marking chalk marks.
	////
	[JsonProperty("temporal-marking-chalk-light-level")]
	public int TemporalMarkingChalkLightLevel { get; set; } = 3;

	//// Controls whether escalating parental-controls death delays are active.
	////
	[JsonProperty("pc-use-death-delay")]
	public bool PcUseDeathDelay { get; set; } = true;

	//// Sets the delay seconds contributed by each death beyond the free one.
	////
	[JsonProperty("pc-spawn-delay-increment")]
	public int PcSpawnDelayIncrement { get; set; } = 20;

	//// Sets the death-free seconds required to remove one counted death.
	////
	[JsonProperty("pc-spawn-delay-cooldown")]
	public int PcSpawnDelayCooldown { get; set; } = 15 * 60;

	//// Reserves the master switch for the planned respawn-sickness system.
	////
	[JsonProperty("pc-use-respawn-sickness")]
	public bool PcUseRespawnSickness { get; set; }



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
		MarkingChalkUses = ClampMarkingChalkUses(MarkingChalkUses);
		MarkingChalkDyeBatchSize = ClampMarkingChalkDyeBatchSize(MarkingChalkDyeBatchSize);
		TemporalMarkingChalkLightLevel = ClampLightLevel(TemporalMarkingChalkLightLevel);
		PcSpawnDelayIncrement = ClampPcSpawnDelayIncrement(PcSpawnDelayIncrement);
		PcSpawnDelayCooldown = ClampPcSpawnDelayCooldown(PcSpawnDelayCooldown);
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



	//// Clamps marking chalk uses to a practical positive range.
	////
	private static int ClampMarkingChalkUses(int uses)
	{
		if (uses < 1)
		{
			return 1;
		}

		return uses > 512 ? 512 : uses;
	}



	//// Clamps marking chalk dye batches to a practical stack-sized range.
	////
	private static int ClampMarkingChalkDyeBatchSize(int batchSize)
	{
		if (batchSize < 1)
		{
			return 1;
		}

		return batchSize > 64 ? 64 : batchSize;
	}



	//// Clamps temporal marking chalk dynamic light to Vintage Story's light
	//// level range.
	////
	private static int ClampLightLevel(int lightLevel)
	{
		if (lightLevel < 0)
		{
			return 0;
		}

		return lightLevel > 32 ? 32 : lightLevel;
	}



	//// Clamps the per-death delay increment to zero through one hour.
	////
	//// Zero remains valid because it disables the effective delay without
	//// disabling death-count tracking or the master switch.
	////
	private static int ClampPcSpawnDelayIncrement(int seconds)
	{
		if (seconds < 0)
		{
			return 0;
		}

		return seconds > 60 * 60 ? 60 * 60 : seconds;
	}



	//// Clamps the death-free cooldown to one second through seven days.
	////
	//// A positive cooldown avoids division by zero when the server lazily
	//// calculates how many counted deaths have expired.
	////
	private static int ClampPcSpawnDelayCooldown(int seconds)
	{
		if (seconds < 1)
		{
			return 1;
		}

		return seconds > 7 * 24 * 60 * 60 ? 7 * 24 * 60 * 60 : seconds;
	}
}
