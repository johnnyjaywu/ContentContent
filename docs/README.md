# ContentContent GameSystems Documentation

Welcome to the documentation for **GameSystems**, a collection of modular, reusable Unity Package Manager (UPM) packages with pure C# cores (`noEngineReferences: true`) and thin Unity adapters.

## Packages

| Package | UPM Name | Documentation |
| --- | --- | --- |
| **Dialog System** | `com.contentcontent.dialog` | [How It Works](dialog/how-it-works.md) · [Tutorial](dialog/tutorial.md) · [Scripting API](dialog/scripting-api.md) |
| **Core (Pooling, HFSM & Grid)** | `com.contentcontent.core` | See repository `README.md` |
| **Navigation** | `com.contentcontent.navigation` | See repository `README.md` |
| **Stats, Buffs & Health** | `com.contentcontent.stats` | See repository `README.md` |

## Installing a Package via UPM

In Unity, open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.dialog
```
