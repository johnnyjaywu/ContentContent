#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Character.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityCharacterController = UnityEngine.CharacterController;

namespace ContentContent.Character.Tests
{
    public class CharacterUnityAdapterTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        [Test]
        public void CharacterConfigAsset_AppliesPresetAndSanitizes()
        {
            var asset = ScriptableObject.CreateInstance<CharacterConfigAsset>();
            _createdObjects.Add(asset);

            asset.ApplyPreset(CharacterPresetType.PrecisionClimber);
            Assert.AreEqual(MovementMode.SideScroller2D, asset.Config.Mode);
            Assert.IsTrue(asset.Config.EnableWallClimb);
            Assert.IsTrue(asset.Config.AllowOmnidirectionalDash2D);

            asset.Config.MaxSpeed = -12f;
            asset.Config.JumpCutMultiplier = 5f;
            asset.Config.Sanitize();

            Assert.AreEqual(0f, asset.Config.MaxSpeed, 0.0001f);
            Assert.AreEqual(1f, asset.Config.JumpCutMultiplier, 0.0001f);
        }

        [Test]
        public void CharacterActionAsset_BuildsConfiguredSimpleCharacterAction()
        {
            var actionAsset = ScriptableObject.CreateInstance<CharacterActionAsset>();
            _createdObjects.Add(actionAsset);

            var serialized = new SerializedObject(actionAsset);
            serialized.FindProperty("_actionId").stringValue = "DashStrike";
            serialized.FindProperty("_duration").floatValue = 0.3f;
            serialized.FindProperty("_cooldown").floatValue = 0.8f;
            serialized.FindProperty("_priority").intValue = 5;
            serialized.FindProperty("_airRule").enumValueIndex = (int)ActionAirRule.GroundedOnly;
            serialized.FindProperty("_cancelVelocityOnStart").boolValue = true;
            serialized.FindProperty("_launchImpulse").vector3Value = new Vector3(8f, 2f, 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SimpleCharacterAction runtime = actionAsset.BuildRuntimeAction();
            Assert.AreEqual("DashStrike", runtime.Id);
            Assert.AreEqual(0.3f, runtime.Duration, 0.0001f);
            Assert.AreEqual(5, runtime.Priority);
            Assert.AreEqual(ActionAirRule.GroundedOnly, runtime.AirRule);
            Assert.IsTrue(runtime.CancelVelocityOnStart);
            Assert.AreEqual(8f, runtime.LaunchImpulse.X, 0.0001f);
        }

#if CONTENTCONTENT_PHYSICS2D
        [Test]
        public void CharacterMotor2D_AutoFitsSensorsToBoxColliderAndAppliesPresets()
        {
            var go = new GameObject("TestPlayer2D");
            _createdObjects.Add(go);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 2.0f);
            col.offset = new Vector2(0f, 0.5f);

            var motor = go.AddComponent<CharacterMotor2D>();
            Assert.IsTrue(motor.AutoFitSensorsFromCollider());
            motor.ApplyPreset(CharacterPresetType.ActionPlatformer);

            Assert.AreEqual(MovementMode.SideScroller2D, motor.InlineConfig.Mode);
            Assert.AreEqual(1, motor.InlineConfig.MaxAirJumps);

            var so = new SerializedObject(motor);
            Assert.AreEqual(-0.5f, so.FindProperty("_groundCheckOffset").vector2Value.y, 0.001f);
            Assert.AreEqual(1.5f, so.FindProperty("_ceilingCheckOffset").vector2Value.y, 0.001f);
            Assert.Greater(so.FindProperty("_wallCheckOffset").vector2Value.x, 0.5f);
        }

        [Test]
        public void MovingPlatform2D_ImplementsISurfaceVelocityProvider()
        {
            var go = new GameObject("TestPlatform");
            _createdObjects.Add(go);

            var platform = go.AddComponent<MovingPlatform2D>();
            Assert.IsInstanceOf<ISurfaceVelocityProvider>(platform);
            Assert.AreEqual(Vector2.zero, platform.SurfaceVelocity);
        }
#endif

#if CONTENTCONTENT_PHYSICS
        [Test]
        public void CharacterMotor3D_AutoFitsSensorsToCharacterController()
        {
            var go = new GameObject("TestPlayer3D");
            _createdObjects.Add(go);

            var uc = go.AddComponent<UnityCharacterController>();
            uc.height = 2.0f;
            uc.radius = 0.5f;
            uc.center = Vector3.zero;

            var motor = go.AddComponent<CharacterMotor3D>();
            Assert.IsTrue(motor.AutoFitSensorsFromController());

            var so = new SerializedObject(motor);
            Assert.Less(so.FindProperty("_groundCheckOffset").vector3Value.y, -0.5f);
            Assert.Greater(so.FindProperty("_ceilingCheckOffset").vector3Value.y, 0.5f);
            Assert.Greater(so.FindProperty("_groundCheckRadius").floatValue, 0.1f);
        }
#endif
    }
}
#endif
