# Changelog

## 0.3.2 - 2026-07-26

### Added

- Added Better Ruins blueprint learning: players can right-click a Better Ruins blueprint to remember it, then craft recipes for that blueprint without placing the physical blueprint in the crafting grid.
- Added the `betterruins-blueprint-learning` config setting, enabled by default.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.3.1` to `0.3.2`.

## 0.3.1 - 2026-07-15

### Added

- Added a tule handbasket recipe: craft the normal reed handbasket with thatch in the same pattern and material cost used by existing cattail and papyrus handbasket recipes.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.3.0` to `0.3.1`.

## 0.3.0 - 2026-07-12

### Added

- Added a configurable persistent player crafting grid tweak, enabled by default with `persistent-crafting-grid`.
- The tweak preserves the vanilla 3x3 player crafting-grid ingredient slots when closing the inventory dialog.
- The implementation uses the existing vanilla `craftinggrid` inventory and output slot; it does not add a new GUI, inventory, block, or custom persistence store.
- Added Config Lib metadata and English text for the new `persistent-crafting-grid` setting.
- Added `PERSISTENT_CRAFTING_GRID.md` documenting the inspected Vintage Story 1.22.3 crafting inventory, close-time evacuation path, persistence behavior, death handling, and spoilage behavior.

### Changed

- Added a Harmony reference from the configured Vintage Story install so the client-side inventory-close patch can be applied.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.2.4` to `0.3.0`.

## 0.2.3 - 2026-07-10

### Fixed

- Fixed Vintage Story patch-load errors from `Ghaelen Tweaks/assets/survival/patches/hacked-locusts.json` when patching `game:entities/lore/locust-hacked.json`.
- Removed the six hacked locust damage patches that targeted `/server/behaviors/8/aitasks/2/...`.
- Those removed patches attempted to edit `/server/behaviors/8/aitasks/2/damageByType`, `/server/behaviors/8/aitasks/2/damageTierByType`, and `/server/behaviors/8/aitasks/2/damageTypeByType`.
- In the reported `locust-hacked.json` data, AI task `2` is `seektargetingentityrepairablelocust`, a repair-targeting task with fields such as `entityCodes`, `movespeed`, `seekingRange`, `leapAtTarget`, and `animation`.
- Because that repair-targeting task does not contain any damage maps, Vintage Story rejected the remove/add operations before they could apply. These patches were already non-functional and were only producing load errors.
- Kept the valid hacked locust damage patches under `/server/behaviors/8/aitasks/1/...`, where the damage maps actually exist.
- Kept the rest of the hacked sawblade locust support intact: the hacked entity mapping, hacked texture setup, corrupt sawblade health values, and `corrupt-sawblade` variant registration are unchanged.
- Regenerated the release package as `Releases/ghaelentweaks_0.2.3.zip` so the packaged mod contains the corrected hacked-locust patch file.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.2.2` to `0.2.3`.

## 0.2.2 - 2026-07-04

### Added

- Added axe and saw recycling drops for looted clutter barricades: 4 aged firewood with an axe, or 4 aged oak boards with a saw.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.2.1` to `0.2.2`.

## 0.2.1 - 2026-07-04

### Changed

- Palisade walls and stakes now drop firewood with axes and oak boards with saws, using the same 1-to-1 quantity based on palisade size.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.2.0` to `0.2.1`.

## 0.2.0 - 2026-07-03

### Added

- Added configurable palisade damage for lore creatures on or horizontally next to `game:palisadewall-*` and `game:palisadestakes-*`.
- Added charging hostile mundane predator palisade damage for adult bears, wolves, and hyenas near players.
- Added Config Lib settings `enable-palisade-damage`, `palisade-damage-amount`, and `palisade-damage-cooldown-seconds`.
- Added axe and saw firewood drops for palisade walls and palisade stakes.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.14` to `0.2.0`.
- Palisade damage now defaults to a 5-second per-creature cooldown, configurable from 1 to 10 seconds.

## 0.1.14 - 2026-06-26

### Changed

- Cat lore-creature and fall-damage immunity now applies everywhere instead of only underground.
- Split cat lore warning radii into underground and above-ground config values.
- Underground cat lore glow and yowl default to 8 blocks; above-ground glow and yowl default to 16 blocks.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.13` to `0.1.14`.

## 0.1.13 - 2026-06-21

### Changed

- Changed the default `radius-for-lore-glow` and `radius-for-lore-yowl` values from 8 blocks to 10 blocks.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.12` to `0.1.13`.

## 0.1.12 - 2026-06-21

### Added

- Added Config Lib settings for the cat lore-guardian config values.
- Cat lore settings now update at runtime when Config Lib publishes setting changes.

### Changed

- Changed the default `radius-for-lore-glow` and `radius-for-lore-yowl` values from 5 blocks to 8 blocks.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.11` to `0.1.12`.

