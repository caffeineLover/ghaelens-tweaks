# Project Memory

Last updated: 2026-07-26

## Project overview

Ghaelen Tweaks is a Vintage Story mod collected under the `Ghaelen Tweaks/` project folder in the Git repository at `E:\Gaming\Vintage Story\mods\Ghaelen Tweaks`. It contains C# mod code, JSON assets, and player-facing documentation for gameplay tweaks.

## Current state

The mod project uses `Ghaelen Tweaks/Ghaelen Tweaks.csproj`. Documentation currently lives under `Ghaelen Tweaks/docs/`. Source code now lives under `Ghaelen Tweaks/src/`, with related source files grouped into feature-oriented subfolders where there is a clear functional grouping. The C# files have been updated to follow the shared comment and callable-member spacing standards. The mod also contains content patches for recipe and asset changes under `Ghaelen Tweaks/assets/`.

## Active work

No multi-step implementation is currently in progress. The most recent implementation work added Better Ruins blueprint learning, controlled by `betterruins-blueprint-learning` and enabled by default. Players can right-click Better Ruins schematic blueprints to learn them per player; learned schematic recipes can then be crafted without placing the physical blueprint in the crafting grid.

## Durable technical knowledge

- All source files must be placed under `Ghaelen Tweaks/src/`.
- Strongly related source files should be grouped in clearly named subfolders under `src/`.
- The SDK-style project file uses default compile includes, so C# files under `src/` are compiled without explicit `Compile Include` entries.
- Existing docs in `docs/CODEX_STATE.md` say not to run builds unless explicitly instructed by the user.
- C# source files must follow the shared Vintage Story mod coding standards: file-level `/* ... */` comments, `////` comments for callable members, internal `//` intent comments, and three blank lines before method comment blocks.
- Vintage Story 1.22.3 `survival/blocktypes/plant/reedpapyrus.json` defines tule as `tallplant-tule-*`. Normal tule drops `thatch`; harvested tule drops `tuleroot`.
- Vintage Story 1.22.3 `survival/recipes/grid/basket.json` uses the 3x2 pattern `L_L	LLL` or `P_P	PPP`, with quantity 2 per occupied slot, for cattail and papyrus handbaskets.
- Vintage Story 1.22.3 `RecipeBase.Enabled` can be changed at runtime. `InventoryCraftingGrid` checks `gridRecipe.Enabled` before matching recipes, so the tule handbasket config toggles the patched recipe by setting that flag after assets finalize and on Config Lib events.
- The palisade damage config toggle is `enable-palisade-damage-to-hostiles`; despite the concise setting name, the affected entities are lore creatures plus charging adult bears, wolves, and hyenas.
- For Vintage Story 1.22.4 research, the sibling Vintage Story Reference index contains BetterRuins `0.6.3` from Mod DB. Its `modinfo.json` declares `"type": "content"`, so there is no Better Ruins assembly to patch for blueprint behavior.
- Better Ruins `0.6.3` defines reusable blueprint/schematic items in `assets/betterruins/itemtypes/betterruins/schematic.json` as `betterruins:br-schematic-{type}`. The variant list has 30 types: `door`, `bed`, `book`, `chest`, `crate`, `gaslamp`, `jonaslamp`, `banner`, `table`, `stone`, `wood`, `ancient`, `candle`, `road`, `jonaspart`, `jonasassembly`, `mechanical`, `roofing`, `textureflipper`, `palisade`, `farmer`, `shipwright`, `weaver`, `artisan`, `pipes`, `gravedigger`, `cartwright`, `alchemist`, `carpenter`, and `toymaker`.
- Better Ruins schematic-gated recipes live under `assets/betterruins/recipes/grid/schematic-*/*.json`. A 2026-07-25 audit found 886 `br-schematic-*` recipe references and all checked references use `"consume": false`.
- Vintage Story 1.22.4 `GridRecipe.Matches` and `GridRecipe.ConsumeInput` both fail when a required ingredient slot is empty. Any feature that treats a learned schematic as a virtual ingredient must handle both matching and consumption, not only output preview.
- Vintage Story 1.22.4 `InventoryCraftingGrid.FindMatchingRecipe` builds candidate recipes from real input stacks via `FastSearchRecipesByIngredient`, then calls `GridRecipe.Matches`. Missing a schematic does not prevent candidate discovery if another real recipe ingredient is present, but the normal match fails later.
- Vintage Story 1.22.4 `IServerPlayer.SetModData<T>` / `GetModData<T>` provide permanently stored per-player mod data that is not automatically synced to clients. Learned schematic state can be persisted there, with custom network sync needed for client-side crafting preview and UI help.
- The Better Ruins blueprint-learning implementation stores learned schematic codes as full canonical item codes, such as `betterruins:br-schematic-ancient`, in server player mod data key `ghaelentweaks:betterruins-blueprint-knowledge`.
- The learned schematic sync channel is `ghaelentweaks-betterruins-blueprints` and carries a protobuf packet with the server feature toggle plus the player's full learned schematic code list.
- `GridRecipe.Matches` is patched on both client and server so exact, non-consuming Better Ruins schematic ingredients can be treated as virtual only when the matching physical schematic is absent from the crafting grid and the player has learned that schematic.
- `GridRecipe.ConsumeInput` is patched on both client and server. For a virtual schematic craft, it clones the recipe, nulls only the missing learned schematic ingredient slots, and lets vanilla consume the remaining inputs.
- Physical Better Ruins schematics in the crafting grid are passed through to vanilla recipe matching and consumption unchanged.

