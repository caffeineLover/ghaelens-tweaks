# Release Standards

Standards version: 0.1.0

Apply these standards only when the user explicitly requests a release or project-specific instructions require the
release workflow.  When the user writes **"do release,"** perform the complete local release process defined below.

"Do release" authorizes updating release metadata, committing those changes, creating the requested local Git tag,
building the local artifact, and preparing release notes.  It does not authorize pushing commits or tags, publishing a
release, or uploading an artifact unless the user explicitly requests that external action.

---

## Version and Tag

Before making release changes, determine:

- The new project version
- The exact Git tag to create

If the user has not supplied either value, ask before continuing.  Do not infer a version increment or tag name.

A tag defines a release and must identify the exact commit used to build it.  The version in project metadata must agree
with the tag.  Never move, reuse, or silently replace a published release tag; a correction requires a new version and
tag.

---

## Verification

Before changing release metadata, verify that the repository has no unresolved or unrelated working-tree changes and
that the current code builds and passes its available tests.

Before building the artifact, verify that:

- Release metadata and `docs/RELEASES.md` contain the exact version and tag
- The release changes are committed
- `HEAD` is the tagged commit
- The working tree is clean
- The build and tests pass from the tagged source

Inspect or smoke-test the completed artifact when practical.  Report any check that could not be run and why.

---

## Release Procedure

1. Confirm the new version and exact tag.
2. Verify the pre-release repository state, build, and tests.
3. Update project version metadata.
4. Add the new `docs/RELEASES.md` entry.
5. Commit the release changes.
6. Create the specified tag on that commit.
7. Verify the tagged, clean repository state.
8. Build the release artifact from the tagged source.
9. Verify the artifact and prepare the corresponding local distribution and release notes.
10. Perform external publication only when explicitly authorized.

Release packages, compiled assemblies, and other generated artifacts may be attached to a hosted release or uploaded to
an authorized distribution platform, but they must not be committed to Git.

---

## Artifact Names

Use a consistent artifact filename containing the project name and exact version:

```text
ProjectName-1.2.0.zip
```

Do not use ambiguous names such as `latest.zip`, `final.zip`, or `new.zip`.

---

## Release History

Every release must add a newest-first entry to `docs/RELEASES.md`.  Each entry must:

- Use the release date in `YYYY-MM-DD` format
- Identify the version and Git tag
- Describe changes from the user's perspective rather than copy raw commit messages
- Omit change categories that have no entries
- Use only a `### Changes` subsection rather than separate Added, Changed, or Removed subsections
- Be separated from the next release by exactly three completely blank lines

Use this format:

```markdown
## Version 1.2.0

**Tag:** `v1.2.0`

**Released:** 2026-07-13

Brief description of the purpose or significance of the release.

### Changes

- Added a new feature.
- Changed an existing behavior.
- Fixed an existing problem.
- Removed obsolete content.



```
