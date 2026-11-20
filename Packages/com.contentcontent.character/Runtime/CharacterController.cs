using System;
using System.Numerics;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Events;
using ContentContent.Core.Fsm;

namespace ContentContent.Character
{
    /// <summary>
    /// High-level pure C# character controller composing <see cref="CharacterMotor"/>,
    /// <see cref="CharacterActionRunner"/>, Core <see cref="StateMachine"/>, Core <see cref="Blackboard"/>,
    /// and optional Core <see cref="IEventBus"/>.
    /// </summary>
    public sealed class CharacterController
    {
        /// <summary>Active configuration.</summary>
        public CharacterConfig Config { get; }

        /// <summary>Underlying kinematic motor.</summary>
        public CharacterMotor Motor { get; }

        /// <summary>Action runner managing cooldown-gated actions and movement locks.</summary>
        public CharacterActionRunner Actions { get; }

        /// <summary>Core HFSM driving locomotion states (<see cref="Idle"/>, <see cref="Move"/>, <see cref="Airborne"/>, <see cref="Dash"/>).</summary>
        public StateMachine StateMachine { get; }

        /// <summary>Core <see cref="Blackboard"/> synchronized with <see cref="CharacterBlackboardKeys"/> every tick.</summary>
        public Blackboard Blackboard { get; }

        /// <summary>Optional Core <see cref="IEventBus"/> for struct event publishing.</summary>
        public IEventBus EventBus { get; }

        /// <summary>Shared context passed to HFSM states and actions.</summary>
        public CharacterContext Context { get; }

        /// <summary>Built-in Idle locomotion state.</summary>
        public IdleState Idle { get; }

        /// <summary>Built-in Move locomotion state.</summary>
        public MoveState Move { get; }

        /// <summary>Built-in Airborne locomotion state.</summary>
        public AirborneState Airborne { get; }

        /// <summary>Built-in Dash locomotion state.</summary>
        public DashState Dash { get; }

        /// <summary>Currently active HFSM state.</summary>
        public IState CurrentState => StateMachine.CurrentState;

        /// <summary>Current combined motor velocity.</summary>
        public Vector3 Velocity => Motor.Velocity;

        /// <summary>Fired whenever the locomotion <see cref="StateMachine"/> changes state.</summary>
        public event Action<CharacterStateChangedEvent> OnStateChanged;

        /// <summary>
        /// Creates a new <see cref="CharacterController"/> with default locomotion HFSM transitions wired.
        /// </summary>
        public CharacterController(
            CharacterConfig config = null,
            Blackboard blackboard = null,
            IEventBus eventBus = null)
        {
            Config = config ?? new CharacterConfig();
            Blackboard = blackboard ?? new Blackboard();
            EventBus = eventBus;

            Motor = new CharacterMotor(Config, EventBus);
            Actions = new CharacterActionRunner(EventBus);
            Context = new CharacterContext(Config, Motor, Actions, Blackboard, EventBus);

            Idle = new IdleState(Context);
            Move = new MoveState(Context);
            Airborne = new AirborneState(Context);
            Dash = new DashState(Context);

            StateMachine = new StateMachine(Idle);
            ConfigureDefaultTransitions();

            StateMachine.OnStateChanged += HandleStateChanged;
            StateMachine.OnEnter();
            SyncBlackboard();
        }

        /// <summary>
        /// Advances actions, motor kinematics, HFSM transitions, and Blackboard sync by <paramref name="deltaTime"/>.
        /// </summary>
        public Vector3 Tick(float deltaTime, in CharacterInput input, in MotorSensors sensors)
        {
            Context.CurrentInput = input;
            Context.CurrentSensors = sensors;

            if (deltaTime <= 0f)
            {
                return Motor.Velocity;
            }

            // 1. Tick actions first so active lock flags apply immediately to motor kinematics
            Actions.Tick(Context, deltaTime);

            // 2. Advance kinematic motor with active action lock flags
            Vector3 velocity = Motor.Tick(deltaTime, in input, in sensors, Actions.ActiveLockFlags);

            // 3. Synchronize Blackboard before HFSM transitions so Blackboard-driven custom transitions see fresh data
            SyncBlackboard();

            // 4. Advance locomotion HFSM
            StateMachine.OnTick(deltaTime);

            return velocity;
        }

