# Interaction Design & Best Practices

A classic beginner mistake is placing `if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame) OpenChest();` inside every single chest, NPC, and door script. The first time two chests overlap or a locked door sits beside an item pickup, pressing `E` triggers **both** objects at once and scatters duplicate input listeners everywhere. `com.contentcontent.interaction` centralizes selection cleanly.

---

## 1. One Active Selector + Weighted Candidate Scoring

```text
   Nearby Colliders ──► Candidate IInteractable[]
                               │
                               ▼
                      InteractionSelector
          Score = (Distance × w1) + (Facing · Dir × w2) + (Priority × w3)
                               │
                               ▼
             Single Focused Target ──► OnPromptChanged / Hold Progress / TryInteract
```

### Why This Solves Overlap Conflicts
- **Guaranteed single activation:** Only `FocusedInteractable` can display a prompt or respond to input — no accidental double-opens when loot sits next to a ladder.
- **Locked targets still educate the player:** Keeping `IncludeBlockedInFocus = true` lets locked doors show `"Open Gate (Needs Red Key)"` instead of mysteriously hiding all UI prompts.
- **Line-of-sight optional:** Enabling `RequireLineOfSight` prevents talking through walls with an optional physics raycast check.

---

## 2. Best Practices & Common Pitfalls

- **Put Interactables on their own physics layer** so `OverlapCircle` / `OverlapSphereNonAlloc` scans only tiny sets of trigger colliders.
- **Use `InteractionInput` (not `Update` polling inside objects):** Input stays in one isolated `InputSystem` bridge so AI, tests, and cutscenes can still trigger `TryInteractNow()`.

---

## 3. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Interaction`) drops directly into Godot C# driven by an `Area2D` / `Area3D` overlap list inside an `Interactor` node.
- **Unreal:** Mirrors a trace/overlap Player Interaction `ActorComponent` scoring `UInteractableInterface` targets and feeding an Enhanced Input Hold trigger + CommonUI prompt widget.
