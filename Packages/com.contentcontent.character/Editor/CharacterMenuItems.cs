using System;
using ContentContent.Character.Unity;
using UnityEditor;
using UnityEngine;
using UnityCharacterController = UnityEngine.CharacterController;

namespace ContentContent.Character.Editor
{
    internal static class CharacterMenuItems
    {
        private const string Input2DAssemblyQualifiedName = "ContentContent.Character.Unity.Input.CharacterInput2D, ContentContent.Character.Input";
        private const string Input3DAssemblyQualifiedName = "ContentContent.Character.Unity.Input.CharacterInput3D, ContentContent.Character.Input";

#if CONTENTCONTENT_PHYSICS2D
        [MenuItem("GameObject/ContentContent/Character/2D Side-Scroller Character", false, 10)]
        private static void CreateSideScroller2D(MenuCommand menuCommand)
        {
            Create2DCharacter("SideScrollerCharacter2D", CharacterPresetType.SideScroller, new Vector2(0.8f, 1.6f), menuCommand);
        }

        [MenuItem("GameObject/ContentContent/Character/2D Top-Down Character", false, 11)]
        private static void CreateTopDown2D(MenuCommand menuCommand)
        {
            Create2DCharacter("TopDownCharacter2D", CharacterPresetType.TopDown, new Vector2(0.9f, 0.9f), menuCommand);
        }

        private static void Create2DCharacter(string name, CharacterPresetType preset, Vector2 colliderSize, MenuCommand menuCommand)
        {
            var go = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(colliderSize.x, colliderSize.y * 0.5f, 0.6f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var motor = go.AddComponent<CharacterMotor2D>();
            motor.ApplyPreset(preset);
            motor.AutoFitSensorsFromCollider();

            Type inputType = CharacterConfigInspectorUI.ResolveOptionalType(Input2DAssemblyQualifiedName);
            if (inputType != null)
            {
                go.AddComponent(inputType);
            }

            go.AddComponent<CharacterAnimatorBridge>();

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeGameObject = go;
        }
#endif

#if CONTENTCONTENT_PHYSICS
        [MenuItem("GameObject/ContentContent/Character/3D Third-Person Character", false, 12)]
        private static void CreateThirdPerson3D(MenuCommand menuCommand)
        {
            var go = new GameObject("ThirdPersonCharacter3D");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

            var uc = go.AddComponent<UnityCharacterController>();
            uc.height = 2f;
            uc.radius = 0.5f;

            var motor = go.AddComponent<CharacterMotor3D>();
            motor.ApplyPreset(CharacterPresetType.ThirdPerson);
            motor.AutoFitSensorsFromController();

            Type inputType = CharacterConfigInspectorUI.ResolveOptionalType(Input3DAssemblyQualifiedName);
            if (inputType != null)
            {
                go.AddComponent(inputType);
            }

            go.AddComponent<CharacterAnimatorBridge>();

            Undo.RegisterCreatedObjectUndo(go, "Create ThirdPersonCharacter3D");
            Selection.activeGameObject = go;
        }
#endif
    }
}