## 0.1.11 - 2026-06-21

### Changed

- Changed cat lore glow from an on/off effect to a proximity-scaled effect.
- Cats now use a soft glow near the edge of `radius-for-lore-glow` and a stronger glow as the nearest lore creature gets closer.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.10` to `0.1.11`.

## 0.1.10 - 2026-06-21

### Added

- Added a configurable cat lore-guardian behavior for the Cats mod.
- Cats now glow while a lore creature is within `radius-for-lore-glow` blocks; `0` disables the glow.
- Cats now play `Angry_Cat.ogg` when a lore creature first enters `radius-for-lore-yowl` blocks; `0` disables the yowl.
- Cats can be made immune to lore-creature damage and fall damage while underground with `cat-impervious-to-lore-creatures`.
- Added `ghaelentweaks.json` mod config defaults for the cat lore behavior.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.9` to `0.1.10`.

## 0.1.9 - 2026-06-20

### Fixed

- Fixed a crash when right-clicking packed dirt or rammed earth in Vintage Story 1.22.2 by avoiding a block sound API call that was not runtime-compatible with 1.22.2.
- Updated the code project default to compile against the StoryForge 1.22.2 API when that install is present.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.8` to `0.1.9`.

## 0.1.8 - 2026-06-20

### Added

- Added a code behavior that lets players right-click placed packed dirt or rammed earth with loose stones to convert it into a stone path.
- Mason, Miner, and Commoner characters use 2 stones.
- Tinker, Artisan, Homesteader, and Clockmaker characters use 3 stones.
- All other characters use 4 stones.
- Added the behavior to packed dirt and rammed earth through a survival patch.

### Changed

- Changed the mod type from `content` to `code`.
- Updated the Cake package task to build and include `GhaelenTweaks.dll`.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.7` to `0.1.8`.

## 0.1.7 - 2026-06-20

### Fixed

- Fixed ModDB upload validation by changing the `game` dependency back to a single minimum version, `1.22.2`; Vintage Story mod dependencies do not accept a `1.22.2 - 1.22.3` range string.
- Kept Vintage Story 1.22.3 listed as supported in the README.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.6` to `0.1.7`.

## 0.1.6 - 2026-06-20

### Changed

- Updated the declared Vintage Story game dependency range to `1.22.2 - 1.22.3`.
- Added a descriptive comment to the Cake build host source file.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.5` to `0.1.6`.

## 0.1.5 - 2026-06-20

### Changed

- Expanded the light mudbrick gravel recipes to include dirty gravel, muddy gravel, and sludgy gravel in addition to normal gravel variants.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.4` to `0.1.5`.

## 0.1.4 - 2026-06-19

### Changed

- Updated the Cake build host target framework from `net7.0` to `net10.0`.
- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.3` to `0.1.4`.

## 0.1.3 - 2026-06-19

### Fixed

- Fixed the bone knife display name by moving custom English language keys into `assets/game/lang/en.json`.
- Moved hacked locust English language keys into the same language file so they load through the normal localization path.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.2` to `0.1.3`.

## 0.1.2 - 2026-06-19

### Fixed

- Fixed the light mudbrick gravel tweak so it adds a gravel recipe instead of replacing the vanilla sand recipe.
- Light mudbricks can now be crafted with either sand or gravel.

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.1` to `0.1.2`.

## 0.1.1 - 2026-06-19

### Fixed

- Fixed the `bone-knife.json`, `mudbrick-gravel.json`, and `soil-path-speed-buffs.json` patches failing with "file not found" errors. Their `file` targets had no domain prefix, so they resolved to the patch's own `survival` domain instead of the vanilla content's `game` domain.
- Prefixed all affected `file` targets with `game:` (`game:itemtypes/tool/knife.json`, `game:recipes/grid/tool/knife.json`, `game:recipes/grid/mudbricks.json`, `game:blocktypes/soil/packeddirt.json`, `game:blocktypes/soil/rammed.json`).

### Changed

- Bumped `Ghaelen Tweaks/modinfo.json` version from `0.1.0` to `0.1.1`.

## 2026-06-19

### Added

