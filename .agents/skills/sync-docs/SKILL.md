---
name: sync-docs
description: >-
  Audits a GameSystems UPM package's public C# API and synchronizes its Docsify
  documentation (how-it-works.md, tutorial.md, scripting-api.md), docs/_sidebar.md,
  docs/README.md, and root README.md. Use after adding or changing public APIs or
  when documenting a package.
---

# Sync Package Documentation

Keep the Docsify site (`docs/`) and root `README.md` accurate against the actual C# implementation.

## Steps

### 1. Inventory the Package Public Surface

Read all public types, properties, methods, events, and Unity adapters in:
- `Packages/com.contentcontent.<slug>/Runtime/**/*.cs`
- `Packages/com.contentcontent.<slug>/Runtime/Unity/**/*.cs`
- `Packages/com.contentcontent.<slug>/package.json`

### 2. Update the Three Docsify Pages

Match the structure and tone of existing docs (e.g. `docs/core/` and `docs/navigation/`):

1. **`docs/<slug>/how-it-works.md`** — Architecture overview, pure C# vs. Unity adapter split, key data structures, lifecycle/performance notes.
2. **`docs/<slug>/tutorial.md`** — Copy-pasteable, step-by-step usage walkthrough covering both pure C# usage and Unity Inspector/MonoBehaviour setup.
3. **`docs/<slug>/scripting-api.md`** — Concise reference tables for every public class, struct, interface, enum, key method, and event, grouped by namespace.

### 3. Update Navigation & Index Files

Check and update:
- `docs/_sidebar.md` — Ensure the package and all 3 subpages are linked.
- `docs/README.md` — Ensure the package table row and UPM git URL are present and accurate.
- `README.md` — Ensure the package summary table, UPM git URL, and any quick example match current method signatures.

### 4. Verify Code Examples & Links

- Verify every type/method name referenced in markdown exists in the C# files.
- Verify every relative markdown link target exists on disk.
- Run `powershell -ExecutionPolicy Bypass -File scripts/verify-repo.ps1` if any code or `.meta` files were touched.
