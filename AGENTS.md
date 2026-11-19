# GameSystems — Agent Instructions

This repository contains modular, reusable Unity game systems structured as independent **Unity Package Manager (UPM)** packages under `Packages/com.contentcontent.<name>/`.

## Environment & Compatibility

- **Unity Editor:** `7000.0.0a16` (`ProjectSettings/ProjectVersion.txt`)
- **UPM Minimum Unity:** `2022.3` (`package.json`)
- **C# Language & Runtime Target:** **C# 9.0** / **`netstandard2.1`** for pure runtime assemblies. Do not use C# 10+ syntax (e.g., file-scoped namespaces, global usings, collection expressions) or post-`netstandard2.1` BCL APIs (e.g., `ArgumentNullException.ThrowIfNull`, `PriorityQueue<T, P>`) in `Packages/`.
- **Test Framework:** NUnit **3.x** classic/constraint assertions (`Assert.AreEqual`, `Assert.That`, etc.).

## Package Architecture (Strict 3-Layer Split)

Every package under `Packages/com.contentcontent.<name>/` follows this exact layout:

1. **`Runtime/` (`ContentContent.<Name>.asmdef`, `"noEngineReferences": true`)**
   - Pure C# logic with **zero** `UnityEngine` or `UnityEditor` references.
   - Deterministic, allocation-conscious, fast to compile and unit test.
2. **`Runtime/Unity/` (`ContentContent.<Name>.Unity.asmdef`)**
   - Thin `MonoBehaviour` and `ScriptableObject` adapters bridging the pure core to Unity's lifecycle, Inspector, and Gizmos.
3. **`Tests/EditMode/` (`ContentContent.<Name>.Tests.asmdef`)**
   - NUnit EditMode unit tests. Keep pure logic tests free of `UnityEngine` references so they run via `dotnet test`.
4. **`Samples~/` (optional)**
   - Hidden from Unity's default asset import (trailing `~`); no `.meta` files inside.

Current packages:
- `com.contentcontent.core` — Pooling, HFSM, `Grid2D<T>` / `Grid3D<T>`
- `com.contentcontent.navigation` — Unified 2D (`XY`/`XZ`) & 3D navigation (`NavGrid`, `NavPathfinder`, `NavPathQueue`, `GridPathfinder`, depends on `ContentContent.Core`)
- `com.contentcontent.stats` — `Stat`, `StatCollection<T>`, `BuffController<T>`, `Health` / `IDamageable`
- `com.contentcontent.dialog` — `DialogGraph`, `DialogRunner`, `DialogScriptParser`, `DialogVariableStore`, uGUI & UI Toolkit samples

## Verification Commands

Always run verification before claiming work is complete:

- **Fast inner loop (~2s, pure C# compilation + all unit tests):**
  ```powershell
  dotnet test tools/PureTests/GameSystems.PureTests.csproj --no-restore
  ```
  *(Omit `--no-restore` on a fresh clone to restore NUnit packages once.)*
- **Full repo hygiene + pure C# tests (`.meta` pairing, `noEngineReferences` enforcement, unit tests):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts/verify-repo.ps1
  ```
- **Full Unity EditMode tests (required when modifying `Runtime/Unity/` adapters, scenes, or prefabs; requires Unity Editor closed):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts/run-unity-tests.ps1
  ```

## Unity & Git Hygiene

- **`.meta` files:** Every non-ignored file and directory inside `Packages/com.contentcontent.*` and `Assets/` must have a matching `.meta` file, and deleting/moving a file must delete/move its `.meta`. Never commit orphaned or missing `.meta` files.
- **Generated files:** Root Unity `.csproj` / `.slnx` files are gitignored (`tools/PureTests/**/*.csproj` are explicitly tracked).
- **Commits:** Use Conventional Commits scoped by package when applicable: `feat(navigation): ...`, `fix(dialog): ...`, `docs(stats): ...`, `refactor(core): ...`.
- **Documentation:** Docsify site lives in `docs/`. Each documented package has `docs/<name>/{how-it-works.md, tutorial.md, scripting-api.md}` and entries in `docs/_sidebar.md`, `docs/README.md`, and root `README.md`.

## Project Skills

Use the repo-specific skills in `.agents/skills/` for recurring workflows:
- `new-upm-package` — Scaffold a new `com.contentcontent.<name>` package and its docs.
- `verify-package` — Run pure C# tests, `.meta`/architecture checks, and Unity batchmode tests.
- `sync-docs` — Audit and update Docsify pages (`how-it-works`, `tutorial`, `scripting-api`) and `README.md` to match the public C# API.
