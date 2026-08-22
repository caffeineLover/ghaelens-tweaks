# Releases

## Version 0.6.2

**Tag:** `0.6.2`

**Released:** 2026-08-21

This release gives players their first death for free while retaining an escalating, recoverable consequence for
repeated deaths.

### Changes

- The first counted death now has no respawn delay.
- With default settings, consecutive deaths now wait 0, 20, 40, and 60 seconds.
- Every 900 death-free seconds removes one counted death, continuing while the player is offline until the count reaches
  zero.
- Replaced the three earlier numeric delay settings with `pc-spawn-delay-increment` and
  `pc-spawn-delay-cooldown`, both exposed through Config Lib.
- Existing 0.6.x accumulated death state migrates automatically to the new counter.



## Version 0.6.1

**Tag:** `0.6.1`

**Released:** 2026-08-21

This hotfix prevents an early rejected respawn attempt from permanently disabling the death dialog.

### Changes

- Respawn clicks are now held on the client while the parental-control delay remains active.
- A rejected early attempt now clears Vintage Story's stale respawning state so the button reliably unlocks when the
  countdown reaches zero.



## Version 0.6.0

**Tag:** `parental-controls-initial`

**Released:** 2026-08-21

This release introduces the first recoverable parental-control consequence for repeated player deaths.

### Changes

- Added a server-enforced respawn delay that escalates separately for each player after repeated deaths.
- The default first-death delay is 30 seconds: a 15-second base plus the newly applied 15-second death increase.
- Added repeated cooldown recovery. Every 900 death-free seconds removes one accumulated increase until none remain;
  recovery continues while the player is offline.
- Added a synced death-screen countdown that disables the respawn button until the server-owned deadline expires.
- Added `pc-use-death-delay`, `pc-death-delay`, `pc-death-delay-increase`, and `pc-death-delay-cooldown`, all exposed
  through Config Lib.
- Added the disabled-by-default `pc-use-respawn-sickness` reserved setting for the next parental-controls feature.
- Added `PARENTAL_CONTROLS.md` with the complete recoverable consequence backlog, implemented delay specification, and
  draft escalating movement-sickness design.



## Version 0.5.0

**Tag:** `Added-alternate-gray-dye-recipe`

**Released:** 2026-08-21

This release makes gray dye affordable without removing the original vanilla recipes.

### Changes

- Added an alternate barrel recipe that turns 2 L of water and one powdered charcoal into 2 L of gray dye after 8
  sealed hours.



## Version 0.4.11

**Tag:** `v0.4.11`

**Released:** 2026-08-09

This release makes temporal marking chalk glow strength configurable and raises the default dynamic glow level.

### Changes

- Added `temporal-marking-chalk-light-level`, a config setting for the dynamic light emitted by temporal marking chalk.
- Changed temporal marking chalk's default dynamic light level from 2 to 3.
- Removed static temporal chalk decor light definitions so the hidden marker entity is the configurable dynamic light
  source.



## Version 0.4.10

**Tag:** `v0.4.10`

**Released:** 2026-08-09

This hotfix restores the marking chalk ladder symbol and clarifies the scope of temporal chalk glow.

### Changes

- Fixed the ladder marking so it uses opaque chalk pixels and appears reliably in game.
- Clarified that temporal marking chalk's level-2 dynamic glow is visual and should not be treated as lore spawn
  prevention.



## Version 0.4.9

**Tag:** `v0.4.9`

**Released:** 2026-08-09

This release expands marking chalk with full-face paint marks, temporal glow variants, and a crisper ladder symbol.

### Changes

- Added paint-face mode for marking chalk. It paints the entire clicked face and costs 4 uses, or the whole stick when
  `marking-chalk-uses` is configured below 4.
- Added temporal marking chalk. Craft any marking chalk with a temporal gear to create a temporal variant whose placed
  glyphs and paint marks emit a level-2 colored dynamic glow.
- Tightened the ladder glyph so it reads more clearly on placed chalk marks.



## Version 0.4.8

**Tag:** `v0.4.8`

**Released:** 2026-08-09

This hotfix corrects the floor-arrow rotation formula after testing showed up/down were still reversed.

### Changes

