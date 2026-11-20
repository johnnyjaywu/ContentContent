using System.Collections.Generic;
using System.Numerics;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Events;
using NUnit.Framework;

namespace ContentContent.Character.Tests
{
    public class CharacterControllerAndActionTests
    {
        [Test]
        public void StateMachine_TransitionsThroughIdleMoveAirborneDashAndSyncsBlackboard()
        {
            var bus = new EventBus();
            var statesVisited = new List<string>();
            bus.Subscribe<CharacterStateChangedEvent>((in CharacterStateChangedEvent e) =>
            {
                statesVisited.Add(e.NextState.ToString());
            });

            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxSpeed = 10f,
                GroundAcceleration = 100f,
                GroundDeceleration = 100f,
                DashDuration = 0.1f,
                DashCooldown = 0.5f,
                JumpVelocity = 12f
            };
            var controller = new CharacterController(config, eventBus: bus);

            Assert.AreSame(controller.Idle, controller.CurrentState);
            Assert.AreEqual(0f, controller.Blackboard.Get(CharacterBlackboardKeys.PlanarSpeed), 0.001f);

            // Move right -> Idle -> Move
            var moveRight = new CharacterInput { Move = new Vector3(1f, 0f, 0f) };
            controller.Tick(0.1f, in moveRight, MotorSensors.Grounded);
            Assert.AreSame(controller.Move, controller.CurrentState);
            Assert.AreEqual(10f, controller.Blackboard.Get(CharacterBlackboardKeys.PlanarSpeed), 0.001f);
            Assert.IsTrue(controller.Blackboard.Get(CharacterBlackboardKeys.IsGrounded));

            // Jump -> Move -> Airborne
            var jumpInput = new CharacterInput { Move = new Vector3(1f, 0f, 0f), JumpPressed = true, JumpHeld = true };
            controller.Tick(0.05f, in jumpInput, MotorSensors.Grounded);
            Assert.AreSame(controller.Airborne, controller.CurrentState);
            Assert.IsFalse(controller.Blackboard.Get(CharacterBlackboardKeys.IsGrounded));

            // Dash in mid-air -> Airborne -> Dash
            var dashInput = new CharacterInput { DashPressed = true };
            controller.Tick(0.05f, in dashInput, MotorSensors.Airborne);
            Assert.AreSame(controller.Dash, controller.CurrentState);
            Assert.IsTrue(controller.Blackboard.Get(CharacterBlackboardKeys.IsDashing));

            // Finish dash while grounded and stopped -> Dash -> Idle
            controller.Tick(0.1f, CharacterInput.None, MotorSensors.Grounded);
            controller.Tick(0.2f, CharacterInput.None, MotorSensors.Grounded);
            Assert.AreSame(controller.Idle, controller.CurrentState);

            CollectionAssert.AreEqual(new[] { "Idle", "Move", "Airborne", "Dash", "Move", "Idle" }, statesVisited);
        }