- Added `Ghaelen Tweaks/assets/survival/patches/mudbrick-gravel.json`.
- The patch targets the Vintage Story 1.22.2 survival recipe file `recipes/grid/mudbricks.json`.
- It adds a second light mudbrick recipe that uses `gravel-*` while leaving the vanilla `sand-*` recipe valid.
- The gravel ingredient remains a block ingredient, matching the vanilla recipe's original sand requirement shape.
- Added `CHANGELOG.md` to `Ghaelen Tweaks.sln` as a `Solution Items` entry so it appears in the IDE solution view.
- Added `README.md` with the current tweaks described from a player perspective.
- Added `README.md` to `Ghaelen Tweaks.sln` as a `Solution Items` entry so it appears in the IDE solution view.
- Added README justification for the gravel mudbrick recipe change from both physics and gameplay perspectives.
- Added `Ghaelen Tweaks/assets/survival/patches/soil-path-speed-buffs.json`.
- Added packed dirt walking speed buff: `blocktypes/soil/packeddirt.json` now receives `walkspeedmultiplier: 1.10`.
- Added rammed earth walking speed buff: `blocktypes/soil/rammed.json` now receives `walkspeedmultiplier: 1.20`.
- Updated `README.md` with the packed dirt and rammed earth speed buff tweak from a player perspective.
- Added `Ghaelen Tweaks/assets/survival/patches/bone-knife.json`.
- Added `Ghaelen Tweaks/assets/game/patches/bone-knife-language.json`.
- Added `bone` to the generic knife material variants so `knife-generic-bone` can exist as a normal knife item.
- Added a bone knife recipe: one bone over one stick produces one `knife-generic-bone`.
- Added bone knife durability: `100`.
- Added bone knife plant cutting speed: `plant: 1`, matching basic stone knives.
- Added bone knife attack power: `0.75`, matching basic stone knives and staying below flint.
- Added bone knife material density: `1900`.
- Added English language text so `knife-generic-bone` displays as `Bone knife`.
- Updated `README.md` with the bone knife tweak from a player perspective.
- Added `Ghaelen Tweaks/assets/survival/patches/hacked-locusts.json`.
- Added `Ghaelen Tweaks/assets/game/patches/hacked-locust-language.json`.
- Added green-eye hacked locust textures from the old Hacked Locusts mod for bronze, corrupt, and corrupt sawblade locusts.
- Added hacked sawblade locust support so Clockmakers can hack sawblade locusts with the tuning spear.
- Added English language text for hacked sawblade locust items.
- Updated `README.md` with the hacked locusts tweak from a player perspective.

### Changed

- Updated `Ghaelen Tweaks/modinfo.json` author from `Unknown` to `Ghaelen`.
- Updated `Ghaelen Tweaks/modinfo.json` game dependency from blank to `1.22.2 - 1.22.3`.
- Updated `README.md` version target text to state that the mod declares support for Vintage Story 1.22.2 through 1.22.3 while noting the current tweaks were built from 1.22.2 data.
- Confirmed `Ghaelen Tweaks/modinfo.json` display name is `Ghaelen Tweaks`.
- Confirmed `Ghaelen Tweaks/modinfo.json` mod id is `ghaelentweaks`.
- Confirmed `README.md` title and intro text use `Ghaelen Tweaks`.
- Confirmed the containing workspace folder is `E:\Gaming\Vintage Story\mods\Ghaelen Tweaks`.
- Confirmed the solution file is `Ghaelen Tweaks.sln`.
- Confirmed the main project display name inside `Ghaelen Tweaks.sln` is `Ghaelen Tweaks`.
- Confirmed Rider settings folder uses `.idea/.idea.Ghaelen Tweaks`.
- Confirmed Rider ignored module file name uses `.idea.Ghaelen Tweaks.iml`.
- Renamed the inner project folder to `Ghaelen Tweaks`.
- Renamed the project file to `Ghaelen Tweaks/Ghaelen Tweaks.csproj`.
- Renamed the mod system source file to `Ghaelen Tweaks/GhaelenTweaksModSystem.cs`.
- Updated the mod system namespace and class name to use `Ghaelen`.
- Updated the Cake build project path from the old project name to `Ghaelen Tweaks`.
- Reworked the old Hacked Locusts patch for Vintage Story 1.22.x behavior indices.
- Preserved hacked sawblade locust health at `120`.
- Normalized `Ghaelen Tweaks.sln` with standard Visual Studio solution version metadata for Rider compatibility.
- Added a standard `SolutionProperties` section to `Ghaelen Tweaks.sln`.
- Normalized `Ghaelen Tweaks.sln` line endings to CRLF because Rider reported a solution parse failure.

### Investigated

