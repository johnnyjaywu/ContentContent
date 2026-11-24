using System.Numerics;
using NUnit.Framework;

namespace ContentContent.Interaction.Tests
{
    public class InteractionSystemTests
    {
        private sealed class FakeInteractable : IInteractable
        {
            public string InteractableId { get; }
            public Vector3 WorldPosition { get; }
            public int Priority { get; }
            public float MaxDistanceOverride { get; set; }
            public string Verb = "Open";
            public string DisplayName = "Chest";
            public bool CanInteract = true;
            public string BlockedReason = "Locked";
            public float HoldDuration;
            public int InteractCount { get; private set; }

            public FakeInteractable(string id, Vector3 pos, int priority = 0, float holdDuration = 0f)
            {
                InteractableId = id;
                WorldPosition = pos;
                Priority = priority;
                HoldDuration = holdDuration;
            }

            public InteractionPrompt GetPrompt(in InteractionContext context) =>
                new InteractionPrompt(InteractableId, Verb, DisplayName, CanInteract, BlockedReason, HoldDuration);

            public InteractionResult TryInteract(in InteractionContext context)
            {
                if (!CanInteract)
                    return InteractionResult.Blocked(BlockedReason);

                InteractCount++;
                return InteractionResult.Success(InteractableId);
            }
        }

        [Test]
        public void InteractionSelector_FavorsFacingAndPriorityWithinRange()
        {
            var config = new InteractionConfig
            {
                MaxDistance = 3f,
                DistanceWeight = 1f,
                FacingWeight = 2f,
                PriorityWeight = 1f
            };
            var selector = new InteractionSelector(config);

            var behind = new FakeInteractable("behind", new Vector3(0f, 0f, -1f), priority: 0);
            var inFront = new FakeInteractable("front", new Vector3(0f, 0f, 1.4f), priority: 0);
            var farOut = new FakeInteractable("far", new Vector3(0f, 0f, 5f), priority: 10);

            var ctx = new InteractionContext(Vector3.Zero, Vector3.UnitZ);
            selector.UpdateFocus(in ctx, new IInteractable[] { behind, inFront, farOut });

            Assert.IsTrue(selector.HasFocus);
            Assert.AreEqual("front", selector.FocusedInteractable.InteractableId);

            var highPriorityBehind = new FakeInteractable("vip", new Vector3(0f, 0f, -1f), priority: 5);
            selector.UpdateFocus(in ctx, new IInteractable[] { inFront, highPriorityBehind });
            Assert.AreEqual("vip", selector.FocusedInteractable.InteractableId);
        }

        [Test]
        public void InteractionSelector_HoldToInteract_CompletesAndCancelsOnFocusLoss()
        {
            var selector = new InteractionSelector();
            var chest = new FakeInteractable("chest", new Vector3(0f, 0f, 1f), holdDuration: 1.0f);
            var ctx = new InteractionContext(Vector3.Zero, Vector3.UnitZ);

            selector.UpdateFocus(in ctx, new[] { chest });
            Assert.IsTrue(selector.BeginInteract(in ctx));
            Assert.IsTrue(selector.IsHolding);

            Assert.IsFalse(selector.TickHold(0.4f, in ctx));
            Assert.AreEqual(0.4f, selector.HoldProgress, 0.001f);
            Assert.AreEqual(0, chest.InteractCount);

            // Losing focus resets hold without triggering interact
            selector.UpdateFocus(in ctx, System.Array.Empty<IInteractable>());
            Assert.IsFalse(selector.IsHolding);
            Assert.AreEqual(0f, selector.HoldProgress, 0.001f);

            // Re-focus and hold full duration
            selector.UpdateFocus(in ctx, new[] { chest });
            Assert.IsTrue(selector.BeginInteract(in ctx));
            Assert.IsTrue(selector.TickHold(1.1f, in ctx));
            Assert.IsFalse(selector.IsHolding);
            Assert.AreEqual(1, chest.InteractCount);
        }

        [Test]
        public void InteractionSelector_RespectsIncludeBlockedInFocus()
        {
            var locked = new FakeInteractable("locked_door", new Vector3(0f, 0f, 1f)) { CanInteract = false, BlockedReason = "Needs Key" };
            var ctx = new InteractionContext(Vector3.Zero, Vector3.UnitZ);

            var showBlocked = new InteractionSelector(new InteractionConfig { IncludeBlockedInFocus = true });
            showBlocked.UpdateFocus(in ctx, new[] { locked });
            Assert.IsTrue(showBlocked.HasFocus);
            Assert.IsFalse(showBlocked.BeginInteract(in ctx));
            Assert.AreEqual(0, locked.InteractCount);

            var hideBlocked = new InteractionSelector(new InteractionConfig { IncludeBlockedInFocus = false });
            hideBlocked.UpdateFocus(in ctx, new[] { locked });
            Assert.IsFalse(hideBlocked.HasFocus);
        }
    }
}
