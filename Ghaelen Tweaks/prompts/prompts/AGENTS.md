# Shared Standards Router

Instructions version: 0.3.0

## Shared Standards Are Read-Only

When this bundle is used from another project, treat this file and everything under its containing `prompts/` directory
as read-only.  Do not modify, rename, move, delete, reformat, or version any shared standards file.

Shared standards may be changed only when the user explicitly requests standards maintenance in the canonical standards
repository.  Project-specific rules, commands, context, and approved exceptions belong in the consuming project's root
`AGENTS.md` or enforced repository configuration.  If a project conflicts with a shared standard, document the rule
locally; never alter the shared bundle to accommodate one project.

## Standards Routing

Before creating, modifying, reviewing, reorganizing, or releasing a project, read and follow
[`PROJECT_HYGIENE.md`](./Project%20Hygiene/PROJECT_HYGIENE.md).

Before substantial work in a project expected to continue across multiple tasks, sessions, or coding agents, also read
and follow [`PROJECT_MEMORY.md`](./Project%20Memory/PROJECT_MEMORY.md).

Before writing, modifying, reviewing, or refactoring code, also read and follow
[`COMMON_STANDARDS.md`](./Coding%20Standards/COMMON_STANDARDS.md), then apply every language profile relevant to
the files or code involved:

- C# (`.cs`, `.csx`):
  [`CSHARP_STANDARDS.md`](./Coding%20Standards/CSHARP_STANDARDS.md)
- Python (`.py`, `.pyi`, `.ipynb` code):
  [`PYTHON_STANDARDS.md`](./Coding%20Standards/PYTHON_STANDARDS.md)
- VBA (`.bas`, `.cls`, `.frm`, Office modules):
  [`VBA_STANDARDS.md`](./Coding%20Standards/VBA_STANDARDS.md)
- C and C++ (`.c`, `.h`, `.cc`, `.cpp`, `.cxx`, `.hh`, `.hpp`, `.hxx`, `.inl`):
  [`C_CPP_STANDARDS.md`](./Coding%20Standards/C_CPP_STANDARDS.md)

Apply additional project profiles according to the task:

- Vintage Story mod project:
  [`VINTAGE_STORY_STANDARDS.md`](./Project%20Hygiene/VINTAGE_STORY_STANDARDS.md)
- Release task, including **"do release"**:
  [`RELEASE_STANDARDS.md`](./Project%20Hygiene/RELEASE_STANDARDS.md)
- Vintage Story release: apply both additional project profiles

For mixed-language or mixed-profile work, apply every relevant standard.  For an unlisted language, apply
`COMMON_STANDARDS.md` and the repository's established conventions.  Do not load an inapplicable profile.

Explicit user instructions, project or team policy, and enforced repository configuration take precedence over these
shared defaults.  A specific profile supplements its common standard and overrides it only where explicitly stated.

The logging requirements in `COMMON_STANDARDS.md` apply to every language and project.
