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
