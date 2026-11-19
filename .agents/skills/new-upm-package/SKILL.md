---
name: new-upm-package
description: >-
  Scaffolds a new ContentContent UPM package under Packages/com.contentcontent.<name>
  following this repo's strict pure-C# Runtime, Unity adapter, EditMode test, .meta,
  and Docsify conventions. Use when creating a new game system package in this repository.
---

# New UPM Package

Scaffold a new modular package under `Packages/com.contentcontent.<slug>/` with valid `.asmdef`, `.meta`, test, and Docsify files.

## Inputs to Confirm

- **Slug** (lowercase, e.g. `inventory`, `quest`, `audio`)
- **PascalCase Name** (e.g. `Inventory`, `Quest`)
- **Dependencies** (does `Runtime/` depend on `ContentContent.Core` or other local packages?)

## Steps

### 1. Create Directory Structure

```text
Packages/com.contentcontent.<slug>/
├── package.json
├── Runtime/
│   ├── ContentContent.<Name>.asmdef
│   └── Unity/
│       └── ContentContent.<Name>.Unity.asmdef
└── Tests/
    └── EditMode/
        └── ContentContent.<Name>.Tests.asmdef
```

### 2. Author `package.json`

```json
{
  "name": "com.contentcontent.<slug>",
  "version": "1.0.0",
  "displayName": "ContentContent - <Name>",
  "description": "<One-line description of the system.>",
  "unity": "2022.3",
  "keywords": [
    "<slug>",
    "contentcontent"
  ]
}
```
If it depends on `com.contentcontent.core`, add `"dependencies": { "com.contentcontent.core": "1.0.0" }`.

### 3. Author Assembly Definitions

- **`Runtime/ContentContent.<Name>.asmdef`** (MUST set `"noEngineReferences": true`):
  ```json
  {
      "name": "ContentContent.<Name>",
      "rootNamespace": "ContentContent.<Name>",
      "references": [],
      "includePlatforms": [],
      "excludePlatforms": [],
      "allowUnsafeCode": false,
      "overrideReferences": false,
      "precompiledReferences": [],
      "autoReferenced": true,
      "defineConstraints": [],
      "versionDefines": [],
      "noEngineReferences": true
  }
  ```
- **`Runtime/Unity/ContentContent.<Name>.Unity.asmdef`**:
  - `"references": ["ContentContent.<Name>"]` (plus any Unity adapter deps like `"ContentContent.Core.Unity"`).
  - `"noEngineReferences": false`.
- **`Tests/EditMode/ContentContent.<Name>.Tests.asmdef`**:
  - `"references": ["UnityEngine.TestRunner", "UnityEditor.TestRunner", "ContentContent.<Name>"]`
  - `"includePlatforms": ["Editor"]`, `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`, `"autoReferenced": false`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`.

### 4. Generate Valid `.meta` Files

Every folder and file inside `Packages/com.contentcontent.<slug>/` (excluding `*~` folders such as `Samples~`) requires a `.meta` file with a unique 32-char lowercase hex `guid`.

- **Folder `.meta`:**
  ```yaml
  fileFormatVersion: 2
  guid: <32-hex-chars>
  folderAsset: yes
  DefaultImporter:
    externalObjects: {}
    userData: 
    assetBundleName: 
    assetBundleVariant: 
  ```
- **`.cs` `.meta`:** uses `MonoImporter` (`serializedVersion: 2`, `defaultReferences: []`, `executionOrder: 0`).
- **`.asmdef` `.meta`:** uses `AssemblyDefinitionImporter`.
- **`package.json.meta`:** uses `TextScriptImporter` (copy format from `Packages/com.contentcontent.core/package.json.meta` with a fresh GUID).

### 5. Scaffold Docsify Documentation

Create:
- `docs/<slug>/how-it-works.md`
- `docs/<slug>/tutorial.md`
- `docs/<slug>/scripting-api.md`

Update:
- `docs/_sidebar.md` (add the new package section)
- `docs/README.md` (add table row + git URL)
- Root `README.md` (add table row + git URL + optional quick example)

### 6. Verify

Run the repo verification script (it auto-discovers the new package in `tools/PureTests/`):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-repo.ps1
```

Confirm zero `.meta` errors, zero `noEngineReferences` violations, and all pure C# tests pass.
