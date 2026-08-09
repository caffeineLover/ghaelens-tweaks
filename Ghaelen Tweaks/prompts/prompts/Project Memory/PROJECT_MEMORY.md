# Persistent Project Memory Standards

Standards version: 0.1.1

These standards apply to projects expected to continue across multiple substantial tasks, sessions, or coding agents.
The project's durable memory belongs at `docs/PROJECT_MEMORY.md` under the repository root.

---

## Purpose

Project memory is a concise engineering notebook that lets a future agent resume work without access to earlier
conversations.  Preserve established discoveries, design reasoning, failed approaches worth avoiding, current risks,
verification knowledge, and unfinished work.

Project memory is not a task list, changelog, conversation summary, README replacement, substitute for source comments,
or dump of terminal commands.

The repository's current code, configuration, tests, and version-control state are authoritative.  When memory conflicts
with the repository, investigate the discrepancy and correct the memory rather than trusting it blindly.

Do not assume access to sibling repositories, previous conversations, or external resources unless the project
explicitly documents them.

---

## When It Applies

Substantial work includes feature implementation, behavioral changes, debugging investigations, architecture or
dependency changes, releases, and broad refactors.  Small mechanical or documentation-only edits need not create or
update project memory unless they produce durable knowledge.

For a read-only task, read existing project memory when it is relevant, but do not create or modify it without
authorization.  If its absence materially limits the review, report that limitation.

---

## Work Cycle

Before substantial work:

1. Look for `docs/PROJECT_MEMORY.md` under the repository root.
2. Read it completely when it exists.
3. If it is required but missing, create it only when the task authorizes project changes.
4. Inspect the repository and Git state before relying on any memory entry.
5. Reconcile stale or contradictory claims with current evidence.

Before completing substantial work:

1. Reread the latest file so concurrent additions are not overwritten.
2. Update the overview, current state, active work, risks, and open questions to match the repository.
3. Record newly established knowledge and meaningful failed approaches.
4. Add one concise, dated session entry using the format below.
5. Remove placeholders, duplication, secrets, transient details, and claims no longer supported by evidence.
6. Include the memory update in the related commit whenever the task is committed.

---

## Content Quality

Record facts and decisions at the level needed to avoid rediscovery.  Include evidence, relevant files, important API
behavior, ownership or lifecycle constraints, compatibility boundaries, and conditions that would justify revisiting a
decision.

Record a failed approach only when a future agent could plausibly repeat it.  State what was attempted, why it seemed
reasonable, the observed result, why it was abandoned, and when it might become viable.

Distinguish verified facts from hypotheses, confirmed bugs from suspected risks, and completed work from intended work.
Never preserve an entry merely because it already exists.

Canonical documentation remains authoritative for setup, APIs, architecture, and user-facing behavior.  Link to those
documents instead of duplicating them.  Promote mature knowledge into the appropriate canonical document and leave only
the resumption context or reference in project memory.

---

## Size and Archiving

Keep `docs/PROJECT_MEMORY.md` focused on current and reusable context, with a target maximum of 2,000 words.  Keep at
most the ten most recent session entries in the active file.

Move older session entries and obsolete but historically useful investigations to
`docs/project-memory/archive/YYYY.md`.  Preserve their original dates and add an archive link to the active file.
Summarize or archive superseded failed approaches rather than allowing the active file to grow indefinitely.

Git history preserves earlier versions, but it does not replace an archive when older context remains useful for future
work.

---

## Security and Concurrent Editing

Never record credentials, tokens, secrets, private keys, unnecessary personal paths, sensitive data, or unsanitized log
dumps.  Follow the repository's data-classification and confidentiality requirements.  Preserve only the minimum
evidence needed to explain a result.

Before writing, reread the file and check Git for concurrent changes.  Preserve entries added by other agents or
branches, resolve merge conflicts deliberately, and never replace the whole file with an older in-memory copy.

---

## Required Structure

Replace every placeholder when creating the project file.  Keep `Overview and current state` and `Recent session
history`; omit any other section when it has no useful content.

```markdown
# Project Memory

Last updated: YYYY-MM-DD

## Overview and current state

...

## Active work

...

## Durable technical knowledge

...

## Architecture decisions and external contracts

...

## Known failed approaches

...

## Bugs, risks, and open questions

...

## Testing and useful commands

...

## Important files and paths

...

## Recent session history

### YYYY-MM-DD — Brief task name

- Outcome:
- Decisions:
- Files affected:
- Verification:
- Unresolved:
- Recommended next action:
```
