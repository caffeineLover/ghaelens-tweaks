# C and C++ Coding Standards

Standards version: 0.1.1

This profile applies to C and C++ source, headers, templates, and inline implementation files.

---

# 1. Type and Resource Safety

Compile with the project's highest practical warning level and run its configured static analyzer.  Treat new warnings as
change failures.  Do not suppress a diagnostic without a narrow scope and an explanation of why the code is safe.

Initialize variables before use, avoid implicit narrowing and signedness conversions, and make ownership and lifetime
explicit.  Use fixed-width integer types when an external format or binary interface requires an exact width.

In C, document ownership of allocated memory and ensure every successful acquisition has a clear cleanup path.  In C++,
prefer automatic storage, RAII, containers, and appropriate smart pointers over manual ownership; do not use C-style casts.

---

# 2. File and Interface Documentation

Every hand-written source and header file must begin with a high-level `/* ... */` block after required legal notices and
before includes or other code.  Explain the file's responsibility, subsystem, ownership model, important dependencies, and
excluded responsibilities when relevant.

Document every function at its authoritative declaration.  This includes constructors, destructors, operators, templates,
callbacks, and internal functions.  When a function has no separate declaration, place its documentation immediately before
the definition.  Do not duplicate the same contract at both declaration and definition.

Use the project's established documentation syntax, normally `///` or `/** ... */` for documented declarations and `//`
for implementation reasoning.  Apply the common two-space sentence rule to every documentation form.

Internal comments must explain meaningful phases, ownership transitions, invariants, non-obvious branches, algorithms,
fallbacks, and platform or API constraints without narrating visible syntax.

---

# 3. Headers and Interfaces

Headers must be self-contained and include what they use.  Prevent repeated inclusion with the project's chosen include
guard or `#pragma once` convention.  Keep implementation details out of public interfaces unless required by the language
or measured performance needs.

Preserve C linkage boundaries explicitly when C++ code exposes or consumes a C ABI.  Do not change a public ABI, data layout,
calling convention, or ownership contract without an explicit requirement and corresponding compatibility review.

---

# 4. Formatting and Validation

Use the repository's configured formatter and language standard.  After relevant changes, compile every affected target,
run static analysis, and execute the available tests under the supported configurations.

---

# 5. Logging

Use the project's established logging abstraction and configure its file sink beneath `logs/`.  Format native levels with
the exact labels `INFO`, `DEBG`, `WARN`, and `CRIT` required by `COMMON_STANDARDS.md`.

`printf`, `fprintf`, `std::cout`, `std::cerr`, and debugger-only output do not replace the required persistent logs.  They
remain appropriate for intentional command-line user output when that output is not an operational log record.
