using System.Collections.Generic;
using ContentContent.Core.Events.Unity;
using UnityEngine;
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.Camera.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter owning a pure-C# <see cref="CameraDirector"/> and updating a
    /// world-space follow anchor transform in <c>LateUpdate</c> for Cinemachine 3 or custom camera rigs.
    /// </summary>
    public class CameraDirectorBehaviour : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraFramingConfigAsset _configAsset;
        [SerializeField] private CameraFramingConfig _inlineConfig = new CameraFramingConfig();
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;

        [Header("Targets")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Transform _followAnchor;

        private readonly Dictionary<string, Object> _boundsRegistry = new Dictionary<string, Object>();
        private CameraDirector _director;

        private Vector3 _facingDirection = Vector3.right;
        private float _verticalLookInput;
        private bool _isGrounded = true;
        private bool _isStationary = true;

        /// <summary>Lazily initialized pure-C# <see cref="CameraDirector"/>.</summary>
        public CameraDirector Director
        {
            get
            {
                if (_director == null)
                {
                    CameraFramingConfig config = _configAsset != null
                        ? _configAsset.CreateRuntimeCopy()
                        : _inlineConfig.Clone();

                    _director = new CameraDirector(
                        config,
                        _eventBusBehaviour != null ? _eventBusBehaviour.Bus : null);
                }

                return _director;
            }
        }

        /// <summary>Latest framing output produced by <see cref="CameraDirector.Tick"/>.</summary>
        public CameraFrameOutput CurrentOutput => Director.CurrentOutput;

        /// <summary>World-space transform followed by the camera.</summary>
        public Transform FollowTarget
        {
            get => _followTarget;
            set => _followTarget = value;
        }

        /// <summary>Optional smoothed framing anchor transform positioned at <see cref="CameraFrameOutput.FollowAnchorPosition"/>.</summary>
        public Transform FollowAnchor
        {
            get => _followAnchor;
            set => _followAnchor = value;
        }

        private void Awake()
        {
            _ = Director;
        }

        private void LateUpdate()
        {
            Vector3 targetPos = _followTarget != null ? _followTarget.position : transform.position;

            var input = new CameraFrameInput
            {
                TargetPosition = new NumVector3(targetPos.x, targetPos.y, targetPos.z),
                FacingDirection = new NumVector3(_facingDirection.x, _facingDirection.y, _facingDirection.z),
                VerticalLookInput = _verticalLookInput,
                IsTargetGrounded = _isGrounded,
                IsTargetStationary = _isStationary
            };

            CameraFrameOutput output = Director.Tick(Time.deltaTime, in input);

            if (_followAnchor != null)
            {
                _followAnchor.position = new Vector3(
                    output.FollowAnchorPosition.X,
                    output.FollowAnchorPosition.Y,
                    output.FollowAnchorPosition.Z);
            }
        }

        /// <summary>Updates per-frame target locomotion hints used for look-ahead and vertical peek.</summary>
        public void SetTargetState(Vector3 facingDirection, float verticalLookInput, bool isGrounded, bool isStationary)
        {
            if (facingDirection.sqrMagnitude > 0.0001f)
            {
                _facingDirection = facingDirection.normalized;
            }

            _verticalLookInput = verticalLookInput;
            _isGrounded = isGrounded;
            _isStationary = isStationary;
        }

        /// <summary>Pushes or updates a camera zone on the underlying <see cref="CameraZoneStack"/>.</summary>
        public void EnterZone(CameraZoneConfig zone) => Director.Zones.EnterZone(zone);

        /// <summary>Removes a camera zone by ID from the underlying <see cref="CameraZoneStack"/>.</summary>
        public bool ExitZone(string zoneId) => Director.Zones.ExitZone(zoneId);

        /// <summary>Requests an immediate camera snap/cut on the next update.</summary>
        public void RequestSnap(Vector3 worldPosition, string reason = null)
        {
            Director.RequestSnap(new NumVector3(worldPosition.x, worldPosition.y, worldPosition.z), reason);
        }

        /// <summary>Requests a camera impulse (forwarded to Cinemachine Impulse Source when bridged).</summary>
        public void RequestImpulse(Vector3 velocity, float strength = 1f, string sourceId = null)
        {
            Director.RequestImpulse(new NumVector3(velocity.x, velocity.y, velocity.z), strength, sourceId);
        }

        /// <summary>Registers a named bounds asset/collider (e.g., <c>Collider2D</c>) for confiner switching.</summary>
        public void RegisterBounds(string boundsId, Object boundsObject)
        {
            if (!string.IsNullOrEmpty(boundsId) && boundsObject != null)
            {
                _boundsRegistry[boundsId] = boundsObject;
            }
        }

        /// <summary>Unregisters a named bounds entry.</summary>
        public void UnregisterBounds(string boundsId)
        {
            if (!string.IsNullOrEmpty(boundsId))
            {
                _boundsRegistry.Remove(boundsId);
            }
        }

        /// <summary>Resolves a typed bounds object previously registered under <paramref name="boundsId"/>.</summary>
        public bool TryGetBounds<T>(string boundsId, out T bounds) where T : Object
        {
            if (!string.IsNullOrEmpty(boundsId) &&
                _boundsRegistry.TryGetValue(boundsId, out Object raw) &&
                raw is T typed)
            {
                bounds = typed;
                return true;
            }

            bounds = null;
            return false;
        }
    }
}
