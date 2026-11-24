using System;
using System.Numerics;

namespace ContentContent.Interaction
{
    public readonly struct InteractionContext
    {
        public Vector3 InteractorPosition { get; }
        public Vector3 ForwardDirection { get; }
        public object Interactor { get; }

        public InteractionContext(Vector3 interactorPosition, Vector3 forwardDirection, object interactor = null)
        {
            InteractorPosition = interactorPosition;
            ForwardDirection = forwardDirection.LengthSquared() > 1e-6f
                ? Vector3.Normalize(forwardDirection)
                : Vector3.UnitZ;
            Interactor = interactor;
        }
    }

    public readonly struct InteractionPrompt
    {
        public string InteractableId { get; }
        public string ActionVerb { get; }
        public string DisplayName { get; }
        public bool CanInteract { get; }
        public string BlockedReason { get; }
        public float HoldDuration { get; }
        public float Distance { get; }

        public bool IsValid => !string.IsNullOrEmpty(ActionVerb) || !string.IsNullOrEmpty(DisplayName) || !string.IsNullOrEmpty(InteractableId);

        public InteractionPrompt(
            string interactableId,
            string actionVerb,
            string displayName,
            bool canInteract = true,
            string blockedReason = "",
            float holdDuration = 0f,
            float distance = 0f)
        {
            InteractableId = interactableId ?? string.Empty;
            ActionVerb = string.IsNullOrEmpty(actionVerb) ? "Interact" : actionVerb;
            DisplayName = displayName ?? string.Empty;
            CanInteract = canInteract;
            BlockedReason = blockedReason ?? string.Empty;
            HoldDuration = Math.Max(0f, holdDuration);
            Distance = Math.Max(0f, distance);
        }

        public string FormatDisplayText()
        {
            string label = string.IsNullOrEmpty(DisplayName) ? ActionVerb : $"{ActionVerb} {DisplayName}";
            if (!CanInteract && !string.IsNullOrEmpty(BlockedReason))
                return $"{label} ({BlockedReason})";
            return label;
        }
    }

    public readonly struct InteractionResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private InteractionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public static InteractionResult Success(string message = "") => new InteractionResult(true, message);
        public static InteractionResult Blocked(string reason = "") => new InteractionResult(false, reason);
    }

    /// <summary>
    /// Implemented by any interactable object (chests, NPCs, doors, pickups, terminals).
    /// </summary>
    public interface IInteractable
    {
        string InteractableId { get; }
        Vector3 WorldPosition { get; }
        int Priority { get; }
        float MaxDistanceOverride { get; }
        InteractionPrompt GetPrompt(in InteractionContext context);
        InteractionResult TryInteract(in InteractionContext context);
    }

    /// <summary>
    /// Tuning weights and distance/sight constraints for <see cref="InteractionSelector"/>.
    /// </summary>
    public sealed class InteractionConfig
    {
        public float MaxDistance = 2.5f;
        public float DistanceWeight = 1f;
        public float FacingWeight = 0.75f;
        public float PriorityWeight = 0.5f;
        public bool RequireLineOfSight;
        public bool IncludeBlockedInFocus = true;

        public void Sanitize()
        {
            MaxDistance = Math.Max(0.1f, MaxDistance);
            DistanceWeight = Math.Max(0f, DistanceWeight);
            FacingWeight = Math.Max(0f, FacingWeight);
            PriorityWeight = Math.Max(0f, PriorityWeight);
        }
    }
}
