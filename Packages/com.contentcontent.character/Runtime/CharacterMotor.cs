using System;
using System.Numerics;
using ContentContent.Core.Events;
using ContentContent.Core.Timers;

namespace ContentContent.Character
{
    /// <summary>
    /// Pure C# sensor-in / velocity-out kinematic character motor supporting
    /// <see cref="MovementMode.SideScroller2D"/>, <see cref="MovementMode.TopDown2D"/>, and
    /// <see cref="MovementMode.ThirdPerson3D"/>.
    /// </summary>
    public sealed class CharacterMotor
    {
        public const string JumpBufferKey = "Jump";
        public const string DashBufferKey = "Dash";

        private readonly IEventBus _eventBus;
        private readonly InputBuffer _inputBuffer;

        private Cooldown _dashCooldown;
        private float _coyoteRemaining;
        private float _dashRemaining;
        private Vector3 _dashDirection;
        private Vector3 _locomotionVelocity;
        private Vector3 _externalVelocity;
        private bool _hasTicked;
        private bool _wasGrounded;
        private bool _jumpedThisTick;

        /// <summary>Active motor configuration.</summary>
        public CharacterConfig Config { get; }

        /// <summary>Input buffer used for jump and dash buffering (also usable for custom inputs).</summary>
        public InputBuffer InputBuffer => _inputBuffer;

        /// <summary>Current combined velocity (locomotion + external impulse).</summary>
        public Vector3 Velocity => _locomotionVelocity + _externalVelocity;

        /// <summary>Current locomotion-only velocity (excluding external impulses).</summary>
        public Vector3 LocomotionVelocity => _locomotionVelocity;

        /// <summary>Current decaying external impulse velocity.</summary>
        public Vector3 ExternalVelocity => _externalVelocity;

        /// <summary>Current normalized facing direction.</summary>
        public Vector3 FacingDirection { get; private set; }

        /// <summary>Latest sensor snapshot passed to <see cref="Tick"/>.</summary>
        public MotorSensors Sensors { get; private set; }

        /// <summary>True when supported by ground (always true in <see cref="MovementMode.TopDown2D"/>).</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>True while a dash burst is actively executing.</summary>
        public bool IsDashing => _dashRemaining > 0f;

        /// <summary>Remaining dash burst duration in seconds.</summary>
        public float DashTimeRemaining => _dashRemaining;

        /// <summary>Remaining dash cooldown in seconds (0 when ready).</summary>
        public float DashCooldownRemaining => _dashCooldown.Remaining;

        /// <summary>True when the dash cooldown has elapsed.</summary>
        public bool IsDashCooldownReady => _dashCooldown.IsReady;

        /// <summary>Remaining coyote time grace window in seconds.</summary>
        public float CoyoteTimeRemaining => _coyoteRemaining;

        /// <summary>Remaining mid-air jumps before next landing.</summary>
        public int AirJumpsRemaining { get; private set; }

        /// <summary>Remaining mid-air dashes before next landing.</summary>
        public int AirDashesRemaining { get; private set; }

        /// <summary>Current planar movement speed (X in SideScroller2D, XY in TopDown2D, XZ in ThirdPerson3D).</summary>
        public float PlanarSpeed => ExtractPlanar(Velocity, Config.Mode).Length();

        /// <summary>Current vertical speed (Y component; always 0 in TopDown2D).</summary>
        public float VerticalSpeed => Config.Mode == MovementMode.TopDown2D ? 0f : Velocity.Y;

        /// <summary>Manual capability locks applied by external systems (e.g. cutscenes, status effects).</summary>
        public ActionLockFlags ManualLocks { get; set; }

        /// <summary>Fired when the character executes a grounded, coyote, or air jump.</summary>
        public event Action<CharacterJumpedEvent> OnJumped;

        /// <summary>Fired when the character transitions from airborne to grounded.</summary>
        public event Action<CharacterLandedEvent> OnLanded;

