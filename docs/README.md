# ContentContent GameSystems Documentation

Welcome to the documentation for **GameSystems**, a collection of modular, reusable Unity Package Manager (UPM) packages with pure C# cores (`noEngineReferences: true`) and thin Unity adapters.

## Packages

| Package | UPM Name | Documentation |
| --- | --- | --- |
| **Core (Pooling, HFSM & Grid)** | `com.contentcontent.core` | [How It Works](core/how-it-works.md) · [Tutorial](core/tutorial.md) · [Scripting API](core/scripting-api.md) |
| **Navigation** | `com.contentcontent.navigation` | [How It Works](navigation/how-it-works.md) · [Tutorial](navigation/tutorial.md) · [Scripting API](navigation/scripting-api.md) |
| **Dialog System** | `com.contentcontent.dialog` | [How It Works](dialog/how-it-works.md) · [Tutorial](dialog/tutorial.md) · [Scripting API](dialog/scripting-api.md) |
| **Stats, Buffs & Health** | `com.contentcontent.stats` | See repository `README.md` |

## Installing a Package via UPM

In Unity, open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.navigation
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.stats
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.dialog
```
