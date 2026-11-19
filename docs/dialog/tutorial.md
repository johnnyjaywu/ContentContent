# Dialog System Tutorial

This tutorial walks through installing `com.contentcontent.dialog`, authoring a branching conversation with variables and commands, wiring both **uGUI** and **UI Toolkit** views, and validating your graph with unit tests.

---

## Step 1: Install the Package

Open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.dialog
```

If you are working inside the `GameSystems` repository directly, the package is already present at `Packages/com.contentcontent.dialog`.

---

## Step 2: Try the Built-In Samples (uGUI & UI Toolkit)

In the `GameSystems` project, you can generate and play both sample scenes immediately from the Unity top menu:

1. **uGUI Sample**: Click **ContentContent → Dialog → Create uGUI Sample Scene** to generate `Assets/Samples/Dialog/UGUI/UGUIDialogSample.unity`, then press **Play**.
2. **UI Toolkit Sample**: Click **ContentContent → Dialog → Create UI Toolkit Sample Scene** to generate `Assets/Samples/Dialog/UIToolkit/UIToolkitDialogSample.unity`, then press **Play**.

If you installed `com.contentcontent.dialog` into another project via UPM, open **Window → Package Manager**, select **ContentContent - Dialog System**, expand **Samples**, and click **Import** on **uGUI Dialog Sample** and/or **UI Toolkit Dialog Sample**.

---

## Step 3: Build a Branching Dialog Graph

You can author dialogs in three ways: **Yarn-style `.txt` (`TextAsset`)**, **Inspector (`DialogGraphAsset`)**, or **C# code (`DialogGraph`)**.

### Option A: Yarn-Style TextAsset (`DialogScriptParser`)

Create a `.txt` file anywhere in `Assets/` (see `Assets/Samples/Dialog/Shared/CityGateDialog.txt`):

```text
<<declare $gold = 60>>
<<declare $gate_open = false>>

title: guard_greeting
---
Gate Guard: Halt, traveler. You have {$gold} gold.
-> Offer 50 gold <<if $gold >= 50 and !$gate_open>>
    <<set $gold -= 50>>
    <<play_sfx coins_clink>>
    <<jump guard_bribed>>
-> Leave
    Gate Guard: Wise choice.
===

title: guard_bribed
---
<<set $gate_open = true>>
<<open_gate north_gate>>
Gate Guard: Fine. You have {$gold} gold left.
===
```

Assign the `.txt` file to `DialogRunnerBehaviour._initialScript` (or call `_runner.StartDialog(myTextAsset)`). You can also drag `CityGateDialog.txt` onto `UGUIDialogSampleBootstrap._dialogScript` or `UIToolkitDialogSampleBootstrap._dialogScript` in either sample scene.

### Option B: Inspector (`DialogGraphAsset`)

1. Right-click in the Project window and choose **Create → ContentContent → Dialog → Dialog Graph**.
2. Set **Start Node Id** to `guard_intro`.
3. Add a default variable: `name = "gold"`, `type = Int`, `intValue = 60`.
4. Add nodes to **Nodes**:
   - `guard_intro`: speaker `SPEAKER_GUARD`, lineKey `LINE_GUARD`, with two choices (`CHOICE_BRIBE` → `bribed` with condition `gold >= 50` and mutation `gold Subtract 50`; `CHOICE_LEAVE` → `leave`).
   - `bribed`: `isEnd = true`, `onEnterCommands` with `open_gate`.
   - `leave`: `isEnd = true`.
5. Optionally create a **Localization Table** (**Create → ContentContent → Dialog → Localization Table**) mapping each key (`SPEAKER_GUARD`, `LINE_GUARD`, etc.) to display strings.

### Option C: C# Code (`DialogGraph`)

```csharp
using System.Collections.Generic;
using ContentContent.Dialog;

