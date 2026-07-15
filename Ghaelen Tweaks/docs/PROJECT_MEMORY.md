# Project Memory

Last updated: 2026-07-15

## Project overview

Ghaelen Tweaks is a Vintage Story mod collected under the `Ghaelen Tweaks/` project folder in the Git repository at `E:\Gaming\Vintage Story\mods\Ghaelen Tweaks`. It contains C# mod code, JSON assets, and player-facing documentation for gameplay tweaks.

## Current state

The mod project uses `Ghaelen Tweaks/Ghaelen Tweaks.csproj`. Documentation currently lives under `Ghaelen Tweaks/docs/`. Source code now lives under `Ghaelen Tweaks/src/`, with related source files grouped into feature-oriented subfolders where there is a clear functional grouping. The C# files have been updated to follow the shared comment and callable-member spacing standards. The mod also contains content patches for recipe and asset changes under `Ghaelen Tweaks/assets/`.

## Active work

No multi-step implementation is currently in progress. The most recent feature work added a server-side recipe patch that lets players craft the normal reed handbasket from thatch harvested from tule, then released it as version `0.3.1` with tag `Tule-Handbasket`.

## Durable technical knowledge

- All source files must be placed under `Ghaelen Tweaks/src/`.
- Strongly related source files should be grouped in clearly named subfolders under `src/`.
- The SDK-style project file uses default compile includes, so C# files under `src/` are compiled without explicit `Compile Include` entries.
- Existing docs in `docs/CODEX_STATE.md` say not to run builds unless explicitly instructed by the user.
- C# source files must follow the shared Vintage Story mod coding standards: file-level `/* ... */` comments, `////` comments for callable members, internal `//` intent comments, and three blank lines before method comment blocks.
- Vintage Story 1.22.3 `survival/blocktypes/plant/reedpapyrus.json` defines tule as `tallplant-tule-*`. Normal tule drops `thatch`; harvested tule drops `tuleroot`.
- Vintage Story 1.22.3 `survival/recipes/grid/basket.json` uses the 3x2 pattern `L_L	LLL` or `P_P	PPP`, with quantity 2 per occupied slot, for cattail and papyrus handbaskets.

## Architecture and design decisions

- Source layout uses `src/` as the sole source root to match shared project standards and the user's explicit instruction.
- Current functional source groupings are:
  - `src/BlockBehaviors/` for registered block behavior classes.
  - `src/EntityBehaviors/` for registered entity behavior classes.
  - `src/Configuration/` for configuration models.
  - `src/Entities/` for shared entity predicate helpers.
  - `src/Systems/` for runtime systems.
  - `src/Patches/` for Harmony patch code.

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

## Testing and verification

- Build command previously recorded in `docs/CODEX_STATE.md`: `dotnet build "Ghaelen Tweaks.sln"`.
- Build was not run during the 2026-07-15 source-layout and coding-standards sessions because explicit build permission was not given.
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
- `Ghaelen Tweaks/src/Patches/PersistentCraftingGridPatches.cs`: Harmony patches for persistent crafting grid behavior.
- `Ghaelen Tweaks/src/Systems/PalisadeDamageSystem.cs`: server-side palisade damage system.
- `Ghaelen Tweaks/assets/`: Vintage Story assets and mod data.
- `Ghaelen Tweaks/assets/survival/patches/tule-handbasket.json`: appends a handbasket recipe using `thatch` in the vanilla handbasket pattern.
- `Ghaelen Tweaks/docs/CODEX_STATE.md`: older persistent session notes that may contain useful historical context.

## Session history

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
