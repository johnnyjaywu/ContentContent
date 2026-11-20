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
    }
}
