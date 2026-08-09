# VBA Coding Standards

Standards version: 0.1.1

This profile applies to VBA standard modules, class modules, forms, and document modules.

---

# 1. Explicit Declarations and Types

Every module must contain `Option Explicit` in its declarations section before any procedure.  Do not rely only on the IDE's
Require Variable Declaration setting because it does not repair existing modules.

Explicitly declare every variable, constant, parameter, property, and function return type.  Specify `ByVal` or `ByRef` for
every parameter rather than relying on VBA's default.  Use `Variant` only when an Office API or genuinely dynamic value
requires it; document the reason and convert to a specific type as early as possible.

Preserve hidden `Attribute` metadata in exported modules.  Do not manually reorder or corrupt designer-managed form and
document metadata.

---

# 2. Module and Procedure Comments

Place the module's high-level header comment immediately after `Option Explicit` and before declarations or procedures.  Use
apostrophe (`'`) comment markers and describe the module's responsibility, host-application interactions, state, lifecycle,
and exclusions when relevant.

Every `Sub`, `Function`, `Property Get`, `Property Let`, `Property Set`, event handler, and callback must have a descriptive
apostrophe-comment block immediately before its declaration.  Use a line containing only `'` to separate paragraphs within
the block.

Use apostrophe comments inside procedures for meaningful phases and non-obvious reasoning.  Apply the common two-space
sentence rule to all VBA comments.  Do not use `Rem` when an apostrophe comment is sufficient.

---

# 3. Error Handling and Cleanup

Handle expected failures explicitly.  Error handlers must add useful operation and object context to the log, preserve or
restore application state, and either recover deliberately or propagate a clear failure result.  Do not use an empty or
unbounded `On Error Resume Next`; limit it to the smallest necessary block and inspect `Err` immediately.

---

# 4. Logging

Route logging through one project-standard VBA logging module.  It must create and write beneath `logs/` and emit only the
exact severity labels `INFO`, `DEBG`, `WARN`, and `CRIT` defined by `COMMON_STANDARDS.md`.

`Debug.Print` and message boxes do not replace persistent logging.  Message boxes remain appropriate only for intentional
user interaction.

---

# 5. Validation

Compile the VBA project after changes and run all available macros or automated checks that exercise the modified behavior.
Resolve undeclared variables and compile errors before considering the change complete.
