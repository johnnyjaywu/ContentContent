using System;
using System.Numerics;
using ContentContent.Core.Events;

namespace ContentContent.Camera
{
    /// <summary>
    /// Pure C# camera framing director computing frame-rate-independent horizontal look-ahead,
    /// grounded vertical peek, zone overrides, zoom smoothing, snap cuts, and impulse requests.
    /// Designed to drive a Cinemachine 3 camera pipeline in Unity.
    /// </summary>
    public sealed class CameraDirector
    {
        private readonly IEventBus _eventBus;

        private bool _hasInitializedFacing;
        private float _desiredLookAheadSign = 1f;
        private float _pendingLookAheadSign = 1f;
        private float _lookAheadTurnTimer;
        private float _lookAheadOffsetX;

        private int _peekDirection;
        private float _verticalPeekHoldTimer;
        private float _verticalPeekOffsetY;

        private float _currentOrthographicSize;
        private bool _snapPending;

        /// <summary>Active framing configuration.</summary>
        public CameraFramingConfig Config { get; }

        /// <summary>Priority zone stack controlling overrides and bounds.</summary>
        public CameraZoneStack Zones { get; }

        /// <summary>Currently active winning camera zone, or null if none.</summary>
        public CameraZoneConfig ActiveZone => Zones.ActiveZone;

        /// <summary>Most recent computed frame output.</summary>
        public CameraFrameOutput CurrentOutput { get; private set; }

        /// <summary>Current smoothed horizontal look-ahead offset.</summary>
        public float CurrentLookAheadOffset => _lookAheadOffsetX;

        /// <summary>Current smoothed vertical peek offset.</summary>
        public float CurrentVerticalPeekOffset => _verticalPeekOffsetY;

        /// <summary>Current smoothed orthographic camera half-height.</summary>
        public float CurrentOrthographicSize => _currentOrthographicSize;

        /// <summary>Fired when <see cref="RequestSnap"/> is invoked.</summary>
        public event Action<CameraSnapRequestedEvent> OnSnapRequested;

        /// <summary>Fired when <see cref="RequestImpulse"/> is invoked.</summary>
        public event Action<CameraImpulseRequestedEvent> OnImpulseRequested;

        /// <summary>
        /// Creates a new <see cref="CameraDirector"/>.
        /// </summary>
        public CameraDirector(CameraFramingConfig config = null, IEventBus eventBus = null)
        {
            Config = config ?? new CameraFramingConfig();
            _eventBus = eventBus;
            Zones = new CameraZoneStack(eventBus);
            _currentOrthographicSize = Math.Max(0.1f, Config.DefaultOrthographicSize);
            CurrentOutput = new CameraFrameOutput(
                Vector3.Zero,
                Vector3.Zero,
                _currentOrthographicSize,
                string.Empty,
                string.Empty,
                isPositionLocked: false,
                snapRequested: false);
        }

