# Releases

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