public static class MerchantDialogFactory
{
    public static DialogGraph Create()
    {
        var defaults = new Dictionary<string, DialogValue>
        {
            { "gold", DialogValue.FromInt(60) },
            { "has_potion", DialogValue.FromBool(false) }
        };

        var nodes = new[]
        {
            new DialogNode(
                id: "greeting",
                speaker: "Merchant",
                lineKey: "Welcome! You have {gold} gold. Care for a healing potion?",
                choices: new[]
                {
                    new DialogChoice(
                        textKey: "Buy Potion (40 gold)",
                        targetNodeId: "bought",
                        conditions: new[]
                        {
                            new DialogCondition("gold", DialogComparisonOperator.GreaterOrEqual, DialogValue.FromInt(40))
                        },
                        mutations: new[]
                        {
                            new DialogMutation("gold", DialogMutationOperator.Subtract, DialogValue.FromInt(40)),
                            new DialogMutation("has_potion", DialogMutationOperator.Set, DialogValue.FromBool(true))
                        },
                        commands: new[]
                        {
                            new DialogCommand("play_sfx", "coins_clink")
                        }),
                    new DialogChoice(
                        textKey: "Maybe later",
                        targetNodeId: "goodbye")
                }),
            new DialogNode(
                id: "bought",
                speaker: "Merchant",
                lineKey: "Pleasure doing business! You have {gold} gold left.",
                isEnd: true),
            new DialogNode(
                id: "goodbye",
                speaker: "Merchant",
                lineKey: "Safe travels!",
                isEnd: true)
        };

        return new DialogGraph("merchant_shop", "greeting", nodes, defaults);
    }
}
```

> **Tip:** Notice that the `gold - 40` cost lives on `DialogChoice.Mutations` rather than on the `bought` node. That guarantees the player is never charged twice if a node is revisited.

---

## Step 4: Register Commands and Start the Runner

Add `DialogRunnerBehaviour` to a GameObject, attach your `IDialogView`, register command handlers, and start the graph:

```csharp
using ContentContent.Dialog;
using ContentContent.Dialog.Unity;
using UnityEngine;

[RequireComponent(typeof(DialogRunnerBehaviour))]
public class MerchantController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _viewComponent; // Implements IDialogView

    private DialogRunnerBehaviour _runner;

    private void Awake()
    {
        _runner = GetComponent<DialogRunnerBehaviour>();
        _runner.SetView(_viewComponent as IDialogView);

        _runner.RegisterCommandHandler("play_sfx", cmd =>
        {
            string clipName = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : "default";
            Debug.Log($"Playing SFX: {clipName}");
        });
    }

    private void Start()
    {
        DialogGraph graph = MerchantDialogFactory.Create();
        _runner.StartDialog(graph);
    }
}
```

---

## Step 5: Connect a UI View

Because `DialogRunner` only talks to `IDialogView`, you can swap UI frameworks without touching any dialog logic.

### Using uGUI

1. Create a `Canvas` (`Scale With Screen Size`, `1920x1080`, `matchWidthOrHeight = 0.5`) with a `GraphicRaycaster`, and ensure the scene has an `EventSystem` with `InputSystemUIInputModule`.
2. Attach `UGUIDialogView` and wire `_runner`, `_panelRoot`, `_speakerText`, `_bodyText`, `_continueButton`, `_choicesContainer`, and `_choiceButtonTemplate`.
3. Call `_runner.SetView(uguiDialogView)`.

When `ShowLine` fires, `UGUIDialogView` reveals characters via `TMP_Text.maxVisibleCharacters`. Clicking **Continue** while typing skips to the end of the line; clicking again calls `_runner.Continue()`.

### Using UI Toolkit

1. Create a `PanelSettings` asset and assign it alongside `DialogView.uxml` to a `UIDocument` component.
2. Attach `UIToolkitDialogView` to the same GameObject as `UIDocument`.
3. Call `_runner.SetView(uiToolkitDialogView)`.

`UIToolkitDialogView` queries `speakerLabel`, `bodyLabel`, `choicesContainer`, `continueButton`, and `restartButton` from `rootVisualElement`, toggles visibility via the `.is-hidden` USS class (never inline `element.style.*`), and dynamically creates `Button` elements styled with `.choice-button`.

---

## Step 6: Validate Your Graph in EditMode Tests

Always validate graphs in automated tests so broken node targets, dead ends, or unreachable branches fail immediately in CI:

```csharp
using NUnit.Framework;
using ContentContent.Dialog;

public class MerchantDialogTests
{
    [Test]
    public void MerchantGraph_HasNoValidationIssues()
    {
        DialogGraph graph = MerchantDialogFactory.Create();
        Assert.IsEmpty(graph.Validate());
    }

    [Test]
    public void BuyingPotion_DeductsGoldAndSetsFlag()
    {
        var runner = new DialogRunner();
        runner.Start(MerchantDialogFactory.Create());

        Assert.AreEqual(2, runner.CurrentChoices.Count);
        runner.SelectChoice(0); // Buy Potion

        Assert.AreEqual(20, runner.Variables.GetInt("gold"));
        Assert.IsTrue(runner.Variables.GetBool("has_potion"));
    }
}
```

Run tests in Unity via **Window → General → Test Runner → EditMode → Run All**.
