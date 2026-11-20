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
        private Vector3 _carriedPlatformVelocity;
        private bool _hasTicked;
        private bool _wasGrounded;
        private bool _jumpedThisTick;
        private bool _bufferedJumpReleased;
        private float _wallCoyoteRemaining;
        private Vector3 _lastWallNormal;
        private float _wallJumpLockRemaining;
        private float _dropThroughRemaining;

        /// <summary>Active motor configuration.</summary>
        public CharacterConfig Config { get; }

        /// <summary>Input buffer used for jump and dash buffering (also usable for custom inputs).</summary>
        public InputBuffer InputBuffer => _inputBuffer;

        /// <summary>Current combined velocity (locomotion + external impulse + carried platform velocity while grounded).</summary>
        public Vector3 Velocity => _locomotionVelocity + _externalVelocity + (IsGrounded ? _carriedPlatformVelocity : Vector3.Zero);

        /// <summary>Current locomotion-only velocity (excluding external impulses and platform carry).</summary>
        public Vector3 LocomotionVelocity => _locomotionVelocity;

        /// <summary>Current decaying external impulse velocity.</summary>
        public Vector3 ExternalVelocity => _externalVelocity;

        /// <summary>Current carried velocity from the supporting moving platform while grounded.</summary>
        public Vector3 CarriedPlatformVelocity => _carriedPlatformVelocity;

        /// <summary>Current normalized facing direction.</summary>
        public Vector3 FacingDirection { get; private set; }

        /// <summary>Latest sensor snapshot passed to <see cref="Tick"/>.</summary>
        public MotorSensors Sensors { get; private set; }

        /// <summary>True when supported by ground (always true in <see cref="MovementMode.TopDown2D"/>).</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>True while a dash burst is actively executing.</summary>
        public bool IsDashing => _dashRemaining > 0f;

        /// <summary>True while wall-sliding along a wall in <see cref="MovementMode.SideScroller2D"/>.</summary>
        public bool IsWallSliding { get; private set; }

        /// <summary>True while actively grabbing/climbing a wall in <see cref="MovementMode.SideScroller2D"/>.</summary>
        public bool IsWallClimbing { get; private set; }

        /// <summary>Outward surface normal of the currently contacted wall, or zero if not touching a wall.</summary>
        public Vector3 WallNormal { get; private set; }

        /// <summary>Remaining dash burst duration in seconds.</summary>
        public float DashTimeRemaining => _dashRemaining;

        /// <summary>Remaining dash cooldown in seconds (0 when ready).</summary>
        public float DashCooldownRemaining => _dashCooldown.Remaining;

        /// <summary>True when the dash cooldown has elapsed.</summary>
        public bool IsDashCooldownReady => _dashCooldown.IsReady;

        /// <summary>Remaining coyote time grace window in seconds.</summary>
        public float CoyoteTimeRemaining => _coyoteRemaining;

        /// <summary>Remaining wall-jump coyote grace window in seconds.</summary>
        public float WallCoyoteTimeRemaining => _wallCoyoteRemaining;

        /// <summary>Remaining horizontal steering lock duration after a wall jump in seconds.</summary>
        public float WallJumpLockRemaining => _wallJumpLockRemaining;

        /// <summary>Remaining climb stamina before touching ground.</summary>
        public float ClimbStamina { get; private set; }

        /// <summary>True while ignoring one-way platforms after pressing drop-through.</summary>
        public bool IsDroppingThroughPlatform => _dropThroughRemaining > 0f;

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

        /// <summary>Fired when the character executes a grounded, coyote, wall, or air jump.</summary>
        public event Action<CharacterJumpedEvent> OnJumped;

        /// <summary>Fired when the character executes a wall jump.</summary>
        public event Action<CharacterWallJumpedEvent> OnWallJumped;

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
            ClimbStamina = Math.Max(0f, Config.MaxClimbStamina);
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
            _wallJumpLockRemaining = Math.Max(0f, _wallJumpLockRemaining - deltaTime);
            _dropThroughRemaining = Math.Max(0f, _dropThroughRemaining - deltaTime);

            // Handle one-way platform drop-through
            bool initiatedDropThrough = false;
            if (hasGravityMode &&
                sensors.IsGrounded &&
                sensors.IsOnOneWayPlatform &&
                input.DropThroughPressed &&
                (locks & ActionLockFlags.BlockMovement) == 0)
            {
                _dropThroughRemaining = Math.Max(deltaTime, Config.OneWayDropThroughDuration);
                groundedNow = false;
                _coyoteRemaining = 0f;
                _carriedPlatformVelocity = Vector3.Zero;
                initiatedDropThrough = true;
            }
            else if (hasGravityMode && _dropThroughRemaining > 0f && sensors.IsOnOneWayPlatform)
            {
                groundedNow = false;
            }

            // Buffer new button presses & track early jump release while buffered
            if (input.JumpPressed && hasGravityMode && !initiatedDropThrough)
            {
                _inputBuffer.Buffer(JumpBufferKey, Math.Max(deltaTime, Config.JumpBufferTime));
                _bufferedJumpReleased = false;
            }

            if (input.JumpReleased && _inputBuffer.HasBuffered(JumpBufferKey))
            {
                _bufferedJumpReleased = true;
            }
            else if (!_inputBuffer.HasBuffered(JumpBufferKey))
            {
                _bufferedJumpReleased = false;
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
                ClimbStamina = Math.Max(0f, Config.MaxClimbStamina);

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
                ClimbStamina = Math.Max(0f, Config.MaxClimbStamina);
                _wallCoyoteRemaining = 0f;
                _wallJumpLockRemaining = 0f;
                IsWallSliding = false;
                IsWallClimbing = false;
                _carriedPlatformVelocity = sensors.PlatformVelocity;
            }
            else
            {
                _coyoteRemaining = Math.Max(0f, _coyoteRemaining - deltaTime);
                _carriedPlatformVelocity = Vector3.Zero;
            }

            // Update wall contact & wall-jump coyote window (2D side-scroller)
            if (Config.Mode == MovementMode.SideScroller2D && !groundedNow && sensors.IsTouchingWall)
            {
                float nx = sensors.WallNormal.X;
                Vector3 wallNormal = Math.Abs(nx) > 0.0001f
                    ? new Vector3(nx > 0f ? 1f : -1f, 0f, 0f)
                    : (FacingDirection.X >= 0f ? -Vector3.UnitX : Vector3.UnitX);

                WallNormal = wallNormal;
                _lastWallNormal = wallNormal;
                _wallCoyoteRemaining = Math.Max(0f, Config.WallJumpCoyoteTime);
            }
            else
            {
                _wallCoyoteRemaining = Math.Max(0f, _wallCoyoteRemaining - deltaTime);
                if (!sensors.IsTouchingWall || groundedNow)
                {
                    WallNormal = Vector3.Zero;
                }
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
            if ((locks & ActionLockFlags.BlockTurn) == 0 && !IsDashing && _wallJumpLockRemaining <= 0f && moveMag >= Config.MinMoveThreshold)
            {
                FacingDirection = NormalizePlanar(movePlanar, Config.Mode, FacingDirection);
            }

            // Try to start a dash
            if (!IsDashing && (locks & ActionLockFlags.BlockDash) == 0 && _inputBuffer.HasBuffered(DashBufferKey))
            {
                TryStartDash(input.Move, movePlanar, moveMag, groundedNow);
            }

            // Active dash update vs normal locomotion
            if (IsDashing)
            {
                IsWallSliding = false;
                IsWallClimbing = false;

                _dashRemaining -= deltaTime;
                _locomotionVelocity = _dashDirection * Config.DashSpeed;

                if (hasGravityMode && !Config.DashSuspendsGravity && (locks & ActionLockFlags.SuspendGravity) == 0)
                {
                    ApplyGravity(deltaTime, groundedNow, sensors.IsTouchingCeiling, input.JumpHeld, input.Move.Y);
                }
                else if (hasGravityMode && Config.DashSuspendsGravity &&
                         !(Config.Mode == MovementMode.SideScroller2D && Config.AllowOmnidirectionalDash2D))
                {
                    _locomotionVelocity.Y = 0f;
                }

                if (_dashRemaining <= 0f)
                {
                    _dashRemaining = 0f;
                    if (Config.DashEndSpeedMultiplier >= 0f && Config.DashEndSpeedMultiplier < 1f)
                    {
                        _locomotionVelocity *= Config.DashEndSpeedMultiplier;
                    }
                }
            }
            else
            {
                IsWallSliding = false;
                IsWallClimbing = false;

                bool canWallClimb = Config.Mode == MovementMode.SideScroller2D &&
                                    Config.EnableWallClimb &&
                                    !groundedNow &&
                                    sensors.IsTouchingWall &&
                                    input.GrabHeld &&
                                    ClimbStamina > 0f &&
                                    (locks & ActionLockFlags.BlockMovement) == 0 &&
                                    _wallJumpLockRemaining <= 0f;

                if (canWallClimb)
                {
                    IsWallClimbing = true;
                    _locomotionVelocity.X = 0f;

                    float climbInput = Math.Abs(input.Move.Y) >= Config.MinMoveThreshold
                        ? (input.Move.Y > 0f ? 1f : -1f)
                        : 0f;

                    _locomotionVelocity.Y = climbInput * Math.Abs(Config.WallClimbSpeed);

                    float drainRate = climbInput > 0f
                        ? Config.ClimbStaminaDrainPerSecond
                        : Config.ClimbHoldStaminaDrainPerSecond;
                    ClimbStamina = Math.Max(0f, ClimbStamina - Math.Max(0f, drainRate) * deltaTime);

                    if (ClimbStamina <= 0f)
                    {
                        IsWallClimbing = false;
                    }
                }
                else if (_wallJumpLockRemaining <= 0f || groundedNow)
                {
                    UpdatePlanarVelocity(deltaTime, movePlanar, moveMag, groundedNow, locks);
                }

                if (hasGravityMode)
                {
                    bool cutAppliedThisTick = false;

                    // Try to execute a jump (Grounded/Coyote -> Wall/WallCoyote -> Air)
                    if ((locks & ActionLockFlags.BlockJump) == 0 && _inputBuffer.HasBuffered(JumpBufferKey))
                    {
                        cutAppliedThisTick = TryExecuteJump(groundedNow, sensors.IsTouchingWall, input.GrabHeld);
                    }

                    // Variable jump height cut on early release
                    if (input.JumpReleased && !cutAppliedThisTick && _locomotionVelocity.Y > 0f && Config.JumpCutMultiplier < 1f)
                    {
                        float cut = Clamp01(Config.JumpCutMultiplier);
                        _locomotionVelocity.Y *= cut;
                    }

                    // Gravity, apex hang, fast-fall & vertical clamping
                    if (!IsWallClimbing)
                    {
                        if ((locks & ActionLockFlags.SuspendGravity) == 0)
                        {
                            ApplyGravity(deltaTime, groundedNow && !_jumpedThisTick, sensors.IsTouchingCeiling, input.JumpHeld, input.Move.Y);
                        }
                        else if (sensors.IsTouchingCeiling && _locomotionVelocity.Y > 0f)
                        {
                            _locomotionVelocity.Y = 0f;
                        }
                    }

                    // Wall slide clamp (2D side-scroller)
                    if (Config.Mode == MovementMode.SideScroller2D &&
                        Config.EnableWallSlide &&
                        !groundedNow &&
                        !_jumpedThisTick &&
                        !IsWallClimbing &&
                        sensors.IsTouchingWall &&
                        _locomotionVelocity.Y <= 0f &&
                        (locks & ActionLockFlags.BlockMovement) == 0)
                    {
                        bool pushingIntoWall = !Config.WallSlideRequiresInput ||
                                               (movePlanar.X * WallNormal.X < -Config.MinMoveThreshold);

                        if (pushingIntoWall)
                        {
                            IsWallSliding = true;
                            float slideSpeed = Math.Abs(Config.WallSlideMaxFallSpeed);
                            if (_locomotionVelocity.Y < -slideSpeed)
                            {
                                _locomotionVelocity.Y = -slideSpeed;
                            }

                            if (Config.RefreshAirDashOnWallSlide)
                            {
                                RefillAirDashes();
                            }
                        }
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
        /// Refills remaining mid-air jump charges (defaults to <see cref="CharacterConfig.MaxAirJumps"/>).
        /// </summary>
        public void RefillAirJumps(int? count = null)
        {
            AirJumpsRemaining = count.HasValue
                ? Math.Max(0, count.Value)
                : Math.Max(0, Config.MaxAirJumps);
        }

        /// <summary>
        /// Refills remaining mid-air dash charges (defaults to <see cref="CharacterConfig.MaxAirDashes"/>)
        /// and optionally resets the dash cooldown.
        /// </summary>
        public void RefillAirDashes(int? count = null, bool resetCooldown = false)
        {
            AirDashesRemaining = count.HasValue
                ? Math.Max(0, count.Value)
                : Math.Max(0, Config.MaxAirDashes);

            if (resetCooldown)
            {
                _dashCooldown.Reset();
            }
        }

        /// <summary>
        /// Refills wall-climb stamina back to <see cref="CharacterConfig.MaxClimbStamina"/>.
        /// </summary>
        public void RefillClimbStamina()
        {
            ClimbStamina = Math.Max(0f, Config.MaxClimbStamina);
        }

        /// <summary>
        /// Overrides vertical velocity upward (e.g., Hollow Knight down-slash pogo or bounce pad)
        /// and optionally refills mid-air jump and dash charges.
        /// </summary>
        public void ApplyBounce(float upwardSpeed, bool refillAirJumps = true, bool refillAirDashes = true)
        {
            _locomotionVelocity.Y = Math.Abs(upwardSpeed);
            _dashRemaining = 0f;
            IsGrounded = false;
            _coyoteRemaining = 0f;
            IsWallSliding = false;
            IsWallClimbing = false;
            _carriedPlatformVelocity = Vector3.Zero;

            if (refillAirJumps)
            {
                RefillAirJumps();
            }

            if (refillAirDashes)
            {
                RefillAirDashes();
            }
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
        /// Resets velocity, timers, cooldowns, buffers, wall state, and air charges to their initial state.
        /// </summary>
        public void Reset()
        {
            _locomotionVelocity = Vector3.Zero;
            _externalVelocity = Vector3.Zero;
            _carriedPlatformVelocity = Vector3.Zero;
            _coyoteRemaining = 0f;
            _dashRemaining = 0f;
            _wallCoyoteRemaining = 0f;
            _lastWallNormal = Vector3.Zero;
            _wallJumpLockRemaining = 0f;
            _dropThroughRemaining = 0f;
            _bufferedJumpReleased = false;
            _dashCooldown.Reset();
            _inputBuffer.Clear();
            AirJumpsRemaining = Math.Max(0, Config.MaxAirJumps);
            AirDashesRemaining = Math.Max(0, Config.MaxAirDashes);
            ClimbStamina = Math.Max(0f, Config.MaxClimbStamina);
            _hasTicked = false;
            _wasGrounded = false;
            _jumpedThisTick = false;
            IsWallSliding = false;
            IsWallClimbing = false;
            WallNormal = Vector3.Zero;
            IsGrounded = Config.Mode == MovementMode.TopDown2D;
        }

        private void TryStartDash(Vector3 rawMove, Vector3 movePlanar, float moveMag, bool groundedNow)
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

            if (Config.Mode == MovementMode.SideScroller2D && Config.AllowOmnidirectionalDash2D)
            {
                Vector3 aim2D = new Vector3(rawMove.X, rawMove.Y, 0f);
                float aimLenSq = aim2D.LengthSquared();
                _dashDirection = aimLenSq >= Config.MinMoveThreshold * Config.MinMoveThreshold
                    ? aim2D / (float)Math.Sqrt(aimLenSq)
                    : FacingDirection;

                if (Math.Abs(_dashDirection.X) > 0.0001f)
                {
                    FacingDirection = new Vector3(_dashDirection.X > 0f ? 1f : -1f, 0f, 0f);
                }
            }
            else
            {
                _dashDirection = moveMag >= Config.MinMoveThreshold
                    ? NormalizePlanar(movePlanar, Config.Mode, FacingDirection)
                    : FacingDirection;

                FacingDirection = _dashDirection;
            }

            _dashRemaining = Config.DashDuration;
            _dashCooldown.Trigger(Math.Max(0f, Config.DashCooldown));
            IsWallSliding = false;
            IsWallClimbing = false;
            _wallJumpLockRemaining = 0f;

            var dashedEvent = new CharacterDashedEvent(_dashDirection, isAirDash);
            OnDashed?.Invoke(dashedEvent);
            _eventBus?.Publish(in dashedEvent);
        }

        private bool TryExecuteJump(bool groundedNow, bool touchingWall, bool grabHeld)
        {
            bool canGroundedJump = groundedNow || _coyoteRemaining > 0f;
            bool canWallJump = !canGroundedJump &&
                               Config.Mode == MovementMode.SideScroller2D &&
                               Config.EnableWallJump &&
                               (touchingWall || _wallCoyoteRemaining > 0f);
            bool canAirJump = !canGroundedJump && !canWallJump && AirJumpsRemaining > 0;

            if (!canGroundedJump && !canWallJump && !canAirJump)
            {
                return false;
            }

            if (!_inputBuffer.Consume(JumpBufferKey))
            {
                return false;
            }

            bool appliedCut = false;

            if (canWallJump)
            {
                Vector3 jumpWallNormal = touchingWall && WallNormal.LengthSquared() > 0.0001f
                    ? WallNormal
                    : (_lastWallNormal.LengthSquared() > 0.0001f ? _lastWallNormal : -FacingDirection);

                float awaySign = jumpWallNormal.X >= 0f ? 1f : -1f;
                _locomotionVelocity.X = awaySign * Math.Abs(Config.WallJumpHorizontalSpeed);
                _locomotionVelocity.Y = Math.Abs(Config.WallJumpVerticalSpeed);
                FacingDirection = new Vector3(awaySign, 0f, 0f);

                _wallJumpLockRemaining = Math.Max(0f, Config.WallJumpLockTime);
                _wallCoyoteRemaining = 0f;
                _coyoteRemaining = 0f;
                _jumpedThisTick = true;
                IsGrounded = false;
                IsWallSliding = false;
                IsWallClimbing = false;

                if (Config.EnableWallClimb && grabHeld)
                {
                    ClimbStamina = Math.Max(0f, ClimbStamina - Math.Max(0f, Config.WallJumpStaminaCost));
                }

                if (Config.RefreshAirJumpOnWallJump)
                {
                    RefillAirJumps();
                }

                if (Config.RefreshAirDashOnWallSlide)
                {
                    RefillAirDashes();
                }

                if (_bufferedJumpReleased && Config.JumpCutMultiplier < 1f)
                {
                    _locomotionVelocity.Y *= Clamp01(Config.JumpCutMultiplier);
                    appliedCut = true;
                }
                _bufferedJumpReleased = false;

                var wallJumpedEvent = new CharacterWallJumpedEvent(jumpWallNormal, Velocity);
                OnWallJumped?.Invoke(wallJumpedEvent);
                _eventBus?.Publish(in wallJumpedEvent);

                var jumpedEvent = new CharacterJumpedEvent(false, AirJumpsRemaining, Velocity, isWallJump: true);
                OnJumped?.Invoke(jumpedEvent);
                _eventBus?.Publish(in jumpedEvent);
                return appliedCut;
            }

            float inheritedUpward = 0f;
            if (canAirJump)
            {
                AirJumpsRemaining--;
            }
            else if (groundedNow && _carriedPlatformVelocity.LengthSquared() > 0.0001f && Config.PlatformMomentumInheritance > 0f)
            {
                Vector3 inherited = _carriedPlatformVelocity * Config.PlatformMomentumInheritance;
                _externalVelocity += ExtractPlanar(inherited, Config.Mode);
                if (inherited.Y > 0f)
                {
                    inheritedUpward = inherited.Y;
                }
                _carriedPlatformVelocity = Vector3.Zero;
            }

            _coyoteRemaining = 0f;
            _jumpedThisTick = true;
            IsGrounded = false;
            IsWallSliding = false;
            IsWallClimbing = false;
            _locomotionVelocity.Y = Config.JumpVelocity + inheritedUpward;

            if (_bufferedJumpReleased && Config.JumpCutMultiplier < 1f)
            {
                _locomotionVelocity.Y *= Clamp01(Config.JumpCutMultiplier);
                appliedCut = true;
            }
            _bufferedJumpReleased = false;

            var jumpEvt = new CharacterJumpedEvent(canAirJump, AirJumpsRemaining, Velocity, isWallJump: false);
            OnJumped?.Invoke(jumpEvt);
            _eventBus?.Publish(in jumpEvt);
            return appliedCut;
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

        private void ApplyGravity(float deltaTime, bool groundedNow, bool touchingCeiling, bool jumpHeld, float moveY)
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

            if (Config.ApexVelocityThreshold > 0f &&
                jumpHeld &&
                Math.Abs(_locomotionVelocity.Y) < Config.ApexVelocityThreshold)
            {
                gravity *= Clamp01(Config.ApexGravityMultiplier);
            }

            _locomotionVelocity.Y -= gravity * deltaTime;

            float maxFall = Math.Abs(Config.MaxFallSpeed);
            if (Config.FastFallMaxSpeed > maxFall && moveY < -0.5f)
            {
                maxFall = Math.Abs(Config.FastFallMaxSpeed);
            }

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