## Architecture and design decisions

- Source layout uses `src/` as the sole source root to match shared project standards and the user's explicit instruction.
- Current functional source groupings are:
  - `src/BlockBehaviors/` for registered block behavior classes.
  - `src/EntityBehaviors/` for registered entity behavior classes.
  - `src/Configuration/` for configuration models.
  - `src/Entities/` for shared entity predicate helpers.
  - `src/Systems/` for runtime systems.
  - `src/Patches/` for Harmony patch code.
- Learned Better Ruins schematics use direct Harmony patches instead of generated duplicate recipes. This avoids adding hundreds of duplicate Better Ruins recipes to the recipe registry and keeps physical blueprint behavior unchanged.
- `assets/ghaelentweaks/patches/betterruins-blueprint-learning.json` conditionally patches Better Ruins' schematic item with the `BetterRuinsBlueprintReading` collectible behavior only when mod id `betterruins` is loaded.

## External interfaces and integrations

- The project integrates with Vintage Story through the mod project, assets folder, and `modinfo.json`.
- Config Lib support is optional and documented by the shared project standards.
- Vintage Story research should use the sibling Vintage Story Reference repository according to `AGENTS.md`.

## Known failed approaches

None recorded yet.

## Bugs, risks, and limitations

- The Git repository root is one level above the mod project folder, while `AGENTS.md`, `docs/`, and `src/` are inside `Ghaelen Tweaks/`. Future agents should be explicit about whether a path is repository-root-relative or mod-project-relative.
- Build verification was not run after the source-layout move or coding-standards comment pass because existing project notes say not to run builds unless explicitly instructed. The 2026-07-15 release was explicitly requested, so build and package verification were run for version `0.3.1`.

## Open questions

- The repository root does not currently contain a root-level `AGENTS.md`, `docs/`, or `src/`; the active mod project keeps those files under `Ghaelen Tweaks/`.
- Better Ruins blueprint learning was implemented against Vintage Story Reference `1.22.4`, but the local StoryForge installs available during implementation were `1.22.0`, `1.22.1`, `1.22.2`, `1.22.3`, and `1.22.5`; no local StoryForge `1.22.4` install was present.

## Testing and verification

