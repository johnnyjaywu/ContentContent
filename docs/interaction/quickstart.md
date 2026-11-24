# Interaction Quickstart

Add context-sensitive doors, chests, levers, NPCs, and hold-to-loot prompts across 2D or 3D scenes without writing trigger detection boilerplate.

---

## 1. Try the Sample Scene First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.interaction`.
2. Open **Window → Package Manager**, select **ContentContent - Interaction**, and click **Download** under **Downloadable Samples**.
3. Click **Open Scene** (`InteractionPlayground.unity`) and press **Play**.

---

## 2. Wire Up Player + Interactables (Code-Free)

1. Select your Player and click **Add Component → ContentContent → Interaction → Interactor** (or **GameObject → ContentContent → Interaction → Interactor**).
   - Pick **Detection Mode**: `Physics2D` or `Physics3D`.
   - Add **Interaction Input** (**ContentContent → Interaction → Interaction Input**) on the same object to bind `E` / Gamepad South automatically (or assign a custom `InputActionReference`).
2. Create any scene object via **GameObject → ContentContent → Interaction → Interactable (2D)** (or **3D**).
   - Set **Action Verb** (`Open`), **Display Name** (`Chest`), and **Hold Duration** (`0` for instant tap, `> 0` for hold).
   - Wire `On Interacted` in the Inspector directly to audio, doors, dialogue (`DialogRunnerBehaviour.StartInitialDialog`), or quests (`QuestSignalEmitter.Emit`).
   - Connect `InteractorBehaviour.OnPromptChanged (string)` to your HUD prompt text!

---

## 3. Custom Code Integration

Implement `IInteractable` on any script or listen directly to `InteractorBehaviour.Selector`:

```csharp
using ContentContent.Interaction.Unity;
using UnityEngine;

public class CustomSwitch : MonoBehaviour
{
    public void BindPrompt(InteractorBehaviour interactor)
    {
        interactor.OnPromptChanged.AddListener(prompt => Debug.Log($"Show HUD: {prompt}"));
    }
}
```

---

## Next Steps

- Learn *why* single-selector focus scoring beats per-trigger `Input` listeners in [Design & Best Practices](design.md).
- Browse all C# and Unity classes in [Scripting API](scripting-api.md).
