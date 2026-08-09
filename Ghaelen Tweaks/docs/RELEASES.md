# Releases

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
