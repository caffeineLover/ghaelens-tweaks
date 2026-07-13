using Newtonsoft.Json;

namespace GhaelenTweaks;

public sealed class GhaelenTweaksConfig
{
	public static GhaelenTweaksConfig Current { get; set; } = new();

	[JsonProperty("cat-impervious-to-lore-creatures")]
	public bool CatImperviousToLoreCreatures { get; set; } = true;

	[JsonProperty("underground-radius-for-lore-glow")]
	public float UndergroundRadiusForLoreGlow { get; set; } = 8f;

	[JsonProperty("underground-radius-for-lore-yowl")]
	public float UndergroundRadiusForLoreYowl { get; set; } = 8f;

	[JsonProperty("above-ground-radius-for-lore-glow")]
	public float AboveGroundRadiusForLoreGlow { get; set; } = 16f;

	[JsonProperty("above-ground-radius-for-lore-yowl")]
	public float AboveGroundRadiusForLoreYowl { get; set; } = 16f;

	[JsonProperty("enable-palisade-damage")]
	public bool EnablePalisadeDamage { get; set; } = true;

	[JsonProperty("palisade-damage-amount")]
	public float PalisadeDamageAmount { get; set; } = 1f;

	[JsonProperty("palisade-damage-cooldown-seconds")]
	public float PalisadeDamageCooldownSeconds { get; set; } = 5f;

	[JsonProperty("persistent-crafting-grid")]
	public bool PersistentCraftingGrid { get; set; } = true;

	public void Normalize()
	{
		UndergroundRadiusForLoreGlow = ClampRadius(UndergroundRadiusForLoreGlow);
		UndergroundRadiusForLoreYowl = ClampRadius(UndergroundRadiusForLoreYowl);
		AboveGroundRadiusForLoreGlow = ClampRadius(AboveGroundRadiusForLoreGlow);
		AboveGroundRadiusForLoreYowl = ClampRadius(AboveGroundRadiusForLoreYowl);
		PalisadeDamageAmount = ClampDamage(PalisadeDamageAmount);
		PalisadeDamageCooldownSeconds = ClampPalisadeDamageCooldown(PalisadeDamageCooldownSeconds);
	}

	public float GetRadiusForLoreGlow(bool isUnderground)
	{
		return isUnderground ? UndergroundRadiusForLoreGlow : AboveGroundRadiusForLoreGlow;
	}

	public float GetRadiusForLoreYowl(bool isUnderground)
	{
		return isUnderground ? UndergroundRadiusForLoreYowl : AboveGroundRadiusForLoreYowl;
	}

	private static float ClampRadius(float radius)
	{
		return radius < 0 ? 0 : radius;
	}

	private static float ClampDamage(float damage)
	{
		return damage < 0 ? 0 : damage;
	}

	private static float ClampPalisadeDamageCooldown(float cooldownSeconds)
	{
		if (cooldownSeconds < 1)
		{
			return 1;
		}

		return cooldownSeconds > 10 ? 10 : cooldownSeconds;
	}
}
