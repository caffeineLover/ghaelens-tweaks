# Python Coding Standards

Standards version: 0.1.1

This profile applies to Python source, stubs, scripts, and notebook code cells.

---

# 1. Type Hints and Static Checking

Configure one project-approved static type checker as required validation and enable its strictest practical project mode.
All maintained Python code must pass that check without errors.

Annotate every function and method parameter and return type, including `-> None`.  Annotate public attributes, module-level
state, empty collections, and local variables whose intended type is not reliably obvious to the checker or reader.

Prefer precise types, parameterized collections, protocols, and generics over `Any`.  Use `Any` only at an unavoidable
untyped boundary, document why it is necessary, and validate or narrow it as soon as possible.

Do not use blanket type-checker exclusions.  A targeted ignore must name the specific diagnostic when supported and include
a nearby explanation of why the checker cannot model the safe behavior.  Remove obsolete ignores during related changes.

Type hints do not replace runtime validation of files, network data, user input, or other untrusted values.

---

# 2. Documentation and Comments

Every hand-written module must begin with a module docstring after any required shebang or encoding declaration and before
imports.  The docstring must explain the module's responsibility, important relationships, lifecycle, ownership, and
deliberate exclusions when relevant.

Every named function, method, and class must have a docstring.  Small private callables may use a one-sentence docstring;
non-obvious callables must document their contract, assumptions, side effects, failures, and lifecycle context as relevant.

Use `#` comments for meaningful internal phases and non-obvious reasoning.  Do not place a prose comment before a function
as a substitute for its docstring.  Apply the common two-space sentence rule to both docstrings and `#` comments.

Use the project's configured formatter and Python spacing conventions.  Do not apply the C# three-blank-line convention to
Python.

---

# 3. Logging

Use Python's `logging` package or the project's approved compatible abstraction.  Configure a file handler under `logs/`
and format native levels with the exact labels `INFO`, `DEBG`, `WARN`, and `CRIT` required by `COMMON_STANDARDS.md`.

Do not use `print` as a substitute for runtime logging.  Command-line output intended for the user may still use the normal
output stream when it is not an operational log record.

---

# 4. Validation

Run the configured formatter, linter, strict type checker, and test suite after relevant changes.  Type-checker failures are
change failures, not advisory output.
