Tweaks I like to play with, especially with my kids. Always looking for people to play with and learn from, so feel free to reach out if you want to play together. I love PvE and hate PvP. Very co-opy kind of guy.

Supported Vintage Story versions: 1.22.2 through 1.22.3. The modinfo dependency uses `game: 1.22.2` because Vintage Story treats that field as the minimum required game version.

## Light Mudbrick Recipe Also Uses Gravel

Light mudbricks can now use gravel or sand in the crafting recipe.

- From a physics perspective, gravel should provide a similar mineral substrate to sand in mudbrick construction.
- From a gameplay perspective, gravel has very few uses, so this makes an underused resource more useful.
- From a player perspective:
  - You can craft light mudbricks with gravel or sand in the same ingredient slot.
  - Normal, layered, dirty, muddy, and sludgy gravel should all work.
  - The recipe is otherwise unchanged.
  - Dark mudbricks are unchanged.

## Packed Dirt And Rammed Earth Speed Buffs

Packed dirt and rammed earth give movement speed buffs, but they stay weaker than stone paths.

- Packed dirt and rammed earth are intentionally prepared walking surfaces, so they should be faster than loose ground.
- Stone paths still require more dedicated road-building material, so they remain the best basic path option.
- Rammed earth takes more work than packed dirt, so it gets the larger buff.
- From a player perspective:
  - Packed dirt increases walking speed by 10%.
  - Rammed earth increases walking speed by 20%.
  - Stone paths are unchanged and still increase walking speed by 30%.

## Stone Paths From Prepared Soil

Placed packed dirt and rammed earth can now be upgraded directly into stone paths with loose stones.

- Prepared dirt already forms the base of a road surface, so adding stones on top should finish it into a stronger path.
- Mason, Miner, and Commoner characters can do this most efficiently.
- Tinker, Artisan, Homesteader, and Clockmaker characters get a smaller material discount.
- From a player perspective:
  - Right-click placed packed dirt or rammed earth while holding loose stones to turn it into a stone path.
  - Mason, Miner, and Commoner characters use 2 stones.
  - Tinker, Artisan, Homesteader, and Clockmaker characters use 3 stones.
  - All other characters use 4 stones.

## Cat Lore Warning

Cats from the Cats mod can now warn you about nearby lore creatures.

- Cats glow when a lore creature is nearby, with a stronger glow as the nearest lore creature gets closer.
- Cats yowl when a lore creature first enters the configured warning radius.
- Cats can be protected from lore-creature damage and fall damage.
- From a player perspective:
  - `cat-impervious-to-lore-creatures` controls lore-creature and fall-damage immunity.
  - `underground-radius-for-lore-glow` controls the underground glow radius; `0` disables underground glow.
  - `underground-radius-for-lore-yowl` controls the underground yowl radius; `0` disables underground yowl.
  - `above-ground-radius-for-lore-glow` controls the above-ground glow radius; `0` disables above-ground glow.
  - `above-ground-radius-for-lore-yowl` controls the above-ground yowl radius; `0` disables above-ground yowl.
  - These settings can be edited through Config Lib when it is installed.
  - The default glow and yowl radii are 8 blocks underground and 16 blocks above ground.

## Palisades

Palisades can now hurt less-aware hostile creatures and can be recycled with woodworking tools.

- Lore creatures take damage when they are on or horizontally next to palisade walls or palisade stakes.
- Hostile mundane predators such as adult bears, wolves, and hyenas also take damage when they are charging toward a nearby player.
- Normal animals and players are not damaged by palisades.
- From a player perspective:
  - `enable-palisade-damage` controls whether palisade creature damage is enabled.
  - `palisade-damage-amount` controls the damage dealt each pulse; the default is `1`.
  - `palisade-damage-cooldown-seconds` controls the per-creature cooldown between damage pulses; the default is `5`, with a range from `1` to `10`.
  - Breaking palisade wall pieces with an axe drops 3 or 4 firewood depending on the wall size.
  - Breaking palisade wall pieces with a saw drops 3 or 4 oak boards depending on the wall size.
  - Breaking palisade stakes with an axe drops 1 firewood.
  - Breaking palisade stakes with a saw drops 1 oak board.
  - Breaking palisades with other tools uses the normal drop behavior.
  - These settings can be edited through Config Lib when it is installed.

## Barricades

Looted clutter barricades can now be recycled into aged wood materials.

- Barricades are old salvaged clutter, so they return aged materials rather than fresh wood.
- From a player perspective:
  - Breaking clutter barricades with an axe drops 4 aged firewood.
  - Breaking clutter barricades with a saw drops 4 aged oak boards.
  - Breaking clutter barricades with other tools uses the normal drop behavior.

## Bone Knife

Bones can now be crafted into a basic knife.

- Bone is softer than stone, but tougher and much less brittle, so it should last a bit longer than plain stone without surpassing flint.
- Bone has a lower real-world material density than stone, so the item uses a lower density value than stone tools.
- The knife gives bone another practical early-game use.
- From a player perspective:
  - Craft a bone knife with one bone over one stick.
  - Bone knife durability is 100.
  - Attack range matches other knives.
  - Attack power is 0.75, matching basic stone knives and staying below flint.
  - Material density is 1900 kg/m^3, the same as bones IRL.

## Hacked Locusts

Clockmakers can now hack sawblade locusts with the tuning spear.

- Sawblade locusts are locusts too, so the Clockmaker's existing locust-hacking ability should work on them.
- Hacked locust textures have green eyes, making friendly locusts easier to tell apart from enemy locusts.
- This does not change the vanilla tuning spear class restriction.
- From a player perspective:
  - Use the tuning spear on sawblade locusts the same way Clockmakers already use it on normal and corrupt locusts.
  - Hacked sawblade locusts follow the same friendly hacked-locust mechanics as other hacked locusts.
  - Hacked sawblade locusts have 120 health.
  - Hacked bronze, corrupt, and sawblade locusts have green-eye textures.
  - Even with class-specific recipes disabled, the tuning spear still seems restricted to Clockmakers.
