#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Interaction.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Interaction.Tests
{
    public class InteractionUnityAdapterTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

#if CONTENTCONTENT_PHYSICS2D
        [Test]
        public void InteractorBehaviour_Physics2D_FocusesInteractsAndRespectsLocks()
        {
            var doorGo = new GameObject("LockedDoor");
            _created.Add(doorGo);
            doorGo.transform.position = new Vector3(1f, 0f, 0f);
            var col = doorGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var interactable = doorGo.AddComponent<InteractableBehaviour>();

            var targetSo = new SerializedObject(interactable);
            targetSo.FindProperty("_interactableId").stringValue = "gate_switch";
            targetSo.FindProperty("_actionVerb").stringValue = "Pull";
            targetSo.FindProperty("_displayName").stringValue = "Lever";
            targetSo.FindProperty("_singleUse").boolValue = true;
            targetSo.FindProperty("_locked").boolValue = true;
            targetSo.FindProperty("_lockedReason").stringValue = "Power Off";
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            var playerGo = new GameObject("PlayerInteractor");
            _created.Add(playerGo);
            playerGo.transform.position = Vector3.zero;
            var interactor = playerGo.AddComponent<InteractorBehaviour>();
            interactor.DetectionMode = InteractionDetectionMode.Physics2D;

            string lastPrompt = null;
            string interactedId = null;
            interactor.OnPromptChanged.AddListener(p => lastPrompt = p);
            interactor.OnInteracted.AddListener(id => interactedId = id);

            Physics2D.SyncTransforms();
            interactor.RefreshFocusNow();

            Assert.IsTrue(interactor.Selector.HasFocus);
            Assert.AreEqual("Pull Lever (Power Off)", lastPrompt);

            InteractionResult blocked = interactor.TryInteractNow();
            Assert.IsFalse(blocked.Succeeded);
            Assert.IsNull(interactedId);

            interactable.Unlock();
            interactor.RefreshFocusNow();
            Assert.AreEqual("Pull Lever", lastPrompt);

            InteractionResult ok = interactor.TryInteractNow();
            Assert.IsTrue(ok.Succeeded);
            Assert.AreEqual("gate_switch", interactedId);
            Assert.IsTrue(interactable.HasUsed);

            // Single-use prevents second activation until reset
            InteractionResult afterSingleUse = interactor.TryInteractNow();
            Assert.IsFalse(afterSingleUse.Succeeded);

            interactable.ResetInteractable();
            Assert.IsTrue(interactor.TryInteractNow().Succeeded);
        }
#endif
    }
}
#endif