- Fixed floor and ceiling arrow rotation so north/south directions use the same Vintage Story surfacelayer mapping as
  east/west directions.
- Documented why the previous formula made right and left look correct while reversing up and down.



## Version 0.4.7

**Tag:** `v0.4.7`

**Released:** 2026-08-09

This hotfix corrects the remaining right-arrow floor and ceiling orientation case.

### Changes

- Fixed right-arrow marks on floors and ceilings so the player-relative direction is resolved from the clicked camera
  ray instead of relying only on entity body yaw.



## Version 0.4.6

**Tag:** `v0.4.6`

**Released:** 2026-08-09

This hotfix corrects the remaining floor and ceiling arrow direction reversal.

### Changes

- Fixed floor and ceiling arrow marks being rotated 180 degrees from the selected wall-arrow direction.



## Version 0.4.5

**Tag:** `v0.4.5`

**Released:** 2026-08-09

This hotfix corrects marking chalk arrow direction on floors and ceilings.

### Changes

- Fixed floor and ceiling arrow marks so arrow up/right/down/left are oriented from the player's facing direction when placed.
- Fixed redrawing a floor or ceiling arrow in the same decor cell so the new rotated mark replaces the old one instead of stacking on top of it.



## Version 0.4.4

**Tag:** `v0.4.4`

**Released:** 2026-08-09

This hotfix improves the exit marker and fixes marking chalk stack use display.

### Changes

- Replaced the exit marker with a simpler doorway glyph that does not depend on left/right direction.
- Fixed marking chalk stacks so using one stick splits it into its own partially used stack while the remaining sticks stay fresh.
- If the inventory cannot accept the fresh remainder during that split, the remainder drops near the player instead of being lost.



## Version 0.4.3

**Tag:** `v0.4.3`

**Released:** 2026-08-09

This hotfix corrects horizontal marking chalk arrows and refines the danger glyph.

### Changes

- Fixed right and left arrow modes placing the opposite horizontal arrow in game.
- Adjusted the danger glyph so the exclamation dot remains visually separate from the triangle base.



## Version 0.4.2

**Tag:** `v0.4.2`

**Released:** 2026-08-09

This hotfix adds marking chalk erasing and improves the danger mark's readability.

### Changes

- Added marking chalk erasing. Shift/crouch right-click or the explicit erase mode removes nearby Ghaelen Tweaks chalk marks from the clicked face.
- Erasing refunds one use to the active chalk stick, capped at the configured maximum.
- Reworked the danger glyph with a bolder pixel-art symbol so it reads more clearly in game.



## Version 0.4.1

**Tag:** `v0.4.1`

**Released:** 2026-08-09

This hotfix fixes marking chalk overlay rendering and makes arrow selection explicit.

### Changes

- Fixed marking chalk overlay textures rendering as filled squares by switching the decor art to the same spritesheet cell format used by vanilla cave art.
- Replaced the single yaw-based marking chalk arrow mode with explicit arrow up, arrow right, arrow down, and arrow left tool modes.
- Existing square markers placed with `0.4.0` may need to be removed and placed again because new marks use corrected cell-based decor block codes.



## Version 0.4.0

**Tag:** `v0.4.0`

**Released:** 2026-08-09

This release adds marking chalk for mine and trail navigation marks, and removes the old cat damage-immunity guardrail.

### Changes

- Added marking chalk, a configurable-use item that places colored decor marks without replacing the marked block.
- Added mark modes for arrows, X marks, dots, ladders, stairs, danger marks, and exit marks.
- Marking chalk works on valid solid stone, ore, brick, log/trunk, packed dirt, dry packed dirt, and rammed earth block faces.
- Added plain marking chalk crafting from vanilla chalk stones.
- Added barrel and fired-bowl dyeing with 1 L of vanilla dye; `dye-woad` produces blue marking chalk.
- Added `marking-chalk-uses` and `marking-chalk-dye-batch-size` config settings with Config Lib controls.
- Removed cat lore-creature and fall-damage immunity, including the removed config setting. Cat glow and yowl warnings remain unchanged.



## Version 0.3.8

**Tag:** `v0.3.8`
**Released:** 2026-08-07

