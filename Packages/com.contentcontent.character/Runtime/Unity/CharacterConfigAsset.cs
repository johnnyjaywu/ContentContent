using UnityEngine;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Inspector-editable <see cref="ScriptableObject"/> storing a reusable <see cref="CharacterConfig"/> preset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterConfig",
        menuName = "ContentContent/Character/Character Config",
        order = 0)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    public class CharacterConfigAsset : ScriptableObject
    {
        [SerializeField]
        private CharacterConfig _config = new CharacterConfig();

        /// <summary>Direct reference to the serialized configuration.</summary>
        public CharacterConfig Config => _config;

        private void OnValidate()
        {
            if (_config == null)
            {
                _config = new CharacterConfig();
            }

            _config.Sanitize();
        }

        /// <summary>Applies a built-in feel preset to this asset.</summary>
        public void ApplyPreset(CharacterPresetType preset)
        {
            if (_config == null)
            {
                _config = new CharacterConfig();
            }

            _config.CopyFrom(CharacterConfig.CreatePreset(preset));
        }

        /// <summary>Creates an independent runtime copy of this configuration.</summary>
        public CharacterConfig CreateRuntimeCopy()
        {
            return _config != null ? _config.Clone() : new CharacterConfig();
        }
    }
}
