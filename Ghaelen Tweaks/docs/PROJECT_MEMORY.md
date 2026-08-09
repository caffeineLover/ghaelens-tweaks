# Project Memory

Last updated: 2026-08-09

## Project overview

Ghaelen Tweaks is a Vintage Story mod collected under the `Ghaelen Tweaks/` project folder in the Git repository at `E:\Gaming\Vintage Story\mods\Ghaelen Tweaks`. It contains C# mod code, JSON assets, and player-facing documentation for gameplay tweaks.

## Current state

The mod project uses `Ghaelen Tweaks/Ghaelen Tweaks.csproj`. Documentation currently lives under `Ghaelen Tweaks/docs/`. Source code now lives under `Ghaelen Tweaks/src/`, with related source files grouped into feature-oriented subfolders where there is a clear functional grouping. The C# files have been updated to follow the shared comment and callable-member spacing standards. The mod also contains content patches for recipe and asset changes under `Ghaelen Tweaks/assets/`. Release `0.4.5` fixes marking chalk floor and ceiling arrow orientation.

## Active work

No active implementation work is currently in progress. The latest completed source change orients floor and ceiling marking chalk arrows from the player's facing at placement time while preserving the tested wall-arrow cell mapping.

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
- Cat Lore Warning only applies cat glow and yowl warning behavior. Ghaelen Tweaks does not cancel cat damage and no longer has a `cat-impervious-to-lore-creatures` config setting.
- Vintage Story Reference `1.22.3` PetAI `5.1.1` defaults `PetConfig.FalldamageOff` to `true`, and `EntityBehaviorTameable.OnEntityReceiveDamage` cancels `EnumDamageSource.Fall` damage for tameable entities while that PetAI config is enabled. Cats `5.0.1` and WolfTaming `5.0.1` attach the `tameable` behavior and do not add their own fall-damage override, so tamed cats and dogs inherit PetAI's default fall-damage immunity unless `petconfig.json` turns it off.
- Marking chalk uses `ItemMarkingChalk` in `src/Items/ItemMarkingChalk.cs`. It places `ghaelentweaks:markingchalk-{color}-{col}-{row}` decor blocks with `IBlockAccessor.SetDecor(..., blockSel.ToDecorIndex())`, so the target block remains unchanged and the adjacent block space is not occupied.
- Marking chalk is stackable while sticks are fresh. A partially used stick stores its remaining uses in stack attribute `markingChalkUsesLeft` and is split into a one-item stack so the fresh remainder is not visually or mechanically treated as partially used. If inventory cannot accept the fresh remainder during the split, it drops near the player.
- Marking chalk mode selection uses stack attribute `markingChalkMode`. Current modes are arrow up, arrow right, arrow down, arrow left, X, dot, ladder, stairs, danger, and exit. Wall arrow modes map to `col`/`row` spritesheet cells directly; right and left modes intentionally map to the opposite-looking source cells because in-game surfacelayer rendering mirrors horizontal arrows on tested wall faces. Floor and ceiling arrow modes use the up-arrow cell plus `DecorBits.Rotation` derived from player yaw, because top/bottom face UV axes are fixed to world directions and made arrow-left look like arrow-up in the 2026-08-09 `0.4.4` local test.
- Marking chalk erase mode is the last tool mode so existing saved draw-mode indices keep their meaning. Shift/crouch right-click also erases regardless of the selected draw mode. Successful erasing searches nearby sub-face decor cells on the clicked face, calls exact `BreakDecor(..., decorIndex)` only for `ghaelentweaks:markingchalk-*` decor, and restores one active-stick use capped at the configured maximum.
- Marking chalk surfacelayer art follows vanilla cave art: one 96x96 spritesheet per color, with `col` and `row` block variants selecting a 16x16 cell. Standalone per-symbol surfacelayer textures rendered as filled squares during the first in-game test.
- Marking chalk placement removes older Ghaelen Tweaks chalk decor in the same face subcell but with a different rotation after a successful placement. Decor rotation is part of the storage key, so this prevents rotated floor/ceiling redraws from stacking multiple marks in one subcell.
- Marking chalk valid surfaces are intentionally narrow: solid Stone/Ore/Brick material faces, trunk/log-like Wood paths beginning `log-`, `logsection-`, `logquad-`, or `lognarrow-`, and prepared Soil paths beginning `packeddirt`, `drypackeddirt`, or `rammed-`.
- Marking chalk config keys are `marking-chalk-uses` (default 32, clamped 1..512) and `marking-chalk-dye-batch-size` (default 16, clamped 1..64). Config Lib metadata lives in `assets/ghaelentweaks/config/configlib-patches.json`.
- Marking chalk dyeing supports vanilla liquid dyes `black`, `blue`, `gray`, `green`, `orange`, `pink`, `purple`, `red`, `white`, and `yellow`; `dye-woad` outputs blue marking chalk because vanilla `dye-woad` and `dye-blue` are visually identical in 1.22.3.
- Marking chalk recipe quantity config is applied by `MarkingChalkRecipeSettings` after assets finalize and after Config Lib changes. Grid recipes are updated directly; barrel recipes are updated via the public `barrelrecipes` registry and reflection to avoid adding a compile-time dependency on the survival assembly.
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
- `/gtweak schematics` is registered server-side through the Vintage Story `api.ChatCommands` builder API. It requires a player caller and the ordinary `Privilege.chat` permission.
- The schematics command prints the caller's authoritative server-side learned schematic set. It formats resolvable schematic item names with the stored canonical item code, and falls back to the code if the item cannot be resolved.
- Vanilla display cases cannot stack directly because their block JSON has `UnstableFalling` and `sidesolid: { all: false }`; the falling placement check asks the lower block's `CanAttachBlockAt(..., BlockFacing.UP, ...)` and fails with `requiresolidground` when the support is not attachable. Ghaelen Tweaks adds `BlockBehaviorDisplayCaseStackingSupport` to normal and tall display cases through `assets/survival/patches/display-case-stacking.json`. The behavior only answers true for display-case-on-display-case top-face attachment when `display-case-stacking` is enabled, so display cases are not made generally solid for unrelated blocks.
- Vintage Story 1.22.3 clutter item stacks store their specific clutter variant in stack attribute `type`. The wooden clutter recycling recipes use exact attribute matches because `CollectibleObject.Satisfies(...)` compares ingredient stack attributes as a subset of the input stack attributes.
- Current wooden clutter recycling targets `barricade1..6`, `rubble-wood1..4`, `table-ruined1..6`, `crate/crate-small-stacked`, `crate/crate-large-rot`, and `chestrubble`. Axe recipes return `game:agedfirewood`; saw recipes return `game:plank-aged`; all tool ingredients set `toolDurabilityCost` to `0`.
- The large rot crate recipes use `returnedStack` on the consumed clutter ingredient to grant 32 `game:rot` in addition to the visible aged wood output. Vintage Story puts returned stacks in player inventory or drops them near the player if inventory space is unavailable.
- `ClutterFuelPatches` applies direct firepit fuel to selected `game:clutter` stack types at 700 C. Burn duration is `24 seconds * recovered aged firewood count`, so direct burning matches the total fuel value of axe-recycling and then burning the recovered aged firewood.
- Vintage Story's handbook builds a generic fuel stack list by calling `GetCombustibleProperties(...)` for every stack. Dynamic `game:clutter` fuels can enter that list even though their fuel value depends on stack attributes rather than static `Collectible.CombustibleProps`. `ClutterFuelHandbookPatches` removes this mod's dynamic clutter fuels from `CollectibleBehaviorHandbookTextAndExtraInfo.addCreatedByInfo(...)` and `addProcessesIntoInfo(...)` fuel lists to avoid crashes in handbook integrations that assume static combustible properties.
- A Culinary Artillery `2.0.0-dev.16` defines `ACulinaryArtillery.Util.HandbookInfoExtensions.getCanSimmer(List<ItemStack> fuels, ItemStack stack)`. Decompilation on 2026-08-07 showed it orders `fuels` by `fuel.Collectible.CombustibleProps.BurnTemperature` with no null guard. Because ACA can call this helper inside its own Harmony prefix before this mod's vanilla handbook prefix runs, `ClutterFuelHandbookPatches` also optionally prefixes ACA's `getCanSimmer` helper directly when that type is loaded.

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
- `assets/survival/patches/display-case-stacking.json` patches vanilla normal and tall display cases with a registered block behavior rather than changing `sidesolid`. This keeps the tweak narrow and avoids making display cases act as general block support.
- Marking chalk uses a dedicated custom item behavior instead of vanilla `CollectibleBehaviorArtPigment` so it can choose color-specific decor, enforce fixed uses, restrict surfaces to logs/stone/brick/prepared soil, and leave room for a future freehand mode.

