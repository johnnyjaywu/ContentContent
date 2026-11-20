#if CONTENTCONTENT_CINEMACHINE
using Unity.Cinemachine;
using UnityEngine;

namespace ContentContent.Camera.Unity.Cinemachine
{
    /// <summary>
    /// Connects a <see cref="CameraDirectorBehaviour"/> to Cinemachine 3
    /// (<see cref="CinemachineCamera"/>, <see cref="CinemachinePositionComposer"/>,
    /// optional <see cref="CinemachineConfiner2D"/>, and optional <see cref="CinemachineImpulseSource"/>).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class CinemachineCameraBridge : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private CameraDirectorBehaviour _director;
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachinePositionComposer _positionComposer;

        [Header("Optional Cinemachine Modules")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;
        [SerializeField] private bool _driveFollowAnchorInsteadOfTargetOffset = false;

#if CONTENTCONTENT_PHYSICS2D
        [SerializeField] private CinemachineConfiner2D _confiner2D;
#endif

        private string _lastAppliedBoundsId = string.Empty;

        private void Awake()
        {
            if (_director == null)
                _director = GetComponent<CameraDirectorBehaviour>();

            if (_cinemachineCamera == null)
                _cinemachineCamera = GetComponent<CinemachineCamera>();

            if (_positionComposer == null && _cinemachineCamera != null)
                _positionComposer = _cinemachineCamera.GetCinemachineComponent<CinemachinePositionComposer>();

#if CONTENTCONTENT_PHYSICS2D
            if (_confiner2D == null && _cinemachineCamera != null)
                _confiner2D = _cinemachineCamera.GetComponent<CinemachineConfiner2D>();
#endif
        }

        private void OnEnable()
        {
            if (_director != null)
            {
                _director.Director.OnImpulseRequested += HandleImpulseRequested;
            }
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.Director.OnImpulseRequested -= HandleImpulseRequested;
            }
        }

        private void LateUpdate()
        {
            if (_director == null || _cinemachineCamera == null)
                return;

            CameraFrameOutput output = _director.CurrentOutput;

            LensSettings lens = _cinemachineCamera.Lens;
            lens.OrthographicSize = output.OrthographicSize;
            _cinemachineCamera.Lens = lens;

            if (_driveFollowAnchorInsteadOfTargetOffset || output.IsPositionLocked)
            {
                if (_director.FollowAnchor != null)
                {
                    _cinemachineCamera.Follow = _director.FollowAnchor;
                }

                if (_positionComposer != null)
                {
                    _positionComposer.TargetOffset = Vector3.zero;
                }
            }
            else
            {
                if (_director.FollowTarget != null)
                {
                    _cinemachineCamera.Follow = _director.FollowTarget;
                }

                if (_positionComposer != null)
                {
                    _positionComposer.TargetOffset = new Vector3(
                        output.FramingOffset.X,
                        output.FramingOffset.Y,
                        output.FramingOffset.Z);
                }
            }

#if CONTENTCONTENT_PHYSICS2D
            if (_confiner2D != null && !string.Equals(_lastAppliedBoundsId, output.ActiveBoundsId, System.StringComparison.Ordinal))
            {
                _lastAppliedBoundsId = output.ActiveBoundsId ?? string.Empty;
                if (_director.TryGetBounds(_lastAppliedBoundsId, out Collider2D bounds))
                {
                    _confiner2D.BoundingShape2D = bounds;
                    _confiner2D.InvalidateBoundingShapeCache();
                }
                else
                {
                    _confiner2D.BoundingShape2D = null;
                    _confiner2D.InvalidateBoundingShapeCache();
                }
            }
#endif

            if (output.SnapRequested)
            {
                Vector3 snapPos = new Vector3(
                    output.FollowAnchorPosition.X,
                    output.FollowAnchorPosition.Y,
                    _cinemachineCamera.transform.position.z);
                _cinemachineCamera.ForceCameraPosition(snapPos, _cinemachineCamera.transform.rotation);
            }
        }

        private void HandleImpulseRequested(CameraImpulseRequestedEvent evt)
        {
            if (_impulseSource == null)
                return;

            Vector3 velocity = new Vector3(evt.Velocity.X, evt.Velocity.Y, evt.Velocity.Z) * evt.Strength;
            _impulseSource.GenerateImpulseWithVelocity(velocity);
        }
    }
}
#endif
