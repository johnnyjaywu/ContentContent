using UnityEngine;

namespace ContentContent.Camera.Unity
{
    /// <summary>
    /// Inspector-editable <see cref="ScriptableObject"/> storing a reusable <see cref="CameraFramingConfig"/> preset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraFramingConfig",
        menuName = "ContentContent/Camera/Camera Framing Config",
        order = 0)]
    public class CameraFramingConfigAsset : ScriptableObject
    {
        [SerializeField]
        private CameraFramingConfig _config = new CameraFramingConfig();

        /// <summary>Direct reference to the serialized configuration.</summary>
        public CameraFramingConfig Config => _config;

        /// <summary>Creates an independent runtime copy of this configuration.</summary>
        public CameraFramingConfig CreateRuntimeCopy()
        {
            return _config != null ? _config.Clone() : new CameraFramingConfig();
        }
    }
}
