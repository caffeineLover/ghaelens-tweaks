# XSkills Fermentation Progression Enhancements

Ghaelen’s Tweaks expands the **Fermentation** skill from xSkills so that progression reflects the full brewing process rather than primarily rewarding the act of sealing barrels.

The enhancement is implemented as an optional compatibility layer. Ghaelen’s Tweaks does not require xSkills or xLib at compile time; when xSkills is installed, the compatibility bridge locates the player’s Fermentation skill at runtime and awards experience through xSkills’ normal `AddExperience` mechanism. Existing xSkills experience modifiers, including **Master Brewmaster**, therefore continue to apply.

## Original xSkills behavior

xSkills grants **3 Fermentation XP** when a player seals a barrel containing a recognized alcoholic fermentation recipe.

Recognized recipe codes include:

- Cider
- Perry
- Wine
- Mead
- Beer
- Ale

The amount of XP does not depend on batch size. Ghaelen’s Tweaks preserves this existing **+3 XP sealing award** and adds additional progression sources.

## Added experience sources

| Activity | Fermentation XP |
|---|---:|
| Existing xSkills fermentation sealing | +3 XP |
| Juice actually produced in a fruit press | 0.10 XP per litre |
| Successful fermentation completion | Volume-scaled |
| First successful cider | +8 XP |
| First successful perry | +8 XP |
| First successful wine | +8 XP |
| First successful mead | +8 XP |
| First successful beer | +8 XP |
| First successful ale | +8 XP |
| Distillate actually produced | 4 XP per litre |
| First successful distillation | +5 XP |

## Fermentation completion XP

Base completion XP is:

`XP = 0.75 × litres + 0.002 × litres²`

A recipe multiplier is then applied:

| Fermentation | Multiplier |
|---|---:|
| Cider | ×1.00 |
| Perry | ×1.00 |
| Wine | ×1.10 |
| Mead | ×1.15 |
| Beer | ×1.25 |
| Ale | ×1.25 |

Examples before other xSkills modifiers:

| Batch | Cider | Wine | Mead | Beer/Ale |
|---:|---:|---:|---:|---:|
| 10 L | 7.7 XP | 8.5 XP | 8.9 XP | 9.6 XP |
| 20 L | 15.8 XP | 17.4 XP | 18.2 XP | 19.8 XP |
| 50 L | 42.5 XP | 46.8 XP | 48.9 XP | 53.1 XP |

The quadratic term provides a modest incentive for larger batches and avoids encouraging players to split production into many tiny barrels merely to collect more sealing events.

The existing +3 XP sealing reward remains separate from the completion reward.

## Fruit pressing

Fruit pressing grants:

`0.10 XP per litre of juice actually produced`

XP is based on juice entering the receiving container, not on inserting fruit or turning fruit into mash.

Examples:

- 10 L juice = 1 XP
- 50 L juice = 5 XP

## Recipe discovery

The first successful completion of each major fermentation type grants an additional **+8 XP**.

Tracked separately:

- cider
- perry
- wine
- mead
- beer
- ale

Each bonus is awarded only once per character.

## Distillation

Successful distillation grants:

`4 XP per litre of distillate actually produced`

The character’s first successful distillation also grants:

`+5 XP`

Merely operating the boiler is not enough; XP is based on actual distillate output.

## Player attribution and persistence

The enhancement records the brewer when a fermentation begins. For an active batch, the barrel retains:

- Brewer player UID
- Recipe type
- Starting batch volume
- Whether enhanced fermentation tracking is active

That data is stored with the barrel and survives server restarts.

When fermentation completes, XP is credited to the player who started the batch rather than whichever player later interacts with the barrel.

Distillation ownership is handled similarly.

## Offline XP awards

If a fermentation finishes while its brewer is offline, the XP is stored in the world save and awarded when that player next becomes ready.

First-time discovery state is also persisted so bonuses cannot be repeatedly collected by restarting the server or repeating the same recipe.

## Experience modifiers

Enhanced XP is passed through xSkills’ normal `PlayerSkill.AddExperience(...)` path rather than directly modifying XP totals.

This preserves normal xSkills modifiers. In particular, **Master Brewmaster’s +40% Fermentation XP bonus** also affects these awards.

Example:

- 20 L cider completion: 15.8 XP
- With +40% modifier: 22.12 XP

## Progression philosophy

The enhanced system rewards the whole craft:

1. Prepare ingredients
2. Press juice
3. Start fermentation
4. Successfully complete fermentation
5. Learn new fermentation types
6. Distill finished products

The majority of progression comes from successfully producing fermented goods, rather than repeatedly performing one administrative action.

The system intentionally does not award XP for transferring liquids, opening barrels, drawing finished liquid with a bucket, or other container manipulation that does not produce anything.

## Compatibility

The enhancement lives in Ghaelen’s Tweaks rather than modifying xSkills itself.

- Without xSkills installed, Ghaelen’s Tweaks loads normally and the Fermentation enhancement remains inactive.
- With xSkills installed, the compatibility bridge activates the enhanced Fermentation progression.

There is no hard compile-time dependency on xSkills or xLib.