## External interfaces and integrations

- The project integrates with Vintage Story through the mod project, assets folder, and `modinfo.json`.
- Config Lib support is optional and documented by the shared project standards.
- Vintage Story research should use the sibling Vintage Story Reference repository according to `AGENTS.md`.

## Known failed approaches

None recorded yet.

## Bugs, risks, and limitations

- The Git repository root is one level above the mod project folder, while `AGENTS.md`, `docs/`, and `src/` are inside `Ghaelen Tweaks/`. Future agents should be explicit about whether a path is repository-root-relative or mod-project-relative.
- Build verification was not run after the source-layout move or coding-standards comment pass because existing project notes say not to run builds unless explicitly instructed. The 2026-07-15 release was explicitly requested, so build and package verification were run for version `0.3.1`.
- Marking chalk stacks from `0.4.2` or `0.4.3` saves may still carry a shared `markingChalkUsesLeft` attribute until the player next draws or erases with that stack. The `0.4.4` code migrates that case by splitting one active stick from the fresh remainder or clearing the attribute when the active stick is full.
- Marking chalk floor and ceiling arrows placed before `0.4.5` may keep their old fixed-UV orientation until erased and redrawn.

## Open questions

- The repository root does not currently contain a root-level `AGENTS.md`, `docs/`, or `src/`; the active mod project keeps those files under `Ghaelen Tweaks/`.
- Better Ruins blueprint learning was implemented against Vintage Story Reference `1.22.4`, but the local StoryForge installs available during implementation were `1.22.0`, `1.22.1`, `1.22.2`, `1.22.3`, and `1.22.5`; no local StoryForge `1.22.4` install was present.