        /// <summary>Fired when the character begins a dash burst.</summary>
        public event Action<CharacterDashedEvent> OnDashed;

        /// <summary>
        /// Creates a new <see cref="CharacterMotor"/>.
        /// </summary>
        /// <param name="config">Motor configuration. If null, default <see cref="CharacterConfig"/> values are used.</param>
        /// <param name="eventBus">Optional Core <see cref="IEventBus"/> for zero-copy struct event publishing.</param>
        /// <param name="inputBuffer">Optional shared <see cref="InputBuffer"/>. If null, creates an internal buffer.</param>
        public CharacterMotor(CharacterConfig config = null, IEventBus eventBus = null, InputBuffer inputBuffer = null)
        {
            Config = config ?? new CharacterConfig();
            _eventBus = eventBus;
            _inputBuffer = inputBuffer ?? new InputBuffer();
            _dashCooldown = new Cooldown(Math.Max(0f, Config.DashCooldown), startReady: true);

            FacingDirection = Config.Mode == MovementMode.ThirdPerson3D ? Vector3.UnitZ : Vector3.UnitX;
            AirJumpsRemaining = Math.Max(0, Config.MaxAirJumps);
            AirDashesRemaining = Math.Max(0, Config.MaxAirDashes);
            IsGrounded = Config.Mode == MovementMode.TopDown2D;
        }

