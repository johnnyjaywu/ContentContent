# ContentContent Documentation

Welcome to the documentation for **ContentContent** — modular, open-source game systems built so common workflows stay code-free while full C# control and architectural best practices remain visible underneath.

> **New here?** Read **[Philosophy & Architecture](architecture.md)** first to learn how every package splits pure simulation rules, code-free Unity components, Editor tooling, and tests — and how those patterns transfer to Godot and Unreal.

## Packages

| Package | UPM Name | Samples | Documentation |
| --- | --- | --- | --- |
| **Core** *(HFSM, 2D/3D Grid, EventBus, Timers, RNG, Commands, Blackboard)* | `com.contentcontent.core` | Core Playground | [Quickstart](core/quickstart.md) · [Design & Best Practices](core/design.md) · [Scripting API](core/scripting-api.md) |
| **Character** *(2D & 3D Locomotion, Feel Presets, Input & Actions)* | `com.contentcontent.character` | Character Playground | [Quickstart](character/quickstart.md) · [Design & Best Practices](character/design.md) · [Scripting API](character/scripting-api.md) |
| **Navigation** *(2D XY/XZ, Volumetric 3D, Patrol & Follow)* | `com.contentcontent.navigation` | Navigation Playground | [Quickstart](navigation/quickstart.md) · [Design & Best Practices](navigation/design.md) · [Scripting API](navigation/scripting-api.md) |
| **Stats, Buffs & Health** *(Modifiers, Buffs, Damage & Health)* | `com.contentcontent.stats` | Combat Stats | [Quickstart](stats/quickstart.md) · [Design & Best Practices](stats/design.md) · [Scripting API](stats/scripting-api.md) |
| **Dialog** *(Branching Narrative, `.txt` Parser & Command Hooks)* | `com.contentcontent.dialog` | uGUI & UI Toolkit | [Quickstart](dialog/quickstart.md) · [Design & Best Practices](dialog/design.md) · [Scripting API](dialog/scripting-api.md) |
| **World** *(Room Graph, Soft-Lock Validator, Checkpoints & Resets)* | `com.contentcontent.world` | World Playground | [Quickstart](world/quickstart.md) · [Design & Best Practices](world/design.md) · [Scripting API](world/scripting-api.md) |
| **Save** *(Atomic Slots, Backups & Schema Migrations)* | `com.contentcontent.save` | Save Playground | [Quickstart](save/quickstart.md) · [Design & Best Practices](save/design.md) · [Scripting API](save/scripting-api.md) |
| **Interaction** *(2D/3D Focus Selector, Hold Prompts & Input Bridge)* | `com.contentcontent.interaction` | Interaction Playground | [Quickstart](interaction/quickstart.md) · [Design & Best Practices](interaction/design.md) · [Scripting API](interaction/scripting-api.md) |

## Installing a Package via UPM

In Unity, open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.character
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.navigation
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.stats
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.dialog
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.world
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.save
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.interaction
```

> **Note:** `com.contentcontent.character`, `com.contentcontent.navigation`, and `com.contentcontent.world` depend on `com.contentcontent.core`.

## Downloading Samples

Samples live outside the UPM packages in top-level `Samples/` and are downloaded on demand inside Unity:

1. Ensure **`com.contentcontent.core`** is installed in your Unity project.
2. Open **Window → Package Manager** and select any **ContentContent** package.
3. In the **Downloadable Samples** panel injected into the package details view, click **Download** to install the sample into `Assets/Samples/ContentContent/`, then click **Open Scene** and press **Play**.
4. See each sample's `README.md` or the package's **Quickstart** page for controls and details.
