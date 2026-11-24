using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// Base <see cref="MonoBehaviour"/> implementing <see cref="IResettable"/> and auto-registering
    /// with <see cref="WorldManagerBehaviour"/> for deterministic room resets.
    /// Remains registered while disabled so deactivated enemies or pickups restore cleanly on room reset.
    /// </summary>
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public abstract class ResettableBehaviour : MonoBehaviour, IResettable
    {
        [Tooltip("Optional WorldManagerBehaviour. Auto-discovered in the scene if omitted.")]
        [SerializeField] private WorldManagerBehaviour _worldManager;

        [Tooltip("Room/zone ID whose reset triggers ResetState() on this object.")]
        [SerializeField] private string _roomId = string.Empty;

        private WorldManagerBehaviour _registeredManager;
        private string _registeredRoomId;

        public string RoomId => _roomId;

        protected virtual void Awake()
        {
            RegisterWithWorld();
        }

        protected virtual void OnEnable()
        {
            RegisterWithWorld();
        }

        protected virtual void OnDestroy()
        {
            UnregisterFromWorld();
        }

        /// <summary>Registers this object with <see cref="WorldManagerBehaviour"/> for <see cref="RoomId"/>.</summary>
        public void RegisterWithWorld()
        {
            if (_worldManager == null)
                _worldManager = FindAnyObjectByType<WorldManagerBehaviour>();

            if (_worldManager == null || string.IsNullOrEmpty(_roomId))
                return;

            if (_registeredManager == _worldManager && string.Equals(_registeredRoomId, _roomId, System.StringComparison.Ordinal))
                return;

            UnregisterFromWorld();
            _worldManager.Controller.RegisterResettable(_roomId, this);
            _registeredManager = _worldManager;
            _registeredRoomId = _roomId;
        }

        /// <summary>Unregisters this object from any previously bound <see cref="WorldManagerBehaviour"/>.</summary>
        public void UnregisterFromWorld()
        {
            if (_registeredManager != null && !string.IsNullOrEmpty(_registeredRoomId))
            {
                _registeredManager.Controller.UnregisterResettable(_registeredRoomId, this);
            }

            _registeredManager = null;
            _registeredRoomId = null;
        }

        public abstract void ResetState();
    }
}
