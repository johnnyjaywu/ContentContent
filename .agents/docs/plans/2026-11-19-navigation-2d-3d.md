# Unified 2D (XY/XZ) & Volumetric 3D Navigation Implementation Plan

> **For agentic workers:** Execute task-by-task, verifying compilation and EditMode tests via Unity batchmode.

**Goal:** Upgrade `com.contentcontent.navigation` into a drop-in 2D (`XY`/`XZ`) + volumetric 3D (`XYZ`) navigation system with zero-boilerplate Unity setup and a fast, zero-GC pure-C# core.

**Architecture:** Add `GridCoord3D` / `Grid3D<T>` / `UnityGridTransform3D` to `com.contentcontent.core`, build a compact 1-byte `NavGrid`, flat-array generation-stamped `NavPathfinder` (with LOS string-pulling and lazy connected-region checks), and `NavPathQueue` in `ContentContent.Navigation` (`noEngineReferences: true`), then expose 3 zero-wiring Unity components (`NavSurface`, `NavAgent`, `NavObstacle`) in `ContentContent.Navigation.Unity` guarded by `versionDefines` for 2D/3D physics modules.

**Tech Stack:** C# 9 (.NET Standard 2.1), Unity 7000.0.0a16 (2022.3+ compatible), NUnit EditMode tests.

**Spec:** [2026-11-19-navigation-2d-3d-design.md](../specs/2026-11-19-navigation-2d-3d-design.md)

## Global Constraints
- `ContentContent.Core.asmdef` and `ContentContent.Navigation.asmdef` MUST keep `"noEngineReferences": true`.
- Existing `Grid2D<TCell>`, `GridCoord`, and `GridPathfinder` APIs MUST remain backwards-compatible.
- `ContentContent.Navigation.Unity.asmdef` MUST use `versionDefines` for `com.unity.modules.physics` and `com.unity.modules.physics2d` so it compiles in 2D-only, 3D-only, and hybrid Unity projects.

## Review Focus
- `start == goal` on a walkable cell returns `[start]` and `true`; on a blocked cell returns `false` and empty path.
- `allowDiagonals: true, preventCornerCutting: false` MUST NOT falsely reject diagonal-only connections via cardinal `AreConnected`.
- `NavPathfinder` reused across 20+ consecutive queries (including grid resize) produces identical results with no stale stamp leakage.
- 3D diagonal corner-cutting blocks movement through diagonal edges/corners when any adjacent axial cell is blocked.
- `NavGrid.HasLineOfSight` never allows smoothed paths to clip diagonally through blocked corner cells.

---

## Task 1: Core 3D Grid (`com.contentcontent.core`)
- [ ] Create `Packages/com.contentcontent.core/Runtime/Grid/GridCoord3D.cs`
- [ ] Create `Packages/com.contentcontent.core/Runtime/Grid/IGrid3D.cs`
- [ ] Create `Packages/com.contentcontent.core/Runtime/Grid/Grid3D.cs`
- [ ] Create `Packages/com.contentcontent.core/Runtime/Unity/Grid/UnityGridTransform3D.cs`
- [ ] Create `Packages/com.contentcontent.core/Tests/EditMode/Grid3DTests.cs`

## Task 2: Pure-C# Navigation Core (`com.contentcontent.navigation/Runtime/`)
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/NavMode.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/NavGrid.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/NavPathfinder.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/NavPathQueue.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Tests/EditMode/NavGridTests.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Tests/EditMode/NavPathfinderTests.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Tests/EditMode/NavPathQueueTests.cs`

## Task 3: Drop-In Unity Adapters (`com.contentcontent.navigation/Runtime/Unity/`)
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/Unity/ContentContent.Navigation.Unity.asmdef`
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/Unity/NavSurface.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/Unity/NavAgent.cs`
- [ ] Create `Packages/com.contentcontent.navigation/Runtime/Unity/NavObstacle.cs`
- [ ] Update `Packages/com.contentcontent.navigation/package.json`

## Task 4: Verification & Documentation
- [ ] Run Unity batchmode compile & EditMode tests (verify 0 compiler errors and all tests pass)
- [ ] Run `check_unity_project.py` to verify `.meta`/GUID integrity
- [ ] Update `docs/core/`, `docs/navigation/`, and `README.md`
