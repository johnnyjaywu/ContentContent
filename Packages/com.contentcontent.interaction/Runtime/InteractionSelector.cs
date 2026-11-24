using System;
using System.Collections.Generic;
using System.Numerics;

namespace ContentContent.Interaction
{
    /// <summary>
    /// Selects the highest-scoring <see cref="IInteractable"/> candidate by distance, facing alignment,
    /// and priority, and manages tap or hold-to-interact execution without engine allocations.
    /// </summary>
    public sealed class InteractionSelector
    {
        public InteractionConfig Config { get; }

        public IInteractable FocusedInteractable { get; private set; }
        public InteractionPrompt CurrentPrompt { get; private set; }
        public bool HasFocus => FocusedInteractable != null;
        public bool IsHolding { get; private set; }
        public float HoldElapsed { get; private set; }

        public float HoldProgress
        {
            get
            {
                if (!IsHolding || FocusedInteractable == null || CurrentPrompt.HoldDuration <= 0f)
                    return 0f;

                return Math.Min(1f, Math.Max(0f, HoldElapsed / CurrentPrompt.HoldDuration));
            }
        }

        public event Action<IInteractable, IInteractable, InteractionPrompt> OnFocusChanged;
        public event Action<float> OnHoldProgressChanged;
        public event Action<IInteractable, InteractionResult> OnInteractionCompleted;
        public event Action<IInteractable, InteractionResult> OnInteractionFailed;

        public InteractionSelector(InteractionConfig config = null)
        {
            Config = config ?? new InteractionConfig();
            Config.Sanitize();
        }

        public void UpdateFocus(
            in InteractionContext context,
            IReadOnlyList<IInteractable> candidates,
            Func<Vector3, Vector3, bool> hasLineOfSight = null)
        {
            Config.Sanitize();

            IInteractable best = null;
            InteractionPrompt bestPrompt = default;
            float bestScore = float.NegativeInfinity;

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    IInteractable candidate = candidates[i];
                    if (candidate == null)
                        continue;

                    Vector3 delta = candidate.WorldPosition - context.InteractorPosition;
                    float dist = delta.Length();
                    float maxDist = candidate.MaxDistanceOverride > 0f
                        ? candidate.MaxDistanceOverride
                        : Config.MaxDistance;

                    if (dist > maxDist)
                        continue;

                    if (Config.RequireLineOfSight && hasLineOfSight != null && !hasLineOfSight(context.InteractorPosition, candidate.WorldPosition))
                        continue;

                    InteractionPrompt rawPrompt = candidate.GetPrompt(in context);
                    if (!rawPrompt.CanInteract && !Config.IncludeBlockedInFocus)
                        continue;

                    var prompt = new InteractionPrompt(
                        rawPrompt.InteractableId,
                        rawPrompt.ActionVerb,
                        rawPrompt.DisplayName,
                        rawPrompt.CanInteract,
                        rawPrompt.BlockedReason,
                        rawPrompt.HoldDuration,
                        dist);

                    float dist01 = maxDist > 0f ? Math.Max(0f, 1f - (dist / maxDist)) : 1f;
                    float dot01 = 1f;
                    if (dist > 1e-4f)
                    {
                        Vector3 dir = delta / dist;
                        dot01 = Math.Max(0f, Vector3.Dot(context.ForwardDirection, dir));
                    }

                    float score = dist01 * Config.DistanceWeight
                                + dot01 * Config.FacingWeight
                                + candidate.Priority * Config.PriorityWeight
                                + (prompt.CanInteract ? 0.05f : 0f);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                        bestPrompt = prompt;
                    }
                }
            }

            bool focusChanged = !ReferenceEquals(FocusedInteractable, best);
            bool promptChanged = focusChanged ||
                                 CurrentPrompt.CanInteract != bestPrompt.CanInteract ||
                                 !string.Equals(CurrentPrompt.FormatDisplayText(), bestPrompt.FormatDisplayText(), StringComparison.Ordinal);

            if (focusChanged)
            {
                IInteractable previous = FocusedInteractable;
                CancelHold();
                FocusedInteractable = best;
                CurrentPrompt = bestPrompt;
                OnFocusChanged?.Invoke(previous, best, bestPrompt);
            }
            else
            {
                CurrentPrompt = bestPrompt;
                if (promptChanged)
                {
                    OnFocusChanged?.Invoke(FocusedInteractable, FocusedInteractable, bestPrompt);
                }
            }
        }

        public bool BeginInteract(in InteractionContext context)
        {
            if (FocusedInteractable == null)
                return false;

            InteractionPrompt prompt = FocusedInteractable.GetPrompt(in context);
            CurrentPrompt = prompt;

            if (!prompt.CanInteract)
            {
                OnInteractionFailed?.Invoke(FocusedInteractable, InteractionResult.Blocked(prompt.BlockedReason));
                return false;
            }

            if (prompt.HoldDuration <= 0f)
            {
                return ExecuteFocused(in context).Succeeded;
            }

            IsHolding = true;
            HoldElapsed = 0f;
            OnHoldProgressChanged?.Invoke(0f);
            return true;
        }

        public bool TickHold(float deltaTime, in InteractionContext context)
        {
            if (!IsHolding || FocusedInteractable == null)
                return false;

            InteractionPrompt prompt = FocusedInteractable.GetPrompt(in context);
            CurrentPrompt = prompt;

            if (!prompt.CanInteract)
            {
                CancelHold();
                OnInteractionFailed?.Invoke(FocusedInteractable, InteractionResult.Blocked(prompt.BlockedReason));
                return false;
            }

            float duration = Math.Max(0.0001f, prompt.HoldDuration);
            HoldElapsed += Math.Max(0f, deltaTime);
            OnHoldProgressChanged?.Invoke(Math.Min(1f, HoldElapsed / duration));

            if (HoldElapsed >= duration)
            {
                IsHolding = false;
                HoldElapsed = 0f;
                OnHoldProgressChanged?.Invoke(0f);
                return ExecuteFocused(in context).Succeeded;
            }

            return false;
        }

        public void CancelHold()
        {
            if (!IsHolding)
                return;

            IsHolding = false;
            HoldElapsed = 0f;
            OnHoldProgressChanged?.Invoke(0f);
        }

        public InteractionResult TryInteractImmediate(in InteractionContext context)
        {
            if (FocusedInteractable == null)
                return InteractionResult.Blocked("Nothing in focus.");

            InteractionPrompt prompt = FocusedInteractable.GetPrompt(in context);
            if (!prompt.CanInteract)
            {
                var blocked = InteractionResult.Blocked(prompt.BlockedReason);
                OnInteractionFailed?.Invoke(FocusedInteractable, blocked);
                return blocked;
            }

            return ExecuteFocused(in context);
        }

        public void ClearFocus()
        {
            if (FocusedInteractable == null && !IsHolding)
                return;

            CancelHold();
            IInteractable prev = FocusedInteractable;
            FocusedInteractable = null;
            CurrentPrompt = default;
            OnFocusChanged?.Invoke(prev, null, default);
        }

        private InteractionResult ExecuteFocused(in InteractionContext context)
        {
            IInteractable target = FocusedInteractable;
            InteractionResult result = target.TryInteract(in context);
            if (result.Succeeded)
            {
                OnInteractionCompleted?.Invoke(target, result);
            }
            else
            {
                OnInteractionFailed?.Invoke(target, result);
            }
            return result;
        }
    }
}