## Testing and verification

- Build command previously recorded in `docs/CODEX_STATE.md`: `dotnet build "Ghaelen Tweaks.sln"`.
- Build was not run during the 2026-07-15 source-layout and coding-standards sessions because explicit build permission was not given.
- Build was not run after the Better Ruins blueprint-learning implementation because project notes still require explicit build permission.
- Better Ruins blueprint-learning and `0.3.2` version-bump static checks run on 2026-07-26: parsed `assets/game/lang/en.json`, `assets/ghaelentweaks/config/configlib-patches.json`, `assets/ghaelentweaks/patches/betterruins-blueprint-learning.json`, and `modinfo.json` with PowerShell `ConvertFrom-Json`; `git diff --check` passed with only Git line-ending normalization warnings.
- Release `0.3.2` pre-tag verification ran `dotnet build "Ghaelen Tweaks.sln"`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=Package` on 2026-07-26. The initial build exposed two Better Ruins nullable warnings, which were fixed before packaging; the final project-code build was clean aside from existing CakeBuild NuGet advisory warnings.
- Release `0.3.3` verification ran `git diff --check`, `dotnet build "Ghaelen Tweaks.sln"`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=Package` on 2026-07-26. The only reported warnings were existing CakeBuild NuGet advisory warnings and Git line-ending normalization warnings.
- Release `0.3.1` verification ran `dotnet build "Ghaelen Tweaks.sln"`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=Package`.
- The build and Cake tasks succeeded. CakeBuild still reports existing NuGet vulnerability warnings for its package dependencies.
- The release ZIP was created at `Releases/ghaelentweaks_0.3.3.zip`; the ZIP's packaged `modinfo.json` was checked and contained version `0.3.3`, mod id `ghaelentweaks`, and game dependency `1.22.3`.
- The release ZIP was created at `Releases/ghaelentweaks_0.3.1.zip`; the ZIP's packaged `modinfo.json` was checked and contained version `0.3.1`, mod id `ghaelentweaks`, and game dependency `1.22.3`.
- The 2026-07-29 wooden clutter update parsed `clutter-barricade-recycling.json` and `clutter-wood-recycling.json` with PowerShell `ConvertFrom-Json`; the files contained 12 and 24 recipes respectively. `git diff --check` reported only the repository's existing CRLF normalization warnings. `dotnet build` was not run because `docs/CODEX_STATE.md` still requires explicit build permission.
- Release `0.3.5` pre-tag verification ran `git diff --check`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet build "Ghaelen Tweaks.sln"` on 2026-07-29. `git diff --check` reported only the repository's existing CRLF normalization warnings after a release-note trailing-space fix. JSON validation and the solution build passed. The only build warnings were existing CakeBuild NuGet advisory warnings.
- The post-`0.3.5` clutter-fuel handbook compatibility fix was verified with `dotnet build "Ghaelen Tweaks.sln"` on 2026-07-29. The solution build passed; the only warnings were existing CakeBuild NuGet advisory warnings.
- Release `0.3.6` pre-tag verification ran `git diff --check`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, `dotnet build "Ghaelen Tweaks.sln"`, and `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=Package` on 2026-07-29. The ZIP was created at `Releases/ghaelentweaks_0.3.6.zip`; its packaged `modinfo.json` was checked and contained version `0.3.6`, mod id `ghaelentweaks`, and game dependency `1.22.3`. The only warnings were existing CakeBuild NuGet advisory warnings and Git line-ending normalization warnings.
- Release `0.3.8` pre-tag verification ran `git diff --check`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet build "Ghaelen Tweaks.sln"` on 2026-08-07. The whitespace check reported only the repository's existing CRLF normalization warnings. JSON validation and the solution build passed; the only build warnings were existing CakeBuild NuGet advisory warnings. The installed ACA `2.0.0-dev.16` DLL was inspected with a temporary `ilspycmd` tool install under `%TEMP%` to confirm the exact helper body and signature.
- The 2026-08-09 cat immunity removal parsed `assets/game/lang/en.json` and `assets/ghaelentweaks/config/configlib-patches.json` with PowerShell `ConvertFrom-Json`. A targeted `rg` scan found no active source or asset references to `CatImpervious`, `cat-impervious`, `OnEntityReceiveDamage`, or `damage = 0`; `git diff --check` reported only the repository's existing CRLF normalization warnings. `dotnet build` was not run because explicit build permission was not given.
- The 2026-08-09 marking chalk implementation parsed the new strict JSON assets and modified language/config JSON with PowerShell `ConvertFrom-Json`, generated 100 block mark PNGs and 10 item PNGs, visually inspected sample textures, and ran `git diff --check`; only the repository's existing CRLF normalization warnings were reported. `dotnet build` was not run because explicit build permission was not given.

## Useful commands

- List solution projects: `dotnet sln "Ghaelen Tweaks.sln" list`
- Build solution when explicitly approved: `dotnet build "Ghaelen Tweaks.sln"`
- Check Git status: `git status --short`

## Important files and code paths

- `Ghaelen Tweaks/AGENTS.md`: project instructions for coding agents.
- `Ghaelen Tweaks/Ghaelen Tweaks.csproj`: C# mod project.
- `Ghaelen Tweaks/src/GhaelenTweaksModSystem.cs`: main mod system registration and lifecycle.
- `Ghaelen Tweaks/src/Configuration/GhaelenTweaksConfig.cs`: mod configuration model.
- `Ghaelen Tweaks/src/Items/ItemMarkingChalk.cs`: marking chalk item behavior for preset glyph placement, tool modes, surface filtering, and stackable use tracking.
- `Ghaelen Tweaks/src/Systems/MarkingChalkRecipeSettings.cs`: applies configured marking chalk dye batch size to resolved grid and barrel recipes.
- `Ghaelen Tweaks/src/BetterRuins/BetterRuinsBlueprintKnowledge.cs`: server persistence and client sync for learned Better Ruins schematic codes.
- `Ghaelen Tweaks/src/BetterRuins/BetterRuinsBlueprintRecipePatches.cs`: Harmony patches for virtual learned schematic recipe matching and consumption.
- `Ghaelen Tweaks/src/BetterRuins/CollectibleBehaviorBetterRuinsBlueprintReading.cs`: right-click behavior that records Better Ruins schematic knowledge.
- `Ghaelen Tweaks/src/GhaelenTweaksChatCommands.cs`: server-side `/gtweak` command registration, including `/gtweak schematics`.
- `Ghaelen Tweaks/src/Patches/PersistentCraftingGridPatches.cs`: Harmony patches for persistent crafting grid behavior.
- `Ghaelen Tweaks/src/Patches/ClutterFuelPatches.cs`: Harmony postfix that adds combustible properties for selected `game:clutter` item-stack variants.
- `Ghaelen Tweaks/src/Systems/PalisadeDamageSystem.cs`: server-side palisade damage system.
- `Ghaelen Tweaks/assets/`: Vintage Story assets and mod data.
- `Ghaelen Tweaks/assets/ghaelentweaks/recipes/grid/clutter-barricade-recycling.json`: explicit axe/saw grid recipes for clutter barricade variants.
- `Ghaelen Tweaks/assets/ghaelentweaks/recipes/grid/clutter-wood-recycling.json`: explicit axe/saw grid recipes for wood rubble, ruined tables, stacked small crates, large crates with rot, and chest rubble.
- `Ghaelen Tweaks/assets/ghaelentweaks/recipes/grid/marking-chalk.json`: plain marking chalk recipe and bowl dyeing recipes.
- `Ghaelen Tweaks/assets/ghaelentweaks/recipes/barrel/marking-chalk.json`: barrel dyeing recipes for marking chalk.
- `Ghaelen Tweaks/assets/ghaelentweaks/blocktypes/overlay/marking-chalk.json`: hidden surfacelayer decor blocks for placed marking chalk glyphs.
- `Ghaelen Tweaks/docs/MARKING_CHALK.md`: living marking chalk design and implementation note.
- `Ghaelen Tweaks/assets/ghaelentweaks/patches/betterruins-blueprint-learning.json`: conditional Better Ruins schematic item behavior patch.
- `Ghaelen Tweaks/assets/survival/patches/tule-handbasket.json`: appends a handbasket recipe named `tule-handbasket` using `thatch` in the vanilla handbasket pattern.
- `Ghaelen Tweaks/docs/CODEX_STATE.md`: older persistent session notes that may contain useful historical context.

## Session history

### 2026-08-09

- Added initial marking chalk support on the primary local branch (`master`; no separate local `main` branch exists). New stackable `marking-chalk-{color}` items place hidden surfacelayer decor blocks on valid solid stone/ore/brick, log/trunk, packed dirt, dry packed dirt, and rammed earth faces.
- Added tool modes for arrow, X, dot, ladder, stairs, danger, and exit glyphs. The first implementation used a single arrow mode that selected a cardinal arrow variant from the player's yaw.
- Added configurable marking chalk use count (`marking-chalk-uses`, default 32) and dye batch size (`marking-chalk-dye-batch-size`, default 16), plus Config Lib metadata and English language text.
- Added recipes: one vanilla `stone-chalk` crafts four plain marking chalk sticks; bowl recipes consume 1 L dye from a fired bowl and recolor the configured batch size while leaving the bowl; barrel recipes consume 1 L dye and recolor the configured batch. `dye-woad` outputs blue marking chalk.
- Added generated item and mark textures: 10 item PNGs and 100 transparent decor glyph PNGs, plus SVG mode icons.
- Updated `docs/MARKING_CHALK.md` from brainstorm draft to the first implementation note. Verified new strict JSON with `ConvertFrom-Json`, checked sample textures visually, and ran `git diff --check`; build was not run because explicit build permission was not given.
- After the first in-game test, screenshots at `C:\Users\p\Pictures\Vintagestory\2026-08-09_09-30-43.png` and `2026-08-09_09-31-00.png` showed marking chalk arrow and X decor rendering as filled squares. The likely cause was using standalone symbol PNGs with `drawtype: surfacelayer`; vanilla cave-art surfacelayer blocks use `col` and `row` variants over a spritesheet.
- Reworked marking chalk decor blocks to `markingchalk-{color}-{col}-{row}` and generated one 96x96 spritesheet per color with explicit cells for arrow up, arrow right, arrow down, arrow left, X, dot, ladder, stairs, danger, and exit.
- Replaced the single yaw-derived arrow mode with explicit arrow up, arrow right, arrow down, and arrow left tool modes and SVG icons so players can directly select the arrow they want.
- Added marking chalk erase mode, then updated it after local-test feedback so Shift/crouch right-click also erases. Successful erasing removes nearby Ghaelen Tweaks chalk decor from the clicked face through exact sub-face `BreakDecor` and restores one active-stick use, capped at the configured maximum.
- The 2026-08-09 11:08 local test loaded `ghaelentweaks_0.4.0.zip`, not the current working tree or the `0.4.1` hotfix. Logs and screenshots showed old block codes such as `markingchalk-purple-x` and `markingchalk-purple-danger`, so the observed filled-square marks were from the old standalone overlay texture implementation.
- The 2026-08-09 12:40 local test correctly loaded `ghaelentweaks_0.4.1.zip`, so the new spritesheet marks appeared. Erase still could not work in that test because the installed `0.4.1` package predates the unreleased erase implementation and contains no erase icon or erase language key.
- The first `0.4.2` local test confirmed erasing works, but right and left arrow modes were reversed in game. Release `0.4.3` keeps the toolbar icons literal and swaps only the placed-decor cell mapping; it also thins the danger glyph base so the exclamation dot stays separate.
- The first `0.4.3` local test confirmed the danger symbol looks good. The exit symbol was still unclear, and stacked chalk showed a shared use count across all items in the stack. Release `0.4.4` replaces the exit cell with a symmetric doorway marker and changes use tracking so the first partial use splits one stick away from the fresh stack.
- The first `0.4.4` local test confirmed the exit glyph and stack-use split improved, but arrow-left on a floor rendered like arrow-up while the same mode looked correct on a wall. Release `0.4.5` uses decor rotation bits for floor/ceiling arrows and clears same-subcell rotated chalk overlaps.
- Removed `EntityBehaviorCatLoreGuardian.OnEntityReceiveDamage`, so Ghaelen Tweaks no longer cancels cat fall damage or lore-creature damage. The cat behavior still applies proximity glow on the client and warning yowls on the server.
- Removed the `cat-impervious-to-lore-creatures` config property, Config Lib setting, English language labels, and README player-facing documentation. Added an Unreleased changelog entry for the removed immunity guardrail.
- Researched Vintage Story Reference `1.22.3` PetAI `5.1.1`, Cats `5.0.1`, and WolfTaming `5.0.1`: PetAI loads/stores `petconfig.json`, defaults `FalldamageOff = true`, and cancels fall damage in `EntityBehaviorTameable.OnEntityReceiveDamage`; Cats and WolfTaming attach `tameable` and do not override that fall-damage path.
- Verified edited JSON files parse with `ConvertFrom-Json`, verified no active source or asset references to the removed immunity setting remain with `rg`, and ran `git diff --check`; only the repository's existing CRLF normalization warnings were reported. Did not run `dotnet build` because the project notes require explicit build permission.

### 2026-08-07

- Investigated a new crash report from Vintage Story `1.22.6` with `ghaelentweaks@0.3.7` and `aculinaryartillery@2.0.0-dev.16`. The latest `client-main.log` confirmed `ghaelentweaks_0.3.7.zip` loaded and initialized `Clutter fuel handbook compatibility feature initialized.`, but the crash still occurred in `ACulinaryArtillery.Util.HandbookInfoExtensions.getCanSimmer(...)`.
- Confirmed from the stack trace and logs that the new crash path was ACA's `GetHandbookProcessesIntoPatch.Prefix(...)`, meaning ACA can inspect the `fuels` list before this mod's vanilla `addProcessesIntoInfo(...)` prefix sanitizes it.
- Inspected the installed ACA `2.0.0-dev.16` DLL from the StoryForge `havoc` cache. Reflection found `HandbookInfoExtensions.getCanSimmer(List<ItemStack> fuels, ItemStack stack)`, and decompilation showed the helper sorts by `fuel.Collectible.CombustibleProps.BurnTemperature` without null checks.
- Updated `ClutterFuelHandbookPatches` so its vanilla handbook prefixes use `Priority.First` and so it optionally prefixes ACA's `getCanSimmer` helper directly when ACA is loaded. The direct helper patch removes this mod's dynamic clutter fuels before ACA sorts the fuel list, independent of vanilla handbook prefix order.
- Bumped `modinfo.json` to `0.3.8`, moved the changelog entry to `0.3.8 - 2026-08-07`, and added the `docs/RELEASES.md` entry for tag `v0.3.8`.
- Release `0.3.8` pre-tag verification ran `git diff --check`, `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ValidateJson`, and `dotnet build "Ghaelen Tweaks.sln"`. The checks passed with only the existing CRLF normalization and CakeBuild NuGet advisory warnings.

### 2026-08-04

- Investigated a submitted crash report from Vintage Story `1.22.6` with `ghaelentweaks@0.3.6` and `aculinaryartillery@2.0.0-dev.16`. The crash still occurred in `ACulinaryArtillery.Util.HandbookInfoExtensions.getCanSimmer(...)`, but this time through ACA's `GetHandbookProcessesIntoPatch` path rather than the older `GetHandbookCreatedByPatch` path.
- Confirmed the player was not running an old Ghaelen Tweaks version: `client-main.log` loaded `mod@ghaelentweaks_0.3.6.zip`, and `Clutter fuel handbook compatibility feature initialized.` showed the `0.3.6` patch was active.
- Extended `ClutterFuelHandbookPatches` so the same `fuels` list sanitizer prefixes both `CollectibleBehaviorHandbookTextAndExtraInfo.addCreatedByInfo(...)` and `addProcessesIntoInfo(...)`. This keeps runtime clutter burning intact while protecting both vanilla handbook relationship paths before ACA inspects them.
- Bumped `modinfo.json` to `0.3.7` and added `0.3.7` entries to `docs/CHANGELOG.md` and `docs/RELEASES.md`.

### 2026-08-02

- Investigated the user's report of a client crash while cutting down a pine tree using the attached StoryForge `havoc` logs. The current `client-main.log` and `client-debug.log` were from 2026-08-02 and loaded `ghaelentweaks_0.3.6.zip`; the `Clutter fuel handbook compatibility feature initialized.` line confirmed the 0.3.6 handbook fuel-list fix applied on the client.
- The attached `client-crash.log` was older, from 2026-07-29, and still showed the known A Culinary Artillery handbook crash in `ACulinaryArtillery.Util.HandbookInfoExtensions.getCanSimmer(...)`. No fresh 2026-08-02 crash log was present in the active log folder.
- Around the reported 2026-08-02 tree-cutting window, `client-debug.log` recorded `forestry, 3 Level up` at 13:39:58 and continued receiving player/inventory data afterward. `client-main.log` ended at 13:40:02 with a window resize/minimize notification, not an exception. This provided no evidence that Ghaelen Tweaks caused a pine/tree-breaking crash.
- A source/content scan found no Ghaelen Tweaks patches for pine trees, vanilla logs, leaves, or normal tree-felling behavior. The only break/drop customization involving wood remains palisade dismantling; the clutter fuel/recycling features target `game:clutter` stacks, not trees.

### 2026-07-29

- Diagnosed the user's 2026-07-29 StoryForge screenshots and logs: the active `havoc` profile loaded `ghaelentweaks_0.3.3.zip`, not the newly released `0.3.4` zip, so the barricade recipe/fuel changes were absent from that runtime.
- Confirmed that vanilla "Wood rubble" is a separate clutter family using `type=rubble-wood1..4`, so it was not covered by the barricade-only `0.3.4` recipes or fuel rule.
- Added `assets/ghaelentweaks/recipes/grid/clutter-wood-recycling.json` for wood rubble, ruined tables, stacked small crates, and chest rubble. Axe recipes return aged firewood; saw recipes return aged boards; all tool durability costs are zero.
- Updated `ClutterFuelPatches` so barricades now burn for 96 seconds and the added wooden clutter burns at 700 C with duration scaled from axe recovery value.
- Updated README, recycling notes, and changelog for the expanded wooden clutter recycling behavior.
- Added large crate with rot recycling using vanilla clutter `type=crate/crate-large-rot`; both axe and saw recipes return 32 rot as a `returnedStack`, plus 6 aged firewood or 6 aged boards as the visible output.
- Bumped `modinfo.json` to `0.3.5` and prepared release notes for tag `v0.3.5`.
- Diagnosed a crash opening the handbook with `ghaelentweaks@0.3.5` and `aculinaryartillery@2.0.0-dev.16`: the stack trace points to A Culinary Artillery processing the handbook `fuels` list, where this mod's dynamic clutter fuel stacks can lack static collectible combustible props.
- Added `ClutterFuelHandbookPatches`, a client-side Harmony prefix that removes this mod's dynamic clutter fuel stacks from generic handbook fuel lists while preserving runtime `GetCombustibleProperties(...)` behavior for firepits.
- Bumped `modinfo.json` to `0.3.6`, added release notes for tag `v0.3.6`, and packaged the hotfix as `Releases/ghaelentweaks_0.3.6.zip`.

### 2026-07-28

- Added display case stacking support. New file `src/BlockBehaviors/BlockBehaviorDisplayCaseStackingSupport.cs` lets normal and tall display cases attach to the top face of another display case when the feature toggle is enabled.
- Registered `DisplayCaseStackingSupport` in `GhaelenTweaksModSystem` and patched `game:blocktypes/wood/displaycase.json` plus `game:blocktypes/wood/displaycase-tall.json` through `assets/survival/patches/display-case-stacking.json`.
- Added the `display-case-stacking` config property, Config Lib metadata, English language text, README section, and Unreleased changelog entry.
- Verified the new display-case patch, Config Lib metadata, and English language JSON parse with `ConvertFrom-Json`. Ran `git diff --check`; it reported only existing CRLF normalization warnings. Did not run `dotnet build` because project notes require explicit build permission.

### 2026-07-26

- Added `/gtweak schematics`, a server-side chat command that lists the calling player's memorized Better Ruins schematic blueprints.
- The command uses the authoritative persisted server knowledge rather than the client's synced preview cache, and it requires only the normal chat privilege.
- Bumped `modinfo.json` from `0.3.2` to `0.3.3`.
- Prepared release notes for tag `v0.3.3`.
- Verified and packaged `0.3.3`; the package task produced `Releases/ghaelentweaks_0.3.3.zip` with the expected packaged `modinfo.json`.
- Implemented Better Ruins blueprint learning on `master` after the user chose not to create a separate branch.
- Added `betterruins-blueprint-learning` to the mod config model, Config Lib metadata, English language text, README, and changelog.
- Added a conditional Better Ruins asset patch that appends `BetterRuinsBlueprintReading` to `betterruins:itemtypes/betterruins/schematic.json`.
- Added server-owned learned schematic persistence, client sync, and tooltip status in `src/BetterRuins/BetterRuinsBlueprintKnowledge.cs`.
- Added `CollectibleBehaviorBetterRuinsBlueprintReading` so right-clicking a Better Ruins schematic learns the corresponding canonical schematic item code without consuming the item.
- Added Harmony prefixes for `GridRecipe.Matches` and `GridRecipe.ConsumeInput` so learned exact Better Ruins schematic ingredients can be virtual when the physical schematic is absent from the grid.
- Added a `protobuf-net.dll` reference from the configured Vintage Story install because the learned schematic sync packet uses Vintage Story's protobuf network channel serialization.
- Verified modified JSON files parse with `ConvertFrom-Json` and ran `git diff --check`; only line-ending normalization warnings were reported. Did not run `dotnet build` because explicit build permission was not given.
- Fixed source errors in `BetterRuinsBlueprintRecipePatches.cs` by adding the `Vintagestory.API.Datastructures` namespace needed for `Tags.Matches(...)` and by guarding nullable schematic recipe ingredients before calling `SatisfiesAsIngredient(...)`.
- Bumped `modinfo.json` from `0.3.1` to `0.3.2`.
- Moved the Better Ruins blueprint-learning changelog notes from `Unreleased` to `0.3.2 - 2026-07-26`.
- Fixed two nullable warnings found during release verification by adding explicit held-stack and crafting-grid stack null guards in the Better Ruins blueprint code.
- Prepared the `0.3.2` release with tag `better-ruin-schematic-memorization` and added its `docs/RELEASES.md` entry.

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
