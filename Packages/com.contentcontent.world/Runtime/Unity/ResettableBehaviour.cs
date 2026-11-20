using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// Base <see cref="MonoBehaviour"/> implementing <see cref="IResettable"/> and auto-registering
    /// with <see cref="WorldManagerBehaviour"/> for deterministic room resets.
    /// </summary>
    public abstract class ResettableBehaviour : MonoBehaviour, IResettable
    {
        [SerializeField] private WorldManagerBehaviour _worldManager;
        [SerializeField] private string _roomId = string.Empty;

        public string RoomId => _roomId;

        protected virtual void OnEnable()
        {
            if (_worldManager == null)
                _worldManager = FindFirstObjectByType<WorldManagerBehaviour>();

            if (_worldManager != null && !string.IsNullOrEmpty(_roomId))
            {
                _worldManager.Controller.RegisterResettable(_roomId, this);
            }
        }

        protected virtual void OnDisable()
        {
            if (_worldManager != null && !string.IsNullOrEmpty(_roomId))
            {
                _worldManager.Controller.UnregisterResettable(_roomId, this);
            }
        }

        public abstract void ResetState();
    }
}
