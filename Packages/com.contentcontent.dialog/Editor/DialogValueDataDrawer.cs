using ContentContent.Dialog.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Dialog.Editor
{
    /// <summary>
    /// Renders <see cref="DialogGraphAsset.ValueData"/> cleanly on one line,
    /// hiding unused type fields based on the selected <see cref="DialogValueType"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(DialogGraphAsset.ValueData))]
    internal sealed class DialogValueDataDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            SerializedProperty typeProp = property.FindPropertyRelative("type");

            float typeWidth = Mathf.Min(90f, contentRect.width * 0.34f);
            float spacing = 4f;
            var typeRect = new Rect(contentRect.x, contentRect.y, typeWidth, contentRect.height);
            var valueRect = new Rect(contentRect.x + typeWidth + spacing, contentRect.y, Mathf.Max(0f, contentRect.width - typeWidth - spacing), contentRect.height);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

            var valueType = (DialogValueType)typeProp.enumValueIndex;
            SerializedProperty valueProp;
            switch (valueType)
            {
                case DialogValueType.Bool:
                    valueProp = property.FindPropertyRelative("boolValue");
                    break;
                case DialogValueType.Int:
                    valueProp = property.FindPropertyRelative("intValue");
                    break;
                case DialogValueType.Float:
                    valueProp = property.FindPropertyRelative("floatValue");
                    break;
                default:
                    valueProp = property.FindPropertyRelative("stringValue");
                    break;
            }

            EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
