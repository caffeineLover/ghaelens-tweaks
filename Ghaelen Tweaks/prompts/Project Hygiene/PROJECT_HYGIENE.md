# Project Hygiene Standards

Standards version: 0.2.0

These standards apply to every project unless the user or a project-specific instruction explicitly overrides them.

---

## Project Structure

Preserve an existing repository's coherent, established structure unless the user requests a reorganization.  For a new
project, use these defaults:

- A root-level `docs/` directory for project documentation
- A root-level `src/` directory for primary source code

Group strongly related source files in clearly named subdirectories.  Tests, public headers, scripts, examples, and
other ecosystem-specific files may use conventional locations such as `tests/`, `include/`, or `scripts/`.

Store project documentation under `docs/` except for files conventionally kept at the root, including `README.md`,
`LICENSE`, `CONTRIBUTING.md`, agent instructions, and repository configuration.

---

## Agent Instructions

Every project must contain a root-level `AGENTS.md`.  Put project-specific instructions there.  Reference shared
standards and agent skills rather than duplicating them, and keep every reference portable or explicitly documented.

---

## Git

Every project must be managed with Git unless the user explicitly opts out.  Before initializing a repository, confirm
the intended repository boundary and ensure the project is not already inside another worktree.  If the project is not
already a repository, initialize Git before making project changes.

Every repository must contain an appropriate root-level `.gitignore`.  It should also contain:

- A root-level `.gitattributes` to standardize line endings and identify binary files
- A root-level `.editorconfig` to standardize basic formatting across editors and coding agents

Do not rewrite published history, move or reuse published tags, or force-push shared branches unless the user explicitly
instructs you to do so.

### Branches for Significant Work

Before beginning a significant feature, enhancement, broad refactor, or otherwise intrusive change, ask whether to
create a new branch.  Small fixes, documentation edits, targeted investigations, and routine maintenance may remain on
the current branch unless the user requests otherwise.

---

## Repository Reproducibility

A project must be buildable, testable, and usable from a clean clone by following its documented setup instructions.  Do
not rely on:

- Untracked files
- IDE state
- Undocumented machine-specific paths
- Undocumented files or tools outside the repository

Document required external tools and resources, including how to obtain them.  A shared resource may be used for
research or reference, but required build and runtime inputs must remain reproducibly available.

Commit dependency manifests and lock files when the ecosystem supports them.  Do not commit downloaded dependency
caches.

---

## Secrets and Local Configuration

Never commit secrets or private machine configuration, including:

- API keys, credentials, access tokens, and signing keys
- Personal paths and private environment files
- Machine-specific configuration

When a configuration file may contain credentials, secrets, personal paths, or private machine values, commit only a
sanitized template.  Form its filename by appending `.template` to the complete live filename, such as
`config.yaml.template` for `config.yaml`.

Add the exact non-template filename or path to `.gitignore`, and never commit the populated file.  The template must
preserve the expected structure and safe defaults but contain only unmistakable placeholders for sensitive values.
Document how to copy the template to the live filename.

Prefer environment variables or an approved secrets manager over plaintext configuration files for production
credentials.

---

## Generated Files and Artifacts

Do not commit disposable build output, release packages, logs, caches, temporary files, or other content that can be
recreated from tracked source.  The root `.gitignore` must exclude applicable paths such as:

```text
bin/
obj/
dist/
release/
releases/
logs/
*.zip
```

A generated file may be committed when it is an intentional, required repository input or deliverable, such as a lock
file, migration, designer-managed file, generated client consumed by downstream users, or source asset.  Document the
reason when the exception is not established repository convention.

---

## User-Maintained Documents

Projects may contain user-maintained Microsoft Word documents (`.docx`) under `docs/`.  A coding agent must not modify,
regenerate, reformat, rename, move, or delete them without explicit instructions.

An agent may read a `.docx` file when necessary for a task.  Permission to read it does not imply permission to modify
it.

---

## Third-Party Material

Do not casually copy third-party source code, libraries, binaries, assets, or decompiled files into a repository.  Add
such material only when genuinely required, and document its origin, version, license, and purpose.