- Reviewed `E:\Gaming\Vintage Story\mods\Vintage Story API\version 1.22.2\assets - 1.22.2\survival\recipes\grid\mudbricks.json`.
- Confirmed only the light mudbrick recipe uses the `S` ingredient slot for sand.
- Confirmed the matching gravel block code pattern is `gravel-*` from `survival/blocktypes/stone/gravel.json`.
- Confirmed this mod's project file already copies `assets\**` into the mod output.
- Reviewed `CakeBuild/Program.cs` and confirmed package builds copy the mod `assets` directory into release packages.
- Reviewed `survival/blocktypes/stone/generic/stonepath.json` and confirmed stone paths use `walkspeedmultiplier: 1.30`.
- Reviewed `survival/blocktypes/soil/packeddirt.json` and confirmed packed dirt had no existing walking speed multiplier.
- Reviewed `survival/blocktypes/soil/rammed.json` and confirmed rammed earth had no existing walking speed multiplier.
- Reviewed `survival/recipes/grid/rammedearth.json` and confirmed rammed earth is crafted from packed dirt.
- Reviewed `survival/itemtypes/tool/knife.json` and confirmed vanilla knife values: basic stone durability `90`, stone with bone handle durability `110`, flint durability `130`, basic stone attack power `0.75`, and flint attack power `1`.
- Reviewed `survival/recipes/grid/tool/knife.json` and confirmed existing knife recipes use a vertical one-wide pattern.
- Reviewed `survival/itemtypes/resource/bone.json` and confirmed the vanilla bone item code is `bone`.
- Reviewed vanilla language entries and confirmed there was no `item-knife-generic-bone` entry.
- Reviewed nearby mod metadata examples and confirmed Vintage Story mod dependency ranges use `minimum - maximum` syntax, such as `1.22.0-pre.1 - 1.22.1`.
- Reviewed Rider's reported `failed to parse solution file` state after the solution rename.
- Reviewed `server-main.log` from the testing installation and confirmed Hacked Locusts was failing on obsolete `locust-hacked.json` AI task paths.
- Reviewed Vintage Story 1.22.2 and 1.22.3 `locust.json` and `locust-hacked.json` data.
- Confirmed 1.22.x keeps sawblade attack data under hacked locust AI tasks `1` and `2`, not task `0`.
- Confirmed the sawblade locust shape uses the `locust` texture slot, while bronze and corrupt locust shapes use `skin`.

### Notes

- No vanilla API files were modified.
- C# naming was updated to match the `Ghaelen Tweaks` project name.
- The README intentionally describes gameplay effects, not implementation details.
- The packed dirt and rammed earth speed buffs are deliberately below stone paths, preserving stone paths as the fastest basic road surface.
- The bone knife uses the existing stone knife shape with a bone texture, avoiding new art assets.
- The bone knife does not override knife range; it uses the same knife item definition as other generic knives.
- Hacked Locusts was merged as content patches and texture assets; the old separate mod zip is no longer needed when using Ghaelen Tweaks.
- The tuning spear class restriction is vanilla behavior and is not changed by this mod.
- The inner project folder and `.csproj` file now use `Ghaelen Tweaks`.
- Rider should load the `Ghaelen Tweaks` project entry directly from `Ghaelen Tweaks.sln`.

### Verification

- Ran `dotnet build 'Ghaelen Tweaks.sln'`; build succeeded with 0 errors after the solution and project rename.
- Ran `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`; JSON validation succeeded.
- Confirmed `dotnet sln 'Ghaelen Tweaks.sln' list` finds `CakeBuild\CakeBuild.csproj` and `Ghaelen Tweaks\Ghaelen Tweaks.csproj`.
- Confirmed `dotnet sln 'Ghaelen Tweaks.sln' list` still finds both projects after normalizing the solution metadata.
- Confirmed `dotnet build 'Ghaelen Tweaks.sln'` still succeeds after normalizing the solution metadata.
- Confirmed `Ghaelen Tweaks.sln` now uses CRLF line endings consistently.
- The build emitted 2 existing SDK warnings that `net7.0` is out of support.
- Parsed `Ghaelen Tweaks/assets/survival/patches/mudbrick-gravel.json` as JSON successfully.
- Parsed `Ghaelen Tweaks/assets/survival/patches/soil-path-speed-buffs.json` as JSON successfully.
- Parsed `Ghaelen Tweaks/assets/survival/patches/bone-knife.json` as JSON successfully.
- Parsed `Ghaelen Tweaks/assets/game/patches/bone-knife-language.json` as JSON successfully.
- Parsed `Ghaelen Tweaks/modinfo.json` as JSON successfully after metadata updates.
- Confirmed the patch was copied to `Ghaelen Tweaks/bin/Debug/Mods/mod/assets/survival/patches/mudbrick-gravel.json`.
- Confirmed the patch was copied to `Ghaelen Tweaks/bin/Debug/Mods/mod/assets/survival/patches/soil-path-speed-buffs.json`.
- Confirmed the patch was copied to `Ghaelen Tweaks/bin/Debug/Mods/mod/assets/survival/patches/bone-knife.json`.
- Confirmed the language patch was copied to `Ghaelen Tweaks/bin/Debug/Mods/mod/assets/game/patches/bone-knife-language.json`.
- Confirmed the updated mod metadata was copied to `Ghaelen Tweaks/bin/Debug/Mods/mod/modinfo.json`.
- Confirmed the built output manifest contains `modid: "ghaelentweaks"` and `name: "Ghaelen Tweaks"`.
- Confirmed `E:\Gaming\Vintage Story\mods\Ghaelen Tweaks` is the active containing folder.
