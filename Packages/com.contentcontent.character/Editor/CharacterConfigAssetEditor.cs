using ContentContent.Character.Unity;
using UnityEditor;

namespace ContentContent.Character.Editor
{
    [CustomEditor(typeof(CharacterConfigAsset))]
    internal sealed class CharacterConfigAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _configProp;

        private void OnEnable()
        {
            _configProp = serializedObject.FindProperty("_config");
        }

        public override void OnInspectorGUI()
        {
            var asset = (CharacterConfigAsset)target;

            serializedObject.Update();

            CharacterConfigInspectorUI.DrawConfig(
                _configProp,
                asset.Config,
                asset,
                preset => asset.ApplyPreset(preset),
                allow2DPresets: true,
                allow3DPreset: true);

            if (serializedObject.ApplyModifiedProperties())
            {
                asset.Config?.Sanitize();
            }
        }
    }
}
