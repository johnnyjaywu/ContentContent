# ContentContent Documentation

Welcome to the documentation for **ContentContent**, a collection of modular, reusable Unity Package Manager (UPM) packages with pure C# cores (`noEngineReferences: true`), thin Unity adapters, EditMode unit tests, and importable UPM samples.

## Packages

| Package | UPM Name | Samples | Documentation |
| --- | --- | --- | --- |
| **Core** *(Pooling, HFSM, Grid, Events, Timers, Services, RNG, Commands, Blackboard)* | `com.contentcontent.core` | Core Playground | [How It Works](core/how-it-works.md) · [Tutorial](core/tutorial.md) · [Scripting API](core/scripting-api.md) |
| **Navigation** *(2D XY/XZ & Volumetric 3D)* | `com.contentcontent.navigation` | Navigation Playground | [How It Works](navigation/how-it-works.md) · [Tutorial](navigation/tutorial.md) · [Scripting API](navigation/scripting-api.md) |
| **Stats, Buffs & Health** | `com.contentcontent.stats` | Combat Stats | [How It Works](stats/how-it-works.md) · [Tutorial](stats/tutorial.md) · [Scripting API](stats/scripting-api.md) |
| **Dialog** *(Branching Narrative & Yarn-Style Parser)* | `com.contentcontent.dialog` | uGUI & UI Toolkit | [How It Works](dialog/how-it-works.md) · [Tutorial](dialog/tutorial.md) · [Scripting API](dialog/scripting-api.md) |
| **Character** *(2D & 3D Locomotion, HFSM & Actions)* | `com.contentcontent.character` | Character Playground | [How It Works](character/how-it-works.md) · [Tutorial](character/tutorial.md) · [Scripting API](character/scripting-api.md) |
| **Camera** *(Framing Director, Zones & Cinemachine 3)* | `com.contentcontent.camera` | Camera Playground | [How It Works](camera/how-it-works.md) · [Tutorial](camera/tutorial.md) · [Scripting API](camera/scripting-api.md) |
| **World** *(Room Graph, Validator, Checkpoints & Flags)* | `com.contentcontent.world` | World Playground | [How It Works](world/how-it-works.md) · [Tutorial](world/tutorial.md) · [Scripting API](world/scripting-api.md) |

## Installing a Package via UPM

In Unity, open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.navigation
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.stats
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.dialog
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.character
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.camera
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.world
```

> **Note:** `com.contentcontent.navigation`, `com.contentcontent.character`, `com.contentcontent.camera`, and `com.contentcontent.world` depend on `com.contentcontent.core`.

## Importing Samples

Every package includes importable samples under `Samples~/`:

1. Open **Window → Package Manager** and select any **ContentContent** package.
2. Expand the **Samples** tab and click **Import**.
3. Follow the `README.md` inside the imported sample folder or the package's **Tutorial** page.