- Build command previously recorded in `docs/CODEX_STATE.md`: `dotnet build "Ghaelen Tweaks.sln"`.
- Build was not run during the 2026-07-15 source-layout and coding-standards sessions because explicit build permission was not given.
- Build was not run after the Better Ruins blueprint-learning implementation because project notes still require explicit build permission.
- Better Ruins blueprint-learning static checks run on 2026-07-26: parsed `assets/game/lang/en.json`, `assets/ghaelentweaks/config/configlib-patches.json`, `assets/ghaelentweaks/patches/betterruins-blueprint-learning.json`, and `modinfo.json` with PowerShell `ConvertFrom-Json`; `git diff --check` passed with only Git line-ending normalization warnings.
- Release `0.3.1` verification ran `dotnet build "Ghaelen Tweaks.sln"`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=Package`.
- The build and Cake tasks succeeded. CakeBuild still reports existing NuGet vulnerability warnings for its package dependencies.
- The release ZIP was created at `Releases/ghaelentweaks_0.3.1.zip`; the ZIP's packaged `modinfo.json` was checked and contained version `0.3.1`, mod id `ghaelentweaks`, and game dependency `1.22.3`.

## Useful commands

- List solution projects: `dotnet sln "Ghaelen Tweaks.sln" list`
- Build solution when explicitly approved: `dotnet build "Ghaelen Tweaks.sln"`
- Check Git status: `git status --short`

## Important files and code paths

- `Ghaelen Tweaks/AGENTS.md`: project instructions for coding agents.
- `Ghaelen Tweaks/Ghaelen Tweaks.csproj`: C# mod project.
- `Ghaelen Tweaks/src/GhaelenTweaksModSystem.cs`: main mod system registration and lifecycle.
- `Ghaelen Tweaks/src/Configuration/GhaelenTweaksConfig.cs`: mod configuration model.
- `Ghaelen Tweaks/src/BetterRuins/BetterRuinsBlueprintKnowledge.cs`: server persistence and client sync for learned Better Ruins schematic codes.
- `Ghaelen Tweaks/src/BetterRuins/BetterRuinsBlueprintRecipePatches.cs`: Harmony patches for virtual learned schematic recipe matching and consumption.
- `Ghaelen Tweaks/src/BetterRuins/CollectibleBehaviorBetterRuinsBlueprintReading.cs`: right-click behavior that records Better Ruins schematic knowledge.
- `Ghaelen Tweaks/src/Patches/PersistentCraftingGridPatches.cs`: Harmony patches for persistent crafting grid behavior.
- `Ghaelen Tweaks/src/Systems/PalisadeDamageSystem.cs`: server-side palisade damage system.
- `Ghaelen Tweaks/assets/`: Vintage Story assets and mod data.
- `Ghaelen Tweaks/assets/ghaelentweaks/patches/betterruins-blueprint-learning.json`: conditional Better Ruins schematic item behavior patch.
- `Ghaelen Tweaks/assets/survival/patches/tule-handbasket.json`: appends a handbasket recipe named `tule-handbasket` using `thatch` in the vanilla handbasket pattern.
- `Ghaelen Tweaks/docs/CODEX_STATE.md`: older persistent session notes that may contain useful historical context.

## Session history

### 2026-07-26

- Implemented Better Ruins blueprint learning on `master` after the user chose not to create a separate branch.
- Added `betterruins-blueprint-learning` to the mod config model, Config Lib metadata, English language text, README, and changelog.
- Added a conditional Better Ruins asset patch that appends `BetterRuinsBlueprintReading` to `betterruins:itemtypes/betterruins/schematic.json`.
- Added server-owned learned schematic persistence, client sync, and tooltip status in `src/BetterRuins/BetterRuinsBlueprintKnowledge.cs`.
- Added `CollectibleBehaviorBetterRuinsBlueprintReading` so right-clicking a Better Ruins schematic learns the corresponding canonical schematic item code without consuming the item.
- Added Harmony prefixes for `GridRecipe.Matches` and `GridRecipe.ConsumeInput` so learned exact Better Ruins schematic ingredients can be virtual when the physical schematic is absent from the grid.
- Added a `protobuf-net.dll` reference from the configured Vintage Story install because the learned schematic sync packet uses Vintage Story's protobuf network channel serialization.
- Verified modified JSON files parse with `ConvertFrom-Json` and ran `git diff --check`; only line-ending normalization warnings were reported. Did not run `dotnet build` because explicit build permission was not given.
- Fixed source errors in `BetterRuinsBlueprintRecipePatches.cs` by adding the `Vintagestory.API.Datastructures` namespace needed for `Tags.Matches(...)` and by guarding nullable schematic recipe ingredients before calling `SatisfiesAsIngredient(...)`.

### 2026-07-25

- Investigated a requested learned Better Ruins blueprint feature against Vintage Story Reference `1.22.4`.
- Confirmed BetterRuins `0.6.3` is content-only and its blueprint mechanics are asset/recipe driven.
- Found the Better Ruins schematic item family `betterruins:br-schematic-{type}` and confirmed schematic recipe ingredients are reusable through `"consume": false`.
- Reviewed Vintage Story 1.22.4 crafting internals and confirmed learned schematic support is feasible but requires code, persistent per-player server data, client sync for preview, and recipe matching/consumption changes.

### 2026-07-15

- Read `AGENTS.md`, shared project memory instructions, shared project standards, and shared coding standards before making repository changes.
- Moved tracked C# source files from the mod project root into `Ghaelen Tweaks/src/`.
- Grouped source files by functionality under `src/` where appropriate.
- Added a source-layout section to `AGENTS.md`.
- Created this `PROJECT_MEMORY.md` file because it was missing.
- Added standards-compliant comments and callable-member spacing across all C# source files under `src/`.
- Converted `EntityBehaviorCatLoreGuardian` from a primary-constructor class to an explicit constructor so the constructor can be documented according to the shared standards.
- Replaced the `PalisadeDamageSystem.PositionSnapshot` record struct with a small readonly struct so its constructor and helper method can be documented explicitly.
- Ran a text audit for file headers, method/constructor comment placement, and three-blank-line method spacing. Ran `git diff --check`; it reported only Git line-ending normalization warnings, not whitespace errors.
- Added `assets/survival/patches/tule-handbasket.json`, a server-side patch against `game:recipes/grid/basket.json`.
- The new recipe uses `thatch` in the vanilla handbasket shape with quantity 2 per occupied slot and outputs `basket-normal-reed`.
- Updated `docs/README.md` with a player-facing Tule Handbasket Recipe section.
- Verified the new patch file parses as JSON with PowerShell `ConvertFrom-Json`. Did not run a build because explicit build permission was not given.
- Bumped `modinfo.json` from `0.3.0` to `0.3.1`.
- Added `docs/RELEASES.md` with a `0.3.1` entry for tag `Tule-Handbasket`.
- Added the `0.3.1` entry to `docs/CHANGELOG.md`.
- Corrected `docs/README.md` to say the current mod metadata depends on Vintage Story `game: 1.22.3`.
- Created the Git release tag `Tule-Handbasket` for version `0.3.1`.
- Built the release artifact `Releases/ghaelentweaks_0.3.1.zip` from the tagged commit and opened the `Releases/` folder in File Explorer.
- Added `GhaelenTweaksConfig.TuleHandbasket` serialized as `tule-handbasket`, defaulting to `true`.
- Added the `tule-handbasket` boolean to `assets/ghaelentweaks/config/configlib-patches.json` and English Config Lib text.
- Changed the patched recipe name from `basket` to `tule-handbasket` so code can identify it without touching vanilla basket recipes.
- Added `GhaelenTweaksModSystem.AssetsFinalize()` and Config Lib event handling to apply the setting to the recipe's runtime `Enabled` flag.
- Renamed the active palisade damage toggle from `enable-palisade-damage` to `enable-palisade-damage-to-hostiles` in the config model, Config Lib metadata, English strings, event handling, and README.
