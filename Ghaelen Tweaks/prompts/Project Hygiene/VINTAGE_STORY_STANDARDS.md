# Vintage Story Project Standards

Standards version: 0.2.0

This profile applies to Vintage Story mod projects.

---

## Prefer Content Mods

Prefer JSON patches, assets, and other content-mod mechanisms over a code mod when they can express the required
behavior cleanly and maintainably.  Use a code mod when the content system cannot implement the behavior reliably or
would make the solution unnecessarily obscure or fragile.

---

## Config Lib

Support Config Lib when it is available, but keep the integration optional.  A mod must load and function correctly when
Config Lib is not installed.

A missing Config Lib dependency must not prevent startup, disable unrelated features, or produce avoidable errors.
Provide a fallback such as direct configuration-file loading or sensible built-in defaults.

---

## Vintage Story Reference Project

Vintage Story mod projects may consult the shared **Vintage Story Reference** solution for decompiled game assemblies,
assets, selected mods, and decompiled code-mod sources.

The Reference project is located at `../Vintage Story Reference`, resolved from the consuming mod project's repository
root.  If that relative path does not exist, ask the user for the correct location rather than searching broadly or
guessing another path.

Use the Reference project that matches the mod's target game version.  Retrieve only the information needed for the
current task; do not broadly ingest or analyze the entire Reference from an ordinary mod project.

When practical, use the Reference project's index to locate relevant files, symbols, assets, and examples before opening
the underlying material.  Treat the Reference as a research resource, not an undocumented build or runtime dependency.

---

## Vintage Story Releases

Provide ready-to-paste changelog text for the Vintage Story Mod Database.  Derive it from the release-history entry and
normally omit its version, tag, and release-date metadata.

After producing the local release package, open File Explorer to its containing directory so the user can inspect it.