        /// <summary>
        /// Advances the motor simulation by <paramref name="deltaTime"/> seconds.
        /// </summary>
        /// <param name="deltaTime">Elapsed time in seconds (ignored if &lt;= 0).</param>
        /// <param name="input">Per-tick input snapshot.</param>
        /// <param name="sensors">Per-tick environment contact snapshot.</param>
        /// <param name="additionalLocks">Additional capability locks (e.g. from active <see cref="ICharacterAction"/>).</param>
        /// <returns>The updated total <see cref="Velocity"/>.</returns>
        public Vector3 Tick(
            float deltaTime,
            in CharacterInput input,
            in MotorSensors sensors,
            ActionLockFlags additionalLocks = ActionLockFlags.None)
        {
            if (deltaTime <= 0f)
            {
                return Velocity;
            }

            ActionLockFlags locks = ManualLocks | additionalLocks;
            Sensors = sensors;
            _jumpedThisTick = false;

            bool hasGravityMode = Config.Mode != MovementMode.TopDown2D;
            bool groundedNow = !hasGravityMode || sensors.IsGrounded;

            // Advance timers
            _inputBuffer.Tick(deltaTime);
            _dashCooldown.Tick(deltaTime);

            // Buffer new button presses
            if (input.JumpPressed && hasGravityMode)
            {
                _inputBuffer.Buffer(JumpBufferKey, Math.Max(deltaTime, Config.JumpBufferTime));
            }

            if (input.DashPressed)
            {
                _inputBuffer.Buffer(DashBufferKey, Math.Max(deltaTime, Config.DashBufferTime));
            }

            // Detect landing transition (skip tick 0 so spawning on ground doesn't fire OnLanded)
            if (hasGravityMode && groundedNow && !_wasGrounded && _hasTicked && _locomotionVelocity.Y <= 0f)
            {
                float impactSpeed = Math.Abs(Velocity.Y);
                Vector3 normal = sensors.GroundNormal.LengthSquared() > 0.0001f
                    ? Vector3.Normalize(sensors.GroundNormal)
                    : Vector3.UnitY;

                AirJumpsRemaining = Math.Max(0, Config.MaxAirJumps);
                AirDashesRemaining = Math.Max(0, Config.MaxAirDashes);

                var landedEvent = new CharacterLandedEvent(impactSpeed, normal);
                OnLanded?.Invoke(landedEvent);
                _eventBus?.Publish(in landedEvent);
            }

            IsGrounded = groundedNow;

            if (groundedNow)
            {
                _coyoteRemaining = Math.Max(0f, Config.CoyoteTime);
                AirJumpsRemaining = Math.Max(0, Config.MaxAirJumps);
                AirDashesRemaining = Math.Max(0, Config.MaxAirDashes);
            }
            else
            {
                _coyoteRemaining = Math.Max(0f, _coyoteRemaining - deltaTime);
            }

            // Extract planar move input clamped to magnitude 1
            Vector3 movePlanar = ExtractPlanar(input.Move, Config.Mode);
            float moveMag = movePlanar.Length();
            if (moveMag > 1f)
            {
                movePlanar /= moveMag;
                moveMag = 1f;
            }

            // Update facing direction
            if ((locks & ActionLockFlags.BlockTurn) == 0 && !IsDashing && moveMag >= Config.MinMoveThreshold)
            {
                FacingDirection = NormalizePlanar(movePlanar, Config.Mode, FacingDirection);
            }

            // Try to start a dash
            if (!IsDashing && (locks & ActionLockFlags.BlockDash) == 0 && _inputBuffer.HasBuffered(DashBufferKey))
            {
                TryStartDash(movePlanar, moveMag, groundedNow);
            }

            // Active dash update vs normal locomotion
            if (IsDashing)
            {
                _dashRemaining -= deltaTime;
                _locomotionVelocity = _dashDirection * Config.DashSpeed;

                if (hasGravityMode && !Config.DashSuspendsGravity && (locks & ActionLockFlags.SuspendGravity) == 0)
                {
                    ApplyGravity(deltaTime, groundedNow, sensors.IsTouchingCeiling);
                }
                else if (hasGravityMode && Config.DashSuspendsGravity)
                {
                    _locomotionVelocity.Y = 0f;
                }

                if (_dashRemaining <= 0f)
                {
                    _dashRemaining = 0f;
                }
            }
            else
            {
                UpdatePlanarVelocity(deltaTime, movePlanar, moveMag, groundedNow, locks);

                if (hasGravityMode)
                {
                    // Try to execute a jump
                    if ((locks & ActionLockFlags.BlockJump) == 0 && _inputBuffer.HasBuffered(JumpBufferKey))
                    {
                        TryExecuteJump(groundedNow);
                    }

                    // Variable jump height cut on early release
                    if (input.JumpReleased && _locomotionVelocity.Y > 0f && Config.JumpCutMultiplier < 1f)
                    {
                        float cut = Clamp01(Config.JumpCutMultiplier);
                        _locomotionVelocity.Y *= cut;
                    }

                    // Gravity & vertical clamping
                    if ((locks & ActionLockFlags.SuspendGravity) == 0)
                    {
                        ApplyGravity(deltaTime, groundedNow && !_jumpedThisTick, sensors.IsTouchingCeiling);
                    }
                    else if (sensors.IsTouchingCeiling && _locomotionVelocity.Y > 0f)
                    {
                        _locomotionVelocity.Y = 0f;
                    }
                }
                else
                {
                    _locomotionVelocity.Z = 0f;
                }
            }

            // Decay external knockback/impulse velocity
            DecayExternalVelocity(deltaTime);

            // Enforce planar invariants per mode
            if (Config.Mode == MovementMode.SideScroller2D || Config.Mode == MovementMode.TopDown2D)
            {
                _locomotionVelocity.Z = 0f;
                _externalVelocity.Z = 0f;
            }

            _wasGrounded = groundedNow && !_jumpedThisTick;
            _hasTicked = true;

            return Velocity;
        }

        /// <summary>
        /// Applies an instantaneous external impulse (such as knockback or a launch pad).
        /// Vertical impulses in gravity modes are applied directly to vertical velocity so gravity arcs work naturally;
        /// planar impulses decay according to <see cref="CharacterConfig.KnockbackDecay"/>.
        /// </summary>
        public void ApplyImpulse(Vector3 impulse)
        {
            if (Config.Mode == MovementMode.TopDown2D)
            {
                _externalVelocity += new Vector3(impulse.X, impulse.Y, 0f);
                return;
            }

            Vector3 planar = ExtractPlanar(impulse, Config.Mode);
            _externalVelocity += planar;

            if (Math.Abs(impulse.Y) > 0.0001f)
            {
                _locomotionVelocity.Y += impulse.Y;
                if (impulse.Y > 0f)
                {
                    IsGrounded = false;
                    _coyoteRemaining = 0f;
                }
            }
        }

