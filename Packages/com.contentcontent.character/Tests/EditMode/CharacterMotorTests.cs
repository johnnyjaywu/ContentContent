using System.Numerics;
using ContentContent.Core.Events;
using NUnit.Framework;

namespace ContentContent.Character.Tests
{
    public class CharacterMotorTests
    {
        [Test]
        public void InputBuffer_BuffersConsumesAndExpiresDeterministically()
        {
            var buffer = new InputBuffer();
            buffer.Buffer("Jump", 0.1f);

            Assert.IsTrue(buffer.HasBuffered("Jump"));
            Assert.AreEqual(0.1f, buffer.GetRemaining("Jump"), 0.0001f);

            buffer.Tick(0.06f);
            Assert.IsTrue(buffer.HasBuffered("Jump"));
            Assert.AreEqual(0.04f, buffer.GetRemaining("Jump"), 0.0001f);

            buffer.Tick(0.05f);
            Assert.IsFalse(buffer.HasBuffered("Jump"));
            Assert.AreEqual(0, buffer.Count);

            buffer.Buffer("Dash", 0.2f);
            Assert.IsTrue(buffer.Consume("Dash"));
            Assert.IsFalse(buffer.Consume("Dash"));
        }

        [Test]
        public void SideScroller2D_AcceleratesDeceleratesAndClampsToMaxSpeed()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxSpeed = 10f,
                GroundAcceleration = 50f,
                GroundDeceleration = 100f
            };
            var motor = new CharacterMotor(config);

            var moveRight = new CharacterInput { Move = new Vector3(1f, 0f, 0f) };
            motor.Tick(0.1f, in moveRight, MotorSensors.Grounded);
            Assert.AreEqual(5f, motor.Velocity.X, 0.001f);
            Assert.AreEqual(Vector3.UnitX, motor.FacingDirection);

            // Second tick reaches MaxSpeed and clamps
            motor.Tick(0.2f, in moveRight, MotorSensors.Grounded);
            Assert.AreEqual(10f, motor.Velocity.X, 0.001f);

