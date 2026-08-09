# Persistent Project Memory Standards

Standards version: 0.2.0

These standards apply to projects expected to continue across substantial tasks, sessions, or coding agents.  Store the
project's durable memory at `docs/PROJECT_MEMORY.md` under the repository root.

---

## Purpose and Applicability

Project memory is a concise engineering notebook that lets a future agent resume work without earlier conversations.
Record only durable context worth preserving, such as established discoveries, design decisions, unresolved risks,
meaningful failed approaches, verification knowledge, and unfinished work.

Do not use memory as a task list, changelog, conversation summary, README replacement, source-comment substitute, or
command dump.  Routine, mechanical, or documentation-only work needs no memory update unless it produces durable
knowledge.

The repository's current code, configuration, tests, and Git state are authoritative.  Investigate and correct any
conflicting memory.  Do not assume access to previous conversations, sibling repositories, or external resources unless
the project documents them.

---

## Workflow

Before substantial work:

1. Read `docs/PROJECT_MEMORY.md` completely when it exists.
2. Inspect the repository and Git state, then reconcile stale or unsupported memory claims.
3. Create missing memory only when the task authorizes project changes and durable memory is warranted.

Before completion, when the work produced durable context:

1. Reread the latest memory so concurrent additions are preserved.
2. Update current state, active work, decisions, risks, verification knowledge, and useful failed approaches.
3. Add a dated session entry only when it provides resumption context not already clear from the repository.
4. Remove obsolete, duplicated, transient, sensitive, or unsupported content.
5. Commit the memory update with the related work.  Use a standalone commit only for memory-specific maintenance.

Read-only tasks may consult memory but must not modify it without authorization.

---

## Content and Accuracy

Record facts and decisions at the level needed to prevent rediscovery.  Include concise evidence, relevant paths,
important behavior or constraints, and conditions that would justify revisiting a decision.  Distinguish verified facts
from hypotheses, confirmed bugs from suspected risks, and completed work from intended work.

Record a failed approach only when a future agent could plausibly repeat it.  State what was attempted, the observed
result, why it was abandoned, and when it might become viable.

Canonical documentation remains authoritative for setup, APIs, architecture, and user-facing behavior.  Link instead
of duplicating it, and promote mature knowledge into the appropriate canonical document.

---

## Size, Security, and Concurrency

Target no more than 2,000 words and ten recent session entries.  Remove obsolete material; archive older information at
`docs/project-memory/archive/YYYY.md` only when it remains useful.

Never record secrets or unnecessary sensitive or personal data.  Before writing, reread the file and check Git so
concurrent additions are preserved and conflicts are resolved deliberately.

---

## Required Structure

Every memory file must contain its title, last-updated date, and `Overview and current state`.  Add optional sections
only when useful: `Active work`, `Durable technical knowledge`, `Architecture decisions and external contracts`,
`Known failed approaches`, `Bugs, risks, and open questions`, `Testing and useful commands`, `Important files and
paths`, and `Recent session history`.

Use this minimum file:

```markdown
# Project Memory

Last updated: YYYY-MM-DD

## Overview and current state

...
```

When a session entry is useful, use:

```markdown
### YYYY-MM-DD — Brief task name

- Outcome:
- Decisions:
- Verification:
- Next:
```