        /// <summary>
        /// Immediately sets the locomotion velocity (useful for custom actions, teleports, or scripted launches).
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            if (Config.Mode == MovementMode.SideScroller2D || Config.Mode == MovementMode.TopDown2D)
            {
                velocity.Z = 0f;
            }

            _locomotionVelocity = velocity;
        }

        /// <summary>
        /// Immediately cancels an active dash, if any.
        /// </summary>
        public void CancelDash()
        {
            _dashRemaining = 0f;
        }

        /// <summary>
        /// Sets the character's normalized facing direction along the active movement plane.
        /// </summary>
        public void SetFacingDirection(Vector3 direction)
        {
            FacingDirection = NormalizePlanar(direction, Config.Mode, FacingDirection);
        }

        /// <summary>
        /// Resets velocity, timers, cooldowns, buffers, and air charges to their initial state.
        /// </summary>
        public void Reset()
        {
            _locomotionVelocity = Vector3.Zero;
            _externalVelocity = Vector3.Zero;
            _coyoteRemaining = 0f;
            _dashRemaining = 0f;
            _dashCooldown.Reset();
            _inputBuffer.Clear();
            AirJumpsRemaining = Math.Max(0, Config.MaxAirJumps);
            AirDashesRemaining = Math.Max(0, Config.MaxAirDashes);
            _hasTicked = false;
            _wasGrounded = false;
            _jumpedThisTick = false;
            IsGrounded = Config.Mode == MovementMode.TopDown2D;
        }

        private void TryStartDash(Vector3 movePlanar, float moveMag, bool groundedNow)
        {
            if (!_dashCooldown.IsReady || Config.DashDuration <= 0f)
            {
                return;
            }

            bool isAirDash = !groundedNow;
            if (isAirDash)
            {
                if (!Config.AllowAirDash || AirDashesRemaining <= 0)
                {
                    return;
                }
            }

            if (!_inputBuffer.Consume(DashBufferKey))
            {
                return;
            }

            if (isAirDash)
            {
                AirDashesRemaining--;
            }

            _dashDirection = moveMag >= Config.MinMoveThreshold
                ? NormalizePlanar(movePlanar, Config.Mode, FacingDirection)
                : FacingDirection;

            FacingDirection = _dashDirection;
            _dashRemaining = Config.DashDuration;
            _dashCooldown.Trigger(Math.Max(0f, Config.DashCooldown));

            var dashedEvent = new CharacterDashedEvent(_dashDirection, isAirDash);
            OnDashed?.Invoke(dashedEvent);
            _eventBus?.Publish(in dashedEvent);
        }

        private void TryExecuteJump(bool groundedNow)
        {
            bool canGroundedJump = groundedNow || _coyoteRemaining > 0f;
            bool isAirJump = false;

            if (!canGroundedJump)
            {
                if (AirJumpsRemaining <= 0)
                {
                    return;
                }

                isAirJump = true;
            }

            if (!_inputBuffer.Consume(JumpBufferKey))
            {
                return;
            }

            if (isAirJump)
            {
                AirJumpsRemaining--;
            }

            _coyoteRemaining = 0f;
            _jumpedThisTick = true;
            IsGrounded = false;
            _locomotionVelocity.Y = Config.JumpVelocity;

            var jumpedEvent = new CharacterJumpedEvent(isAirJump, AirJumpsRemaining, Velocity);
            OnJumped?.Invoke(jumpedEvent);
            _eventBus?.Publish(in jumpedEvent);
        }