This hotfix removes the remaining A Culinary Artillery handbook crash path caused by patch ordering around dynamic wooden clutter fuels.

### Changes

- Fixed the A Culinary Artillery simmer-handbook helper path so dynamic wooden clutter fuels are filtered even when ACA's handbook prefix runs before the vanilla handbook fuel-list sanitizer.
- The existing firepit fuel behavior for wooden clutter remains unchanged.



## Version 0.3.7

**Tag:** `v0.3.7`
**Released:** 2026-08-04

This hotfix extends the handbook crash protection for wooden clutter fuels to another handbook page path used by A Culinary Artillery.

### Changes

- Fixed another A Culinary Artillery handbook crash path by filtering dynamic wooden clutter fuels before "processes into" handbook entries are built.
- The existing firepit fuel behavior for wooden clutter remains unchanged.



## Version 0.3.6

**Tag:** `v0.3.6`
**Released:** 2026-07-29

This hotfix keeps wooden clutter burnable in firepits while avoiding a handbook crash with mods that assume fuel entries use static collectible combustible properties.

### Changes

- Added a client-side handbook compatibility patch for dynamic wooden clutter fuels.
- Firepit fuel behavior for barricades, wood rubble, ruined tables, stacked small crates, large rot crates, and chest rubble remains unchanged.



## Version 0.3.5

**Tag:** `v0.3.5`
**Released:** 2026-07-29

This release expands clutter recycling to more wooden ruin objects and makes direct clutter burning match recovered fuel value.

### Changes

- Added axe and saw recycling recipes for wood rubble, ruined tables, stacked small crates, large crates with rot, and chest rubble.
- Axe recipes return aged firewood; saw recipes return aged boards; neither tool loses durability.
- Large crates with rot also return 32 rot when recycled.
- Added firepit fuel support for the new wooden clutter groups.
- Changed barricade clutter burn duration from 24 seconds to 96 seconds so direct burning matches the recovered aged firewood value.



## Version 0.3.4

**Tag:** `v0.3.4`
**Released:** 2026-07-29

This release adds display case stacking and reworks barricade clutter recycling into crafting recipes with direct fuel support.

### Changes

- Added `display-case-stacking`, enabled by default, so normal and tall display cases can be placed directly on top of other display cases.
- Added axe and saw crafting recipes for all vanilla clutter barricade variants. Axe recipes return 4 aged firewood, saw recipes return 4 aged boards, and neither tool loses durability.
- Added firepit fuel support for barricade clutter, matching aged firewood fuel values.
- Removed the old barricade break-drop block behavior and clutter patch.
- Clarified palisade documentation as dismantling/reuse rather than clutter recycling.
- Cleaned up Better Ruins nullable and namespace inspections.



## Version 0.3.3

**Tag:** `v0.3.3`
**Released:** 2026-07-26

This release adds a player command for checking which Better Ruins schematic blueprints have been memorized.

### Changes

- Added `/gtweak schematics`, which lists the player's memorized Better Ruins schematics.
- The command shows readable schematic names when they can be resolved, along with the stored schematic code for troubleshooting.
- If Better Ruins blueprint learning is disabled, the command still reports the saved memorized schematic list and clearly notes that the feature is disabled.



## Version 0.3.2

**Tag:** `better-ruin-schematic-memorization`
**Released:** 2026-07-26

This release adds per-player memorization for Better Ruins schematic blueprints, so known schematic recipes no longer require placing the physical blueprint in the crafting grid.

### Changes

- Added right-click memorization for Better Ruins schematic blueprints, including chat feedback when a blueprint is learned or already known.
- Learned Better Ruins schematics are stored per player and synced to the client for crafting preview and tooltip status.
- Crafting recipes gated by a learned Better Ruins schematic can now be crafted without placing that schematic in the grid, while physical schematics still work normally.
- Added the `betterruins-blueprint-learning` config setting, enabled by default.



## Version 0.3.1

**Tag:** `Tule-Handbasket`
**Released:** 2026-07-15

This release adds a new handbasket recipe for players harvesting tule.

### Changes

- Added a tule handbasket recipe: craft the normal reed handbasket with thatch in the same pattern and material cost used by existing cattail and papyrus handbasket recipes.
