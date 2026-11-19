---
name: verify-package
description: >-
  Verifies GameSystems UPM packages: runs pure C# unit tests via dotnet test,
  validates .meta pairing and noEngineReferences boundaries, and optionally runs
  Unity batchmode EditMode tests. Use before claiming any code change is complete.
---

# Verify Package

Run automated checks for the repository's pure C# cores, `.meta` hygiene, and Unity integration layer.

## Steps

### 1. Fast Pure-C# Test & Hygiene Check (Always Run)

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-repo.ps1
```

This checks:
- Every file/directory under `Packages/com.contentcontent.*` (outside `*~` folders) has a matching `.meta` file and no orphaned `.meta` files exist.
- Every pure `Runtime/*.asmdef` sets `"noEngineReferences": true`.
- No pure `Runtime/**/*.cs` file (outside `Runtime/Unity/`) imports `UnityEngine` or `UnityEditor`.
- All pure C# runtime code compiles cleanly against **`netstandard2.1` + C# 9.0** (`tools/PureTests/Runtime/GameSystems.PureRuntime.csproj`).
- All NUnit EditMode tests pass (`tools/PureTests/GameSystems.PureTests.csproj`).

For rapid TDD on a single test class or package:
```powershell
dotnet test tools/PureTests/GameSystems.PureTests.csproj --no-restore --filter "FullyQualifiedName~Navigation"
```

### 2. Unity Batchmode EditMode Tests (When Touching `Runtime/Unity/` or Scenes)

If changes touched `Runtime/Unity/`, `.asmdef` references, `Assets/`, or `Samples~/`, run Unity EditMode tests (requires the Unity Editor to be closed for this project):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-unity-tests.ps1
```

Optional filter:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-unity-tests.ps1 -TestFilter "ContentContent.Navigation"
```

### 3. Report Evidence

Before claiming completion, report:
- Exact commands run and pass/fail counts.
- Whether Unity batchmode tests were run (or why they were skipped, e.g., pure-C#-only change or Editor open).