        private void UpdatePlanarVelocity(
            float deltaTime,
            Vector3 movePlanar,
            float moveMag,
            bool groundedNow,
            ActionLockFlags locks)
        {
            bool movementBlocked = (locks & ActionLockFlags.BlockMovement) != 0;
            bool hasInput = !movementBlocked && moveMag >= Config.MinMoveThreshold;

            Vector3 currentPlanar = ExtractPlanar(_locomotionVelocity, Config.Mode);
            Vector3 targetPlanar = hasInput ? movePlanar * Config.MaxSpeed : Vector3.Zero;

            float rate;
            if (groundedNow)
            {
                rate = hasInput ? Config.GroundAcceleration : Config.GroundDeceleration;
            }
            else
            {
                rate = hasInput ? Config.AirAcceleration : Config.AirDeceleration;
            }

            Vector3 nextPlanar = MoveTowards(currentPlanar, targetPlanar, Math.Max(0f, rate) * deltaTime);
            SetPlanar(ref _locomotionVelocity, nextPlanar, Config.Mode);
        }

        private void ApplyGravity(float deltaTime, bool groundedNow, bool touchingCeiling)
        {
            if (touchingCeiling && _locomotionVelocity.Y > 0f)
            {
                _locomotionVelocity.Y = 0f;
            }

            if (groundedNow && _locomotionVelocity.Y <= 0f)
            {
                _locomotionVelocity.Y = -Math.Abs(Config.GroundStickVelocity);
                return;
            }

            float gravity = Math.Abs(Config.Gravity);
            if (_locomotionVelocity.Y < 0f)
            {
                gravity *= Math.Max(0f, Config.FallGravityMultiplier);
            }

            _locomotionVelocity.Y -= gravity * deltaTime;

            float maxFall = Math.Abs(Config.MaxFallSpeed);
            if (maxFall > 0f && _locomotionVelocity.Y < -maxFall)
            {
                _locomotionVelocity.Y = -maxFall;
            }
        }

        private void DecayExternalVelocity(float deltaTime)
        {
            if (_externalVelocity.LengthSquared() <= 0.000001f)
            {
                _externalVelocity = Vector3.Zero;
                return;
            }

            _externalVelocity = MoveTowards(_externalVelocity, Vector3.Zero, Math.Max(0f, Config.KnockbackDecay) * deltaTime);
        }

        private static Vector3 ExtractPlanar(Vector3 v, MovementMode mode)
        {
            switch (mode)
            {
                case MovementMode.SideScroller2D:
                    return new Vector3(v.X, 0f, 0f);
                case MovementMode.TopDown2D:
                    return new Vector3(v.X, v.Y, 0f);
                case MovementMode.ThirdPerson3D:
                default:
                    return new Vector3(v.X, 0f, v.Z);
            }
        }

        private static void SetPlanar(ref Vector3 target, Vector3 planar, MovementMode mode)
        {
            switch (mode)
            {
                case MovementMode.SideScroller2D:
                    target.X = planar.X;
                    target.Z = 0f;
                    break;
                case MovementMode.TopDown2D:
                    target.X = planar.X;
                    target.Y = planar.Y;
                    target.Z = 0f;
                    break;
                case MovementMode.ThirdPerson3D:
                default:
                    target.X = planar.X;
                    target.Z = planar.Z;
                    break;
            }
        }

        private static Vector3 NormalizePlanar(Vector3 direction, MovementMode mode, Vector3 fallback)
        {
            Vector3 planar = ExtractPlanar(direction, mode);
            float lenSq = planar.LengthSquared();
            if (lenSq <= 0.000001f)
            {
                return fallback;
            }

            return planar / (float)Math.Sqrt(lenSq);
        }

        private static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
        {
            Vector3 delta = target - current;
            float distSq = delta.LengthSquared();
            if (distSq == 0f || (maxDelta >= 0f && distSq <= maxDelta * maxDelta))
            {
                return target;
            }

            float dist = (float)Math.Sqrt(distSq);
            return current + delta / dist * maxDelta;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