            // Release input -> decelerates to 0
            motor.Tick(0.1f, CharacterInput.None, MotorSensors.Grounded);
            Assert.AreEqual(0f, motor.Velocity.X, 0.001f);
        }

        [Test]
        public void TopDown2D_MovesPlanarXYWithNoGravity()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.TopDown2D,
                MaxSpeed = 10f,
                GroundAcceleration = 100f,
                Gravity = 30f
            };
            var motor = new CharacterMotor(config);

            // Diagonal input > magnitude 1 gets normalized
            var input = new CharacterInput { Move = new Vector3(1f, 1f, 5f) };
            Vector3 vel = motor.Tick(0.2f, in input, MotorSensors.Airborne);

            Assert.IsTrue(motor.IsGrounded);
            Assert.AreEqual(0f, vel.Z, 0.0001f);
            Assert.AreEqual(10f, vel.Length(), 0.001f);
            Assert.AreEqual(vel.X, vel.Y, 0.001f);
        }

        [Test]
        public void ThirdPerson3D_MovesXZAndAppliesGravityOnY()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.ThirdPerson3D,
                MaxSpeed = 8f,
                AirAcceleration = 80f,
                Gravity = 20f,
                FallGravityMultiplier = 1f
            };
            var motor = new CharacterMotor(config);

            var input = new CharacterInput { Move = new Vector3(0f, 0f, 1f) };
            Vector3 vel = motor.Tick(0.1f, in input, MotorSensors.Airborne);

            Assert.AreEqual(0f, vel.X, 0.001f);
            Assert.AreEqual(8f, vel.Z, 0.001f);
            Assert.AreEqual(-2f, vel.Y, 0.001f);
            Assert.AreEqual(Vector3.UnitZ, motor.FacingDirection);
        }

        [Test]
        public void Jump_CoyoteTime_AllowsJumpBrieflyAfterLeavingGround()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                CoyoteTime = 0.12f,
                JumpVelocity = 12f,
                Gravity = 0f,
                MaxAirJumps = 0
            };
            var motor = new CharacterMotor(config);

            // Establish grounded state
            motor.Tick(0.05f, CharacterInput.None, MotorSensors.Grounded);

            // Walk off ledge for 0.08s (< 0.12s coyote window)
            motor.Tick(0.08f, CharacterInput.None, MotorSensors.Airborne);
            Assert.Greater(motor.CoyoteTimeRemaining, 0f);

            int jumpCount = 0;
            bool wasAirJump = true;
            motor.OnJumped += e =>
            {
                jumpCount++;
                wasAirJump = e.IsAirJump;
            };

            // Press jump within coyote window
            var jumpInput = new CharacterInput { JumpPressed = true, JumpHeld = true };
            motor.Tick(0.02f, in jumpInput, MotorSensors.Airborne);

            Assert.AreEqual(1, jumpCount);
            Assert.IsFalse(wasAirJump);
            Assert.AreEqual(12f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(0f, motor.CoyoteTimeRemaining, 0.0001f);
        }

        [Test]
        public void Jump_BufferedBeforeTouchdown_ExecutesOnLanding()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                JumpBufferTime = 0.15f,
                JumpVelocity = 14f,
                Gravity = 20f,
                MaxAirJumps = 0
            };
            var motor = new CharacterMotor(config);

            // Falling airborne
            motor.Tick(0.1f, CharacterInput.None, MotorSensors.Airborne);

            // Press jump 0.05s before landing
            var jumpPress = new CharacterInput { JumpPressed = true, JumpHeld = true };
            motor.Tick(0.05f, in jumpPress, MotorSensors.Airborne);
            Assert.Less(motor.Velocity.Y, 0f);

            int landCount = 0;
            int jumpCount = 0;
            motor.OnLanded += _ => landCount++;
            motor.OnJumped += _ => jumpCount++;

            // Touch ground next tick (without pressing jump again)
            motor.Tick(0.02f, CharacterInput.None, MotorSensors.Grounded);

            Assert.AreEqual(1, landCount);
            Assert.AreEqual(1, jumpCount);
            Assert.Greater(motor.Velocity.Y, 10f);
        }

        [Test]
        public void Jump_VariableHeightCutAndMultiJumpAndCeilingBonk()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                JumpVelocity = 12f,
                JumpCutMultiplier = 0.5f,
                Gravity = 0f,
                MaxAirJumps = 1
            };
            var motor = new CharacterMotor(config);

            // Grounded jump
            var jumpPress = new CharacterInput { JumpPressed = true, JumpHeld = true };
            motor.Tick(0.05f, in jumpPress, MotorSensors.Grounded);
            Assert.AreEqual(12f, motor.Velocity.Y, 0.001f);

            // Release jump early -> velocity halved
            var jumpRelease = new CharacterInput { JumpReleased = true };
            motor.Tick(0.05f, in jumpRelease, MotorSensors.Airborne);
            Assert.AreEqual(6f, motor.Velocity.Y, 0.001f);

            // Air jump consumes charge
            motor.Tick(0.05f, in jumpPress, MotorSensors.Airborne);
            Assert.AreEqual(0, motor.AirJumpsRemaining);
            Assert.AreEqual(12f, motor.Velocity.Y, 0.001f);

            // Ceiling bonk zeroes upward velocity
            var ceilingSensors = new MotorSensors { IsGrounded = false, IsTouchingCeiling = true };
            motor.Tick(0.05f, CharacterInput.None, in ceilingSensors);
            Assert.AreEqual(0f, motor.Velocity.Y, 0.001f);
        }

        [Test]
        public void Dash_RespectsDurationCooldownGravitySuspendAndAirDashCap()
        {
            var bus = new EventBus();
            int busDashEvents = 0;
            bus.Subscribe<CharacterDashedEvent>((in CharacterDashedEvent _) => busDashEvents++);

            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                DashSpeed = 20f,
                DashDuration = 0.1f,
                DashCooldown = 0.5f,
                DashBufferTime = 0f,
                DashSuspendsGravity = true,
                AllowAirDash = true,
                MaxAirDashes = 1,
                Gravity = 30f
            };
            var motor = new CharacterMotor(config, bus);

            // Start airborne dash with no move input -> uses default facing (+X)
            var dashInput = new CharacterInput { DashPressed = true };
            Vector3 vel = motor.Tick(0.05f, in dashInput, MotorSensors.Airborne);

            Assert.IsTrue(motor.IsDashing);
            Assert.AreEqual(1, busDashEvents);
            Assert.AreEqual(0, motor.AirDashesRemaining);
            Assert.AreEqual(20f, vel.X, 0.001f);
            Assert.AreEqual(0f, vel.Y, 0.001f);

            // Complete dash duration
            motor.Tick(0.06f, CharacterInput.None, MotorSensors.Airborne);
            Assert.IsFalse(motor.IsDashing);
            Assert.IsFalse(motor.IsDashCooldownReady);

            // Wait out cooldown while still airborne -> cannot air-dash again because MaxAirDashes == 1
            motor.Tick(0.6f, CharacterInput.None, MotorSensors.Airborne);
            Assert.IsTrue(motor.IsDashCooldownReady);
            motor.Tick(0.05f, in dashInput, MotorSensors.Airborne);
            Assert.IsFalse(motor.IsDashing);

            // Land -> resets air dash charge, can dash again
            motor.Tick(0.05f, CharacterInput.None, MotorSensors.Grounded);
            Assert.AreEqual(1, motor.AirDashesRemaining);
            motor.Tick(0.05f, in dashInput, MotorSensors.Airborne);
            Assert.IsTrue(motor.IsDashing);
        }

        [Test]
        public void ApplyImpulse_AddsExternalVelocityAndDecaysOverTime()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                KnockbackDecay = 20f,
                Gravity = 0f
            };
            var motor = new CharacterMotor(config);

            motor.ApplyImpulse(new Vector3(-10f, 5f, 0f));
            Assert.AreEqual(-10f, motor.ExternalVelocity.X, 0.001f);
            Assert.AreEqual(5f, motor.Velocity.Y, 0.001f);

            motor.Tick(0.25f, CharacterInput.None, MotorSensors.Airborne);
            Assert.AreEqual(-5f, motor.ExternalVelocity.X, 0.001f);

            motor.Tick(0.3f, CharacterInput.None, MotorSensors.Airborne);
            Assert.AreEqual(0f, motor.ExternalVelocity.X, 0.001f);
        }

        [Test]
        public void SpawningOnGround_DoesNotFalselyFireOnLanded()
        {
            var motor = new CharacterMotor(new CharacterConfig { Mode = MovementMode.SideScroller2D });
            int landedCount = 0;
            motor.OnLanded += _ => landedCount++;

            motor.Tick(0.016f, CharacterInput.None, MotorSensors.Grounded);
            motor.Tick(0.016f, CharacterInput.None, MotorSensors.Grounded);

            Assert.AreEqual(0, landedCount);
        }

        [Test]
        public void WallSlide_ClampsFallSpeedWhenPushingIntoWallAndRefreshesAirDashWhenConfigured()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                Gravity = 40f,
                FallGravityMultiplier = 1f,
                MaxFallSpeed = 25f,
                EnableWallSlide = true,
                WallSlideRequiresInput = true,
                WallSlideMaxFallSpeed = 3.5f,
                RefreshAirDashOnWallSlide = true,
                AllowAirDash = true,
                MaxAirDashes = 1,
                DashDuration = 0.05f,
                DashCooldown = 0f
            };
            var motor = new CharacterMotor(config);

            // Spend air dash first
            var dash = new CharacterInput { DashPressed = true };
            motor.Tick(0.02f, in dash, MotorSensors.Airborne);
            motor.Tick(0.05f, CharacterInput.None, MotorSensors.Airborne);
            Assert.AreEqual(0, motor.AirDashesRemaining);

            // Fall against right wall (wall normal = -X) without pushing into wall -> falls faster than slide cap
            var rightWall = new MotorSensors { IsGrounded = false, IsTouchingWall = true, WallNormal = -Vector3.UnitX };
            motor.Tick(0.2f, CharacterInput.None, in rightWall);
            Assert.IsFalse(motor.IsWallSliding);
            Assert.Less(motor.Velocity.Y, -3.5f);

            // Push right toward wall -> clamps fall speed to -3.5 and refreshes air dash
            var pushRight = new CharacterInput { Move = Vector3.UnitX };
            motor.Tick(0.05f, in pushRight, in rightWall);
            Assert.IsTrue(motor.IsWallSliding);
            Assert.AreEqual(-3.5f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(1, motor.AirDashesRemaining);
        }

        [Test]
        public void WallJump_LaunchesAwayWithSteeringLockAndPriorityOverAirJump()
        {
            var bus = new EventBus();
            int wallJumpEvents = 0;
            bool jumpedAsWall = false;
            bus.Subscribe<CharacterWallJumpedEvent>((in CharacterWallJumpedEvent _) => wallJumpEvents++);
            bus.Subscribe<CharacterJumpedEvent>((in CharacterJumpedEvent e) => jumpedAsWall = e.IsWallJump);

            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                EnableWallJump = true,
                WallJumpHorizontalSpeed = 10f,
                WallJumpVerticalSpeed = 14f,
                WallJumpLockTime = 0.15f,
                MaxAirJumps = 1,
                Gravity = 0f,
                AirAcceleration = 100f,
                MaxSpeed = 8f
            };
            var motor = new CharacterMotor(config, bus);

            // Touching right wall (normal points -X) with 1 air jump remaining
            var rightWall = new MotorSensors { IsGrounded = false, IsTouchingWall = true, WallNormal = -Vector3.UnitX };
            var jumpWhilePushingRight = new CharacterInput { Move = Vector3.UnitX, JumpPressed = true, JumpHeld = true };
            motor.Tick(0.02f, in jumpWhilePushingRight, in rightWall);

            Assert.AreEqual(1, wallJumpEvents);
            Assert.IsTrue(jumpedAsWall);
            Assert.AreEqual(-10f, motor.Velocity.X, 0.001f);
            Assert.AreEqual(14f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(-Vector3.UnitX, motor.FacingDirection);
            // Wall jump should NOT consume air jump charge
            Assert.AreEqual(1, motor.AirJumpsRemaining);

            // Steering back right during WallJumpLockTime is suppressed
            var steerRight = new CharacterInput { Move = Vector3.UnitX, JumpHeld = true };
            motor.Tick(0.05f, in steerRight, MotorSensors.Airborne);
            Assert.AreEqual(-10f, motor.Velocity.X, 0.001f);
            Assert.Greater(motor.WallJumpLockRemaining, 0f);

            // After lock expires, horizontal air control resumes
            motor.Tick(0.15f, in steerRight, MotorSensors.Airborne);
            Assert.Greater(motor.Velocity.X, -10f);
        }

        [Test]
        public void WallJump_CoyoteTimeAllowsJumpBrieflyAfterLeavingWall()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                EnableWallJump = true,
                WallJumpCoyoteTime = 0.12f,
                WallJumpHorizontalSpeed = 9f,
                WallJumpVerticalSpeed = 12f,
                Gravity = 0f
            };
            var motor = new CharacterMotor(config);

            // Contact left wall (normal +X)
            var leftWall = new MotorSensors { IsGrounded = false, IsTouchingWall = true, WallNormal = Vector3.UnitX };
            motor.Tick(0.02f, CharacterInput.None, in leftWall);

            // Separate from wall for 0.08s (< 0.12s wall coyote)
            motor.Tick(0.08f, CharacterInput.None, MotorSensors.Airborne);
            Assert.Greater(motor.WallCoyoteTimeRemaining, 0f);

            // Press jump while airborne within wall coyote -> executes wall jump away (+X)
            var jump = new CharacterInput { JumpPressed = true, JumpHeld = true };
            motor.Tick(0.02f, in jump, MotorSensors.Airborne);
            Assert.AreEqual(9f, motor.Velocity.X, 0.001f);
            Assert.AreEqual(12f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(0f, motor.WallCoyoteTimeRemaining, 0.0001f);
        }

        [Test]
        public void WallClimb_MovesVerticallyDrainsStaminaAndRefillsOnLanding()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                EnableWallClimb = true,
                WallClimbSpeed = 5f,
                MaxClimbStamina = 100f,
                ClimbStaminaDrainPerSecond = 50f,
                ClimbHoldStaminaDrainPerSecond = 10f,
                Gravity = 30f
            };
            var motor = new CharacterMotor(config);
            var rightWall = new MotorSensors { IsGrounded = false, IsTouchingWall = true, WallNormal = -Vector3.UnitX };

            // Hold grab + up -> climbs at +5, drains 50/s
            var climbUp = new CharacterInput { Move = Vector3.UnitY, GrabHeld = true };
            motor.Tick(0.5f, in climbUp, in rightWall);
            Assert.IsTrue(motor.IsWallClimbing);
            Assert.AreEqual(0f, motor.Velocity.X, 0.001f);
            Assert.AreEqual(5f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(75f, motor.ClimbStamina, 0.001f);

            // Hold still on wall -> 0 Y velocity, drains 10/s
            var holdStill = new CharacterInput { GrabHeld = true };
            motor.Tick(1f, in holdStill, in rightWall);
            Assert.AreEqual(0f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(65f, motor.ClimbStamina, 0.001f);

            // Drain remaining stamina -> falls
            motor.Tick(2f, in climbUp, in rightWall);
            Assert.AreEqual(0f, motor.ClimbStamina, 0.001f);
            Assert.IsFalse(motor.IsWallClimbing);
            Assert.Less(motor.Velocity.Y, 0f);

            // Land -> stamina refills to max
            motor.Tick(0.05f, CharacterInput.None, MotorSensors.Grounded);
            Assert.AreEqual(100f, motor.ClimbStamina, 0.001f);
        }

        [Test]
        public void Dash_Omnidirectional2D_SupportsUpAndDiagonalDashesWithoutZeroingY()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                AllowOmnidirectionalDash2D = true,
                DashSpeed = 20f,
                DashDuration = 0.1f,
                DashSuspendsGravity = true,
                MaxAirDashes = 2,
                DashCooldown = 0f
            };
            var motor = new CharacterMotor(config);

            // Straight up dash
            var upDash = new CharacterInput { Move = Vector3.UnitY, DashPressed = true };
            Vector3 vel = motor.Tick(0.05f, in upDash, MotorSensors.Airborne);
            Assert.IsTrue(motor.IsDashing);
            Assert.AreEqual(0f, vel.X, 0.001f);
            Assert.AreEqual(20f, vel.Y, 0.001f);
            // Facing stays horizontal
            Assert.AreEqual(Vector3.UnitX, motor.FacingDirection);

            motor.Tick(0.06f, CharacterInput.None, MotorSensors.Airborne);
            Assert.IsFalse(motor.IsDashing);

            // Up-left diagonal dash
            var upLeftDash = new CharacterInput { Move = new Vector3(-1f, 1f, 0f), DashPressed = true };
            vel = motor.Tick(0.05f, in upLeftDash, MotorSensors.Airborne);
            Assert.IsTrue(motor.IsDashing);
            float expectedComponent = 20f / (float)System.Math.Sqrt(2.0);
            Assert.AreEqual(-expectedComponent, vel.X, 0.01f);
            Assert.AreEqual(expectedComponent, vel.Y, 0.01f);
            Assert.AreEqual(-Vector3.UnitX, motor.FacingDirection);
        }

        [Test]
        public void Dash_EndSpeedMultiplier_ScalesExitVelocity()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                DashSpeed = 20f,
                DashDuration = 0.1f,
                DashEndSpeedMultiplier = 0.5f,
                GroundDeceleration = 0f,
                AirDeceleration = 0f,
                Gravity = 0f
            };
            var motor = new CharacterMotor(config);

            var dash = new CharacterInput { DashPressed = true };
            motor.Tick(0.05f, in dash, MotorSensors.Grounded);
            Assert.AreEqual(20f, motor.Velocity.X, 0.001f);

            // Tick finishes dash -> scales velocity by 0.5
            motor.Tick(0.06f, CharacterInput.None, MotorSensors.Grounded);
            Assert.IsFalse(motor.IsDashing);
            Assert.AreEqual(10f, motor.Velocity.X, 0.001f);
        }

        [Test]
        public void Jump_BufferedShortHop_CutsVelocityWhenReleasedBeforeLanding()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                JumpBufferTime = 0.2f,
                JumpVelocity = 14f,
                JumpCutMultiplier = 0.5f,
                Gravity = 0f
            };
            var motor = new CharacterMotor(config);

            // Press and immediately release jump while still airborne (tap before landing)
            var tapBeforeLanding = new CharacterInput { JumpPressed = true, JumpHeld = false, JumpReleased = true };
            motor.Tick(0.05f, in tapBeforeLanding, MotorSensors.Airborne);

            // Land on next tick -> fires buffered jump already cut to 7f
            motor.Tick(0.02f, CharacterInput.None, MotorSensors.Grounded);
            Assert.AreEqual(7f, motor.Velocity.Y, 0.001f);
        }

        [Test]
        public void Jump_ApexHangReducesGravityNearPeakAndFastFallIncreasesCap()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                Gravity = 20f,
                FallGravityMultiplier = 1f,
                ApexVelocityThreshold = 4f,
                ApexGravityMultiplier = 0.5f,
                MaxFallSpeed = 15f,
                FastFallMaxSpeed = 25f
            };
            var motor = new CharacterMotor(config);
            motor.SetVelocity(new Vector3(0f, 2f, 0f));

            // Inside apex threshold while holding jump -> half gravity (10 instead of 20)
            var holdJump = new CharacterInput { JumpHeld = true };
            motor.Tick(0.1f, in holdJump, MotorSensors.Airborne);
            Assert.AreEqual(1f, motor.Velocity.Y, 0.001f);

            // Drop without fast fall -> caps at MaxFallSpeed (15)
            motor.Tick(2f, CharacterInput.None, MotorSensors.Airborne);
            Assert.AreEqual(-15f, motor.Velocity.Y, 0.001f);

            // Hold down -> increases cap to FastFallMaxSpeed (25)
            var fastFall = new CharacterInput { Move = -Vector3.UnitY };
            motor.Tick(2f, in fastFall, MotorSensors.Airborne);
            Assert.AreEqual(-25f, motor.Velocity.Y, 0.001f);
        }

        [Test]
        public void ApplyBounce_OverridesDownwardVelocityAndRefillsAirCharges()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxAirJumps = 1,
                MaxAirDashes = 1,
                DashDuration = 0.1f,
                Gravity = 0f
            };
            var motor = new CharacterMotor(config);

            // Consume both air jump and air dash
            motor.Tick(0.02f, new CharacterInput { JumpPressed = true, JumpHeld = true }, MotorSensors.Airborne);
            motor.Tick(0.02f, new CharacterInput { DashPressed = true }, MotorSensors.Airborne);
            Assert.AreEqual(0, motor.AirJumpsRemaining);
            Assert.AreEqual(0, motor.AirDashesRemaining);
            Assert.IsTrue(motor.IsDashing);

            // Pogo bounce overrides dash, sets upward velocity, refills air jump & air dash
            motor.ApplyBounce(13f, refillAirJumps: true, refillAirDashes: true);
            Assert.IsFalse(motor.IsDashing);
            Assert.AreEqual(13f, motor.Velocity.Y, 0.001f);
            Assert.AreEqual(1, motor.AirJumpsRemaining);
            Assert.AreEqual(1, motor.AirDashesRemaining);
        }

        [Test]
        public void MovingPlatform_CarriesVelocityWhileGroundedAndInheritsMomentumOnJumpWithoutDoubling()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                JumpVelocity = 12f,
                Gravity = 0f,
                KnockbackDecay = 0f,
                PlatformMomentumInheritance = 1f
            };
            var motor = new CharacterMotor(config);

            var platformSensors = new MotorSensors
            {
                IsGrounded = true,
                GroundNormal = Vector3.UnitY,
                PlatformVelocity = new Vector3(4f, 2f, 0f)
            };

            // Standing on moving platform -> Velocity includes carried platform velocity once
            motor.Tick(0.05f, CharacterInput.None, in platformSensors);
            Assert.AreEqual(4f, motor.Velocity.X, 0.001f);
            Assert.AreEqual(4f, motor.CarriedPlatformVelocity.X, 0.001f);

            // Jump off platform -> inherits platform velocity cleanly without doubling
            var jump = new CharacterInput { JumpPressed = true, JumpHeld = true };
            motor.Tick(0.02f, in jump, in platformSensors);
            Assert.IsFalse(motor.IsGrounded);
            Assert.AreEqual(4f, motor.Velocity.X, 0.001f);
            Assert.AreEqual(14f, motor.Velocity.Y, 0.001f);

            // Next airborne tick preserves inherited momentum (not doubled)
            motor.Tick(0.02f, CharacterInput.None, MotorSensors.Airborne);
            Assert.AreEqual(4f, motor.Velocity.X, 0.001f);
        }

        [Test]
        public void OneWayPlatform_DropThroughDisablesGroundingWithoutJumping()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                OneWayDropThroughDuration = 0.2f,
                Gravity = 20f
            };
            var motor = new CharacterMotor(config);
            var oneWay = new MotorSensors
            {
                IsGrounded = true,
                GroundNormal = Vector3.UnitY,
                IsOnOneWayPlatform = true
            };

            motor.Tick(0.05f, CharacterInput.None, in oneWay);
            Assert.IsTrue(motor.IsGrounded);

            // Press drop-through -> ungrounds and begins falling even while sensors still overlap platform
            var drop = new CharacterInput { DropThroughPressed = true };
            motor.Tick(0.05f, in drop, in oneWay);
            Assert.IsTrue(motor.IsDroppingThroughPlatform);
            Assert.IsFalse(motor.IsGrounded);
            Assert.Less(motor.Velocity.Y, 0f);

            // Still suppressed at 0.1s (< 0.2s)
            motor.Tick(0.05f, CharacterInput.None, in oneWay);
            Assert.IsFalse(motor.IsGrounded);

            // After 0.2s passes -> platform grounding resumes if still overlapping
            motor.Tick(0.15f, CharacterInput.None, in oneWay);
            Assert.IsFalse(motor.IsDroppingThroughPlatform);
            Assert.IsTrue(motor.IsGrounded);
        }

        [Test]
        public void Presets_HollowKnightAndCeleste_ProduceValidConfigurations()
        {
            CharacterConfig hk = CharacterConfig.CreateHollowKnightPreset();
            Assert.AreEqual(MovementMode.SideScroller2D, hk.Mode);
            Assert.IsFalse(hk.AllowOmnidirectionalDash2D);
            Assert.IsTrue(hk.EnableWallSlide);
            Assert.IsTrue(hk.EnableWallJump);
            Assert.IsTrue(hk.RefreshAirDashOnWallSlide);
            Assert.IsTrue(hk.RefreshAirJumpOnWallJump);
            Assert.IsFalse(hk.EnableWallClimb);
            Assert.AreEqual(1, hk.MaxAirJumps);

            CharacterConfig celeste = CharacterConfig.CreateCelestePreset();
            Assert.AreEqual(MovementMode.SideScroller2D, celeste.Mode);
            Assert.IsTrue(celeste.AllowOmnidirectionalDash2D);
            Assert.IsTrue(celeste.EnableWallClimb);
            Assert.Greater(celeste.MaxClimbStamina, 0f);
            Assert.AreEqual(0, celeste.MaxAirJumps);
        }
    }
}