        /// <summary>
        /// Advances look-ahead, vertical peek, zone framing, and zoom by <paramref name="deltaTime"/> seconds.
        /// </summary>
        public CameraFrameOutput Tick(float deltaTime, in CameraFrameInput input)
        {
            if (deltaTime <= 0f)
            {
                return CurrentOutput;
            }

            CameraZoneConfig activeZone = Zones.ActiveZone;
            bool allowLookAhead = activeZone == null || activeZone.AllowLookAhead;
            bool allowPeek = activeZone == null || activeZone.AllowVerticalPeek;
            bool isLocked = activeZone != null && activeZone.LockPosition;
            Vector3 staticOffset = activeZone != null ? activeZone.StaticOffset : Vector3.Zero;

            // 1. Horizontal facing look-ahead with turn delay
            if (Math.Abs(input.FacingDirection.X) > 0.001f)
            {
                float rawSign = input.FacingDirection.X > 0f ? 1f : -1f;
                if (!_hasInitializedFacing)
                {
                    _desiredLookAheadSign = rawSign;
                    _pendingLookAheadSign = rawSign;
                    _lookAheadTurnTimer = 0f;
                    _hasInitializedFacing = true;
                }
                else if (Math.Abs(rawSign - _desiredLookAheadSign) > 0.01f)
                {
                    if (Math.Abs(rawSign - _pendingLookAheadSign) > 0.01f)
                    {
                        _pendingLookAheadSign = rawSign;
                        _lookAheadTurnTimer = 0f;
                    }

                    _lookAheadTurnTimer += deltaTime;
                    if (_lookAheadTurnTimer >= Math.Max(0f, Config.LookAheadTurnDelay))
                    {
                        _desiredLookAheadSign = rawSign;
                        _lookAheadTurnTimer = 0f;
                    }
                }
                else
                {
                    _pendingLookAheadSign = rawSign;
                    _lookAheadTurnTimer = 0f;
                }
            }

            float targetLookAheadX = allowLookAhead
                ? _desiredLookAheadSign * Config.LookAheadDistance
                : 0f;

            // 2. Vertical peek (grounded + stationary + hold delay)
            float targetPeekY = 0f;
            if (allowPeek &&
                input.IsTargetGrounded &&
                input.IsTargetStationary &&
                Math.Abs(input.VerticalLookInput) >= 0.5f)
            {
                int dir = input.VerticalLookInput > 0f ? 1 : -1;
                if (dir != _peekDirection)
                {
                    _peekDirection = dir;
                    _verticalPeekHoldTimer = 0f;
                }

                _verticalPeekHoldTimer += deltaTime;
                if (_verticalPeekHoldTimer >= Math.Max(0f, Config.VerticalPeekHoldDelay))
                {
                    targetPeekY = dir > 0
                        ? Math.Abs(Config.VerticalPeekUpDistance)
                        : -Math.Abs(Config.VerticalPeekDownDistance);
                }
            }
            else
            {
                _peekDirection = 0;
                _verticalPeekHoldTimer = 0f;
            }

            // 3. Target orthographic size
            float targetOrthoSize = activeZone != null && activeZone.OrthographicSizeOverride > 0f
                ? activeZone.OrthographicSizeOverride
                : Math.Max(0.1f, Config.DefaultOrthographicSize);

            // 4. Frame-rate-independent exponential smoothing or instant snap
            bool snapThisTick = _snapPending;
            _snapPending = false;

            if (snapThisTick)
            {
                _lookAheadOffsetX = targetLookAheadX;
                _verticalPeekOffsetY = targetPeekY;
                _currentOrthographicSize = targetOrthoSize;
            }
            else
            {
                _lookAheadOffsetX = ExpSmooth(_lookAheadOffsetX, targetLookAheadX, Config.LookAheadSmoothSpeed, deltaTime);
                _verticalPeekOffsetY = ExpSmooth(_verticalPeekOffsetY, targetPeekY, Config.VerticalPeekSmoothSpeed, deltaTime);
                _currentOrthographicSize = ExpSmooth(_currentOrthographicSize, targetOrthoSize, Config.ZoomSmoothSpeed, deltaTime);
            }

            Vector3 framingOffset = new Vector3(_lookAheadOffsetX, _verticalPeekOffsetY, 0f) + staticOffset;
            Vector3 anchorPosition = isLocked
                ? activeZone.LockedPosition + staticOffset
                : input.TargetPosition + framingOffset;

            CurrentOutput = new CameraFrameOutput(
                anchorPosition,
                framingOffset,
                _currentOrthographicSize,
                activeZone != null ? activeZone.ZoneId : string.Empty,
                activeZone != null ? activeZone.BoundsId ?? string.Empty : string.Empty,
                isLocked,
                snapThisTick);

            return CurrentOutput;
        }

        /// <summary>
        /// Requests an immediate camera cut on the next <see cref="Tick"/> (e.g., on room transition or respawn).
        /// </summary>
        public void RequestSnap(Vector3 worldPosition, string reason = null)
        {
            _snapPending = true;
            var evt = new CameraSnapRequestedEvent(worldPosition, reason ?? string.Empty);
            OnSnapRequested?.Invoke(evt);
            _eventBus?.Publish(in evt);
        }

        /// <summary>
        /// Requests a directional camera impulse/shake (forwarded to Cinemachine Impulse Source in Unity).
        /// </summary>
        public void RequestImpulse(Vector3 velocity, float strength = 1f, string sourceId = null)
        {
            var evt = new CameraImpulseRequestedEvent(velocity, strength, sourceId ?? string.Empty);
            OnImpulseRequested?.Invoke(evt);
            _eventBus?.Publish(in evt);
        }

        /// <summary>
        /// Clears all zones and resets look-ahead, vertical peek, and zoom state.
        /// </summary>
        public void Reset()
        {
            Zones.Clear();
            _hasInitializedFacing = false;
            _desiredLookAheadSign = 1f;
            _pendingLookAheadSign = 1f;
            _lookAheadTurnTimer = 0f;
            _lookAheadOffsetX = 0f;
            _peekDirection = 0;
            _verticalPeekHoldTimer = 0f;
            _verticalPeekOffsetY = 0f;
            _currentOrthographicSize = Math.Max(0.1f, Config.DefaultOrthographicSize);
            _snapPending = false;
            CurrentOutput = new CameraFrameOutput(
                Vector3.Zero,
                Vector3.Zero,
                _currentOrthographicSize,
                string.Empty,
                string.Empty,
                isPositionLocked: false,
                snapRequested: false);
        }

        private static float ExpSmooth(float current, float target, float rate, float deltaTime)
        {
            if (rate <= 0f)
            {
                return target;
            }

            float factor = 1f - (float)Math.Exp(-rate * deltaTime);
            return current + (target - current) * factor;
        }
    }
}