        [Test]
        public void ActionRunner_AppliesLockFlagsCooldownsAndBlackboardKeys()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxSpeed = 10f,
                GroundAcceleration = 100f,
                GroundDeceleration = 100f,
                Gravity = 20f
            };
            var controller = new CharacterController(config);

            bool ended = false;
            bool wasInterrupted = true;
            var attack = new DelegateCharacterAction(
                id: "Attack",
                duration: 0.2f,
                cooldown: 0.5f,
                priority: 1,
                lockFlags: ActionLockFlags.BlockMovement | ActionLockFlags.BlockJump,
                onEnd: (_, interrupted) =>
                {
                    ended = true;
                    wasInterrupted = interrupted;
                });

            controller.RegisterAction(attack);

            Assert.IsTrue(controller.TryStartAction("Attack"));
            Assert.IsTrue(controller.Actions.IsActionActive);
            Assert.AreEqual("Attack", controller.Blackboard.Get(CharacterBlackboardKeys.ActiveActionId));

            // While Attack is active, movement and jump inputs are suppressed
            var moveAndJump = new CharacterInput { Move = Vector3.UnitX, JumpPressed = true, JumpHeld = true };
            controller.Tick(0.1f, in moveAndJump, MotorSensors.Grounded);
            Assert.AreEqual(0f, controller.Velocity.X, 0.001f);
            Assert.LessOrEqual(controller.Velocity.Y, 0f);
            Assert.IsFalse(ended);

            // Complete action duration
            controller.Tick(0.11f, CharacterInput.None, MotorSensors.Grounded);
            Assert.IsTrue(ended);
            Assert.IsFalse(wasInterrupted);
            Assert.IsFalse(controller.Actions.IsActionActive);
            Assert.AreEqual(string.Empty, controller.Blackboard.Get(CharacterBlackboardKeys.ActiveActionId));

            // Still on cooldown -> cannot restart yet
            Assert.IsFalse(controller.TryStartAction("Attack"));

            // Advance past remaining cooldown (0.5 - 0.21 = 0.29)
            controller.Tick(0.35f, CharacterInput.None, MotorSensors.Grounded);
            Assert.IsTrue(controller.TryStartAction("Attack"));
        }

        [Test]
        public void ActionRunner_HigherPriorityActionInterruptsLowerPriorityAndBuffersLowerPriority()
        {
            var bus = new EventBus();
            var startedIds = new List<string>();
            var endedEvents = new List<CharacterActionEndedEvent>();

            bus.Subscribe<CharacterActionStartedEvent>((in CharacterActionStartedEvent e) => startedIds.Add(e.ActionId));
            bus.Subscribe<CharacterActionEndedEvent>((in CharacterActionEndedEvent e) => endedEvents.Add(e));

            var controller = new CharacterController(eventBus: bus);

            var attack = new DelegateCharacterAction("Attack", duration: 0.3f, cooldown: 0f, priority: 1);
            var hitstun = new DelegateCharacterAction("Hitstun", duration: 0.15f, cooldown: 0f, priority: 10, lockFlags: ActionLockFlags.All);
            var followUp = new DelegateCharacterAction("FollowUp", duration: 0.1f, cooldown: 0f, priority: 1);

            controller.RegisterAction(attack);
            controller.RegisterAction(hitstun);
            controller.RegisterAction(followUp);

            // Start Attack
            Assert.IsTrue(controller.TryStartAction("Attack"));

            // Hitstun (priority 10) interrupts Attack (priority 1)
            Assert.IsTrue(controller.TryStartAction("Hitstun"));
            Assert.AreEqual(1, endedEvents.Count);
            Assert.AreEqual("Attack", endedEvents[0].ActionId);
            Assert.IsTrue(endedEvents[0].Interrupted);

            // Buffer lower-priority FollowUp while Hitstun is active
            Assert.IsFalse(controller.TryStartAction("FollowUp", bufferWindow: 0.2f));
            Assert.AreEqual("Hitstun", controller.Actions.ActiveAction.Id);

            // Tick past Hitstun duration -> Hitstun ends and buffered FollowUp automatically starts
            controller.Tick(0.16f, CharacterInput.None, MotorSensors.Grounded);
            Assert.IsTrue(controller.Actions.IsActionActive);
            Assert.AreEqual("FollowUp", controller.Actions.ActiveAction.Id);

            CollectionAssert.AreEqual(new[] { "Attack", "Hitstun", "FollowUp" }, startedIds);
        }

        [Test]
        public void Reset_ClearsVelocityCancelsActionAndReturnsToIdle()
        {
            var controller = new CharacterController(new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxSpeed = 10f,
                GroundAcceleration = 100f
            });

            controller.RegisterAction(new DelegateCharacterAction("Channel", duration: 5f));
            controller.TryStartAction("Channel");

            var moveRight = new CharacterInput { Move = Vector3.UnitX };
            controller.Tick(0.1f, in moveRight, MotorSensors.Grounded);
            Assert.AreSame(controller.Move, controller.CurrentState);
            Assert.IsTrue(controller.Actions.IsActionActive);

            controller.Reset();

            Assert.AreSame(controller.Idle, controller.CurrentState);
            Assert.IsFalse(controller.Actions.IsActionActive);
            Assert.AreEqual(Vector3.Zero, controller.Velocity);
            Assert.AreEqual(string.Empty, controller.Blackboard.Get(CharacterBlackboardKeys.ActiveActionId));
        }

        [Test]
        public void StateMachine_TransitionsThroughWallSlideAndWallClimbAndSyncsBlackboard()
        {
            var config = new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                EnableWallSlide = true,
                WallSlideRequiresInput = true,
                EnableWallClimb = true,
                MaxClimbStamina = 100f,
                Gravity = 30f
            };
            var controller = new CharacterController(config);
            var rightWall = new MotorSensors { IsGrounded = false, IsTouchingWall = true, WallNormal = -Vector3.UnitX };

            // Airborne
            controller.Tick(0.05f, CharacterInput.None, MotorSensors.Airborne);
            Assert.AreSame(controller.Airborne, controller.CurrentState);

            // Push into right wall while falling -> WallSlide
            controller.Tick(0.05f, new CharacterInput { Move = Vector3.UnitX }, in rightWall);
            Assert.AreSame(controller.WallSlide, controller.CurrentState);
            Assert.IsTrue(controller.Blackboard.Get(CharacterBlackboardKeys.IsWallSliding));
            Assert.AreEqual(-Vector3.UnitX, controller.Blackboard.Get(CharacterBlackboardKeys.WallNormal));

            // Hold Grab -> WallClimb
            controller.Tick(0.05f, new CharacterInput { Move = Vector3.UnitY, GrabHeld = true }, in rightWall);
            Assert.AreSame(controller.WallClimb, controller.CurrentState);
            Assert.IsTrue(controller.Blackboard.Get(CharacterBlackboardKeys.IsWallClimbing));
            Assert.Less(controller.Blackboard.Get(CharacterBlackboardKeys.ClimbStamina), 100f);

            // Land -> Idle
            controller.Tick(0.05f, CharacterInput.None, MotorSensors.Grounded);
            Assert.AreSame(controller.Idle, controller.CurrentState);
            Assert.IsFalse(controller.Blackboard.Get(CharacterBlackboardKeys.IsWallSliding));
            Assert.IsFalse(controller.Blackboard.Get(CharacterBlackboardKeys.IsWallClimbing));
        }
    }
}
