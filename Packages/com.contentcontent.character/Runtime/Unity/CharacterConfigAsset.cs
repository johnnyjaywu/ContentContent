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
    public class CharacterConfigAsset : ScriptableObject
    {
        [SerializeField]
        private CharacterConfig _config = new CharacterConfig();

        /// <summary>Direct reference to the serialized configuration.</summary>
        public CharacterConfig Config => _config;

        /// <summary>Creates an independent runtime copy of this configuration.</summary>
        public CharacterConfig CreateRuntimeCopy()
        {
            return _config != null ? _config.Clone() : new CharacterConfig();
        }
    }
}
