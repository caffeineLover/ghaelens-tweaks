# Codex State Notes

Last updated: 2026-07-12.

## User Instructions

- Do not edit files, run builds, create zips, bump versions, rewrite history, or inspect additional files unless explicitly instructed.
- Never bump the mod version unless explicitly instructed.
- Never track release artifacts in Git. Release identity should come from tags, not committed zips or generated release folders.

## Current Repo State

- Documentation lives in the root `docs/` folder.
- Current Git dirty state at the time this file was updated: `Ghaelen Tweaks.sln` is modified, and root markdown documentation was moved into `docs/`.
- The `.sln` cleanup removed virtual `Solution Items` and `Releases` nodes because Rider showed unwanted refactor/status icons and stale missing zip references.
- `dotnet sln "Ghaelen Tweaks.sln" list` shows the two real projects:
  - `CakeBuild\CakeBuild.csproj`
  - `Ghaelen Tweaks\Ghaelen Tweaks.csproj`
- `dotnet build "Ghaelen Tweaks.sln"` passed with 0 errors after the solution cleanup.
- Build warnings are existing `CakeBuild` NuGet vulnerability warnings for NuGet packages.

## Release Artifact Policy

- `Releases/` and `*.zip` are ignored in `.gitignore`.
- Release zips and the expanded `Releases/ghaelentweaks` package folder were removed from Git history.
- Verification after the rewrite:
  - `git ls-files Releases "*.zip"` returned nothing.
  - `git log --all --name-only --pretty=format: -- Releases "*.zip"` returned nothing.
- The local `Releases` folder was removed from the repo working tree.
- Any future test zip should be created outside tracked source or in an ignored path, then copied to the target Vintage Story/StoryForge `Mods` folder.

## Persistent Crafting Grid Feature

- Feature is intended to remain in this branch.
- Config key: `persistent-crafting-grid`.
- Default: `true`.
- The feature uses Harmony on the client to patch `Vintagestory.Client.NoObf.GuiDialogInventory.OnGuiClosed()`.
- Patch file: `Ghaelen Tweaks/PersistentCraftingGridPatches.cs`.
- Mod system registration file: `Ghaelen Tweaks/GhaelenTweaksModSystem.cs`.
- Config file: `Ghaelen Tweaks/GhaelenTweaksConfig.cs`.
- Config Lib metadata: `Ghaelen Tweaks/assets/ghaelentweaks/config/configlib-patches.json`.
- English strings: `Ghaelen Tweaks/assets/game/lang/en.json`.
- Design intent: skip only vanilla close-time crafting-grid evacuation, preserving vanilla inventory close packets, GUI cleanup, player inventory persistence, death handling, and spoilage behavior.
- This was investigated against Vintage Story 1.22.3 reference source.

## Current Mod Metadata

- `Ghaelen Tweaks/modinfo.json` currently has version `0.3.0`.
- `Ghaelen Tweaks/modinfo.json` currently depends on `game: 1.22.3`.
- `docs/README.md` may still say the dependency uses `game: 1.22.2`; verify before publishing because the dependency was changed later to `1.22.3`.

## Palisade Damage Lifetime Fix

- `PalisadeDamageSystem` now implements `IDisposable`.
- It stores the ID returned by `api.Event.RegisterGameTickListener(...)`.
- It unregisters that listener in `Dispose()` and clears its internal caches.
- `GhaelenTweaksModSystem.Dispose()` disposes and clears `palisadeDamageSystem`.
- This was done to fix Rider's warning that `palisadeDamageSystem` was assigned but never used, while also making the server tick listener lifetime explicit.

## Packaging Done Today

- A test zip named `ghaelentweaks_0.3.0.zip` was created and copied to:
  `C:\Users\p\AppData\Roaming\StoryForge\installations\working_test_world\Mods`
- That zip was later recreated with `modinfo.json` still at version `0.3.0` and dependency changed to `game: 1.22.3`.
- Do not assume the repo still has a local copy of that zip; release artifacts were removed/ignored afterward.

## Important Caveats

- Runtime Vintage Story testing was not completed in this session.
- Recommended in-game checks for persistent crafting grid:
  - Place items in the player crafting grid, close inventory, reopen inventory.
  - Log out/restart and verify grid inputs persist.
  - Verify crafting output recalculates from persisted inputs.
  - Check death behavior under the relevant world death settings.
  - Check spoilage/transition behavior for perishable inputs.
- If Rider still shows stale UI nodes/icons after `.sln` changes, reload the solution or invalidate Rider caches.
