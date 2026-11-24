#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.World.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.World.Tests
{
    public class WorldUnityAdapterTests
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

        [Test]
        public void WorldGraphAsset_ValidateReachability_DetectsUnreachableAndSoftLocks()
        {
            var asset = ScriptableObject.CreateInstance<WorldGraphAsset>();
            _created.Add(asset);

            var so = new SerializedObject(asset);
            so.FindProperty("_startRoomId").stringValue = "Start";
            SerializedProperty rooms = so.FindProperty("_rooms");

            // Room 0: Start (hasCheckpoint) -> TrapPit (one-way)
            rooms.InsertArrayElementAtIndex(0);
            SerializedProperty r0 = rooms.GetArrayElementAtIndex(0);
            r0.FindPropertyRelative("roomId").stringValue = "Start";
            r0.FindPropertyRelative("hasCheckpoint").boolValue = true;
            SerializedProperty r0Exits = r0.FindPropertyRelative("exits");
            r0Exits.InsertArrayElementAtIndex(0);
            r0Exits.GetArrayElementAtIndex(0).FindPropertyRelative("exitId").stringValue = "ToPit";
            r0Exits.GetArrayElementAtIndex(0).FindPropertyRelative("destinationRoomId").stringValue = "TrapPit";

            // Room 1: TrapPit (no return exit -> soft-lock)
            rooms.InsertArrayElementAtIndex(1);
            SerializedProperty r1 = rooms.GetArrayElementAtIndex(1);
            r1.FindPropertyRelative("roomId").stringValue = "TrapPit";
            r1.FindPropertyRelative("hasCheckpoint").boolValue = false;

            // Room 2: IsolatedRoom (no incoming edges -> unreachable)
            rooms.InsertArrayElementAtIndex(2);
            SerializedProperty r2 = rooms.GetArrayElementAtIndex(2);
            r2.FindPropertyRelative("roomId").stringValue = "IsolatedRoom";
            so.ApplyModifiedPropertiesWithoutUndo();

            WorldValidationReport report = asset.ValidateReachability();
            Assert.IsTrue(report.HasStartRoom);
            Assert.IsFalse(report.IsValid);
            CollectionAssert.Contains(report.SoftLockRooms, "TrapPit");
            CollectionAssert.Contains(report.UnreachableRooms, "IsolatedRoom");
        }

        [Test]
        public void WorldManagerBehaviour_LoadsWorldGraphAsset_AndFiresEvents()
        {
            var asset = ScriptableObject.CreateInstance<WorldGraphAsset>();
            _created.Add(asset);
            var assetSo = new SerializedObject(asset);
            assetSo.FindProperty("_startRoomId").stringValue = "Hub";
            SerializedProperty rooms = assetSo.FindProperty("_rooms");
            rooms.InsertArrayElementAtIndex(0);
            rooms.GetArrayElementAtIndex(0).FindPropertyRelative("roomId").stringValue = "Hub";
            rooms.InsertArrayElementAtIndex(1);
            rooms.GetArrayElementAtIndex(1).FindPropertyRelative("roomId").stringValue = "Cave";
            assetSo.ApplyModifiedPropertiesWithoutUndo();

            var mgrGo = new GameObject("WorldManager");
            _created.Add(mgrGo);
            var mgr = mgrGo.AddComponent<WorldManagerBehaviour>();
            mgr.WorldGraphAsset = asset;

            string enteredRoom = null;
            string activatedCheckpoint = null;
            mgr.OnRoomEntered.AddListener(r => enteredRoom = r);
            mgr.OnCheckpointActivated.AddListener(c => activatedCheckpoint = c);

            Assert.IsTrue(mgr.Graph.TryGetRoom("Hub", out _));
            Assert.IsTrue(mgr.Graph.TryGetRoom("Cave", out _));

            mgr.TransitionToRoom("Cave");
            Assert.AreEqual("Cave", enteredRoom);

            mgr.ActivateCheckpoint("Shrine_1", "Cave", "SpawnA", Vector3.zero);
            Assert.AreEqual("Shrine_1", activatedCheckpoint);
        }

        [Test]
        public void ResettableTransform_RestoresPositionAndActiveState_EvenAfterDeactivation()
        {
            var mgrGo = new GameObject("WorldManager");
            _created.Add(mgrGo);
            var mgr = mgrGo.AddComponent<WorldManagerBehaviour>();

            var crateGo = new GameObject("Crate");
            _created.Add(crateGo);
            crateGo.transform.position = new Vector3(1f, 2f, 0f);
            var resettable = crateGo.AddComponent<ResettableTransform>();

            var so = new SerializedObject(resettable);
            so.FindProperty("_roomId").stringValue = "ZoneA";
            so.ApplyModifiedPropertiesWithoutUndo();

            resettable.CaptureSnapshot();
            resettable.RegisterWithWorld();

            crateGo.transform.position = new Vector3(99f, -10f, 0f);
            crateGo.SetActive(false);

            mgr.Controller.ResetRoom("ZoneA");

            Assert.IsTrue(crateGo.activeSelf);
            Assert.AreEqual(1f, crateGo.transform.position.x, 0.001f);
            Assert.AreEqual(2f, crateGo.transform.position.y, 0.001f);
        }
    }
}
#endif