        /// <summary>
        /// Forwards fixed-timestep ticks to the underlying <see cref="StateMachine"/>.
        /// </summary>
        public void FixedTick(float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0f)
            {
                return;
            }

            StateMachine.OnFixedTick(fixedDeltaTime);
        }

        /// <summary>
        /// Registers an <see cref="ICharacterAction"/> on <see cref="Actions"/>.
        /// </summary>
        public void RegisterAction(ICharacterAction action)
        {
            Actions.Register(action);
        }

        /// <summary>
        /// Attempts to start a registered action immediately (or buffers it if <paramref name="bufferWindow"/> &gt; 0).
        /// </summary>
        public bool TryStartAction(string actionId, float bufferWindow = 0f)
        {
            bool started = Actions.TryStart(actionId, Context, bufferWindow);
            if (started)
            {
                SyncBlackboard();
            }

            return started;
        }

        /// <summary>
        /// Resets the motor, cancels any active action, returns the HFSM to <see cref="Idle"/>, and syncs the Blackboard.
        /// </summary>
        public void Reset()
        {
            Actions.CancelActive(Context);
            Actions.ClearBuffer();
            Motor.Reset();
            StateMachine.SetState(Idle);
            SyncBlackboard();
        }

        private void ConfigureDefaultTransitions()
        {
            // Highest priority: entering Dash from any locomotion state
            StateMachine.AddAnyTransition(Dash, () => Motor.IsDashing, priority: 100);

            // Leaving Dash
            StateMachine.AddTransition(Dash, Airborne, () => !Motor.IsDashing && !Motor.IsGrounded, priority: 20);
            StateMachine.AddTransition(Dash, Move, () => !Motor.IsDashing && Motor.IsGrounded && IsMoving(), priority: 10);
            StateMachine.AddTransition(Dash, Idle, () => !Motor.IsDashing && Motor.IsGrounded && !IsMoving(), priority: 0);

            // Grounded <-> Airborne
            StateMachine.AddTransition(Idle, Airborne, () => !Motor.IsGrounded, priority: 20);
            StateMachine.AddTransition(Move, Airborne, () => !Motor.IsGrounded, priority: 20);
            StateMachine.AddTransition(Airborne, Move, () => Motor.IsGrounded && IsMoving(), priority: 10);
            StateMachine.AddTransition(Airborne, Idle, () => Motor.IsGrounded && !IsMoving(), priority: 0);

            // Idle <-> Move
            StateMachine.AddTransition(Idle, Move, IsMoving, priority: 0);
            StateMachine.AddTransition(Move, Idle, () => !IsMoving(), priority: 0);
        }

        private bool IsMoving()
        {
            return Motor.PlanarSpeed >= Config.MinMoveThreshold;
        }

        private void SyncBlackboard()
        {
            Vector3 velocity = Motor.Velocity;
            Blackboard.Set(CharacterBlackboardKeys.Velocity, velocity);
            Blackboard.Set(CharacterBlackboardKeys.Speed, velocity.Length());
            Blackboard.Set(CharacterBlackboardKeys.PlanarSpeed, Motor.PlanarSpeed);
            Blackboard.Set(CharacterBlackboardKeys.VerticalSpeed, Motor.VerticalSpeed);
            Blackboard.Set(CharacterBlackboardKeys.FacingDirection, Motor.FacingDirection);
            Blackboard.Set(CharacterBlackboardKeys.IsGrounded, Motor.IsGrounded);
            Blackboard.Set(CharacterBlackboardKeys.IsDashing, Motor.IsDashing);
            Blackboard.Set(CharacterBlackboardKeys.AirJumpsRemaining, Motor.AirJumpsRemaining);
            Blackboard.Set(CharacterBlackboardKeys.ActiveActionId, Actions.ActiveAction != null ? Actions.ActiveAction.Id : string.Empty);
            Blackboard.Set(CharacterBlackboardKeys.IsActionActive, Actions.IsActionActive);
        }

        private void HandleStateChanged(IState previous, IState next)
        {
            var evt = new CharacterStateChangedEvent(previous, next);
            OnStateChanged?.Invoke(evt);
            EventBus?.Publish(in evt);
        }
    }
}
