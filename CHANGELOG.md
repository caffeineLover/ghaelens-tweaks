# Changelog

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
