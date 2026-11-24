# Interaction Scripting API

Complete reference for `ContentContent.Interaction` (pure C# core) and `ContentContent.Interaction.Unity` (Unity adapters).

---

## Namespace: `ContentContent.Interaction`

### Structs & Interfaces (`InteractionTypes.cs`)

- **`InteractionContext`**: `InteractorPosition` (`System.Numerics.Vector3`), `ForwardDirection`, `Interactor`.
- **`InteractionPrompt`**: `InteractableId`, `ActionVerb`, `DisplayName`, `CanInteract`, `BlockedReason`, `HoldDuration`, `Distance`, `FormatDisplayText()`.
- **`InteractionResult`**: `Succeeded`, `Message`, `Success(string)`, `Blocked(string)`.
- **`IInteractable`**: `InteractableId`, `WorldPosition`, `Priority`, `MaxDistanceOverride`, `GetPrompt(in InteractionContext)`, `TryInteract(in InteractionContext)`.
- **`InteractionConfig`**: `MaxDistance`, `DistanceWeight`, `FacingWeight`, `PriorityWeight`, `RequireLineOfSight`, `IncludeBlockedInFocus`, `Sanitize()`.

### `InteractionSelector` (`sealed class`)

- Properties: `Config`, `FocusedInteractable`, `CurrentPrompt`, `HasFocus`, `IsHolding`, `HoldElapsed`, `HoldProgress`.
- Methods: `UpdateFocus(in InteractionContext, IReadOnlyList<IInteractable>, Func<Vector3, Vector3, bool> hasLineOfSight = null)`, `BeginInteract(in InteractionContext)`, `TickHold(float deltaTime, in InteractionContext)`, `CancelHold()`, `TryInteractImmediate(in InteractionContext)`, `ClearFocus()`.
- Events: `OnFocusChanged`, `OnHoldProgressChanged`, `OnInteractionCompleted`, `OnInteractionFailed`.

---

## Namespace: `ContentContent.Interaction.Unity`

- **`InteractableBehaviour`** (`MonoBehaviour`, `IInteractable`): Configure `_actionVerb`, `_displayName`, `_holdDuration`, `_priority`, `_cooldownSeconds`, `_singleUse`, `_locked`, `_lockedReason`, plus `SetLocked`, `Lock`, `Unlock`, `ResetInteractable`, `OnFocused`, `OnUnfocused`, `OnInteracted`, `OnInteractionBlocked`.
- **`InteractorBehaviour`** (`MonoBehaviour`): Non-alloc `Physics2D` / `Physics3D` candidate collector driving `InteractionSelector`, exposing `RefreshFocusNow()`, `BeginInteract()`, `CancelInteract()`, `TryInteractNow()`, `OnPromptChanged`, `OnPromptCleared`, `OnHoldProgress`, `OnInteracted`.
- **`InteractionInput`** (`ContentContent.Interaction.Unity.Input`, `CONTENTCONTENT_INPUT_SYSTEM`): Binds an optional `InputActionReference` (defaults to `<Keyboard>/e` and `<Gamepad>/buttonSouth`) to `InteractorBehaviour`.
