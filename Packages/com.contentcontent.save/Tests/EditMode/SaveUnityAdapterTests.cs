#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Save.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Save.Tests
{
    public class SaveUnityAdapterTests
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
        public void SaveManagerBehaviour_AndSaveableTransform_SaveAndRestoreEvenWhenDeactivated()
        {
            var mgrGo = new GameObject("SaveManager");
            _created.Add(mgrGo);
            var mgr = mgrGo.AddComponent<SaveManagerBehaviour>();

            var mgrSo = new SerializedObject(mgr);
            mgrSo.FindProperty("_useInMemoryStorage").boolValue = true;
            mgrSo.FindProperty("_defaultSlotId").stringValue = "test_slot";
            mgrSo.ApplyModifiedPropertiesWithoutUndo();

            string lastSavedSlot = null;
            string lastLoadedSlot = null;
            mgr.OnSlotSaved.AddListener(slot => lastSavedSlot = slot);
            mgr.OnSlotLoaded.AddListener(slot => lastLoadedSlot = slot);

            var crateGo = new GameObject("Chest");
            _created.Add(crateGo);
            crateGo.transform.localPosition = new Vector3(4f, 5f, -2f);
            crateGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            crateGo.SetActive(true);

            var saveable = crateGo.AddComponent<SaveableTransform>();
            var savSo = new SerializedObject(saveable);
            savSo.FindProperty("_saveId").stringValue = "chest_hub_01";
            savSo.FindProperty("_saveScale").boolValue = true;
            savSo.ApplyModifiedPropertiesWithoutUndo();
            saveable.RegisterWithManager();

            mgr.SaveDefaultSlot();
            Assert.AreEqual("test_slot", lastSavedSlot);
            Assert.IsTrue(mgr.SlotExists("test_slot"));

            // Mutate and deactivate the object before loading
            crateGo.transform.localPosition = Vector3.zero;
            crateGo.transform.localRotation = Quaternion.identity;
            crateGo.transform.localScale = Vector3.one * 3f;
            crateGo.SetActive(false);

            bool ok = mgr.LoadDefaultSlot();
            Assert.IsTrue(ok);
            Assert.AreEqual("test_slot", lastLoadedSlot);
            Assert.IsTrue(crateGo.activeSelf);
            Assert.AreEqual(4f, crateGo.transform.localPosition.x, 0.001f);
            Assert.AreEqual(5f, crateGo.transform.localPosition.y, 0.001f);
            Assert.AreEqual(-2f, crateGo.transform.localPosition.z, 0.001f);
            Assert.AreEqual(90f, crateGo.transform.localRotation.eulerAngles.y, 0.1f);
            Assert.AreEqual(1f, crateGo.transform.localScale.x, 0.001f);
        }
    }
}
#endif
