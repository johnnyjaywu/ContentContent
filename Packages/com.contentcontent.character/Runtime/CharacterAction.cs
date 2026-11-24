using System;
using System.Collections.Generic;
using System.Numerics;
using ContentContent.Core.Events;
using ContentContent.Core.Timers;

namespace ContentContent.Character
{
    /// <summary>
    /// Contract for an extensible character action or ability (e.g. attack, dodge, cast, hitstun)
    /// executed by <see cref="CharacterActionRunner"/>.
    /// </summary>
    public interface ICharacterAction
    {
        /// <summary>Unique identifier for this action (e.g. "Attack", "Roll", "Hitstun").</summary>
        string Id { get; }

        /// <summary>Priority used when determining whether this action can interrupt another.</summary>
        int Priority { get; }

        /// <summary>Movement/input capability locks applied while this action is active.</summary>
        ActionLockFlags LockFlags { get; }

        /// <summary>True once the action has finished its execution and should end.</summary>
        bool IsFinished { get; }

        /// <summary>Returns true if the action's preconditions (cooldown, groundedness, resources, etc.) are satisfied.</summary>
        bool CanStart(CharacterContext context);

        /// <summary>Returns true if <paramref name="incoming"/> is permitted to interrupt this running action.</summary>
        bool CanBeInterruptedBy(ICharacterAction incoming);

        /// <summary>Advances internal timers such as cooldowns even when the action is not active.</summary>
        void TickCooldown(float deltaTime);

        /// <summary>Called once when the action becomes active.</summary>
        void OnStart(CharacterContext context);

        /// <summary>Called every tick while the action is active.</summary>
        void OnTick(CharacterContext context, float deltaTime);

        /// <summary>Called once when the action finishes naturally or is interrupted.</summary>
        void OnEnd(CharacterContext context, bool interrupted);
    }

    /// <summary>
    /// Convenient base class for timed, cooldown-gated <see cref="ICharacterAction"/> implementations
    /// backed by Core's <see cref="Cooldown"/>.
    /// </summary>
    public abstract class CharacterActionBase : ICharacterAction
    {
        private Cooldown _cooldown;

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public virtual ActionLockFlags LockFlags { get; }

        /// <summary>Fixed duration of this action in seconds. Set to 0 or negative for manually completed actions.</summary>
        public float Duration { get; protected set; }

        /// <summary>Elapsed active time since <see cref="OnStart"/>.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Normalized progress [0..1] when <see cref="Duration"/> &gt; 0.</summary>
        public float Progress => Duration > 0f ? Math.Min(1f, Elapsed / Duration) : 0f;

        /// <summary>Remaining cooldown in seconds (0 when ready).</summary>
        public float CooldownRemaining => _cooldown.Remaining;

        /// <summary>True when the action's cooldown is ready.</summary>
        public bool IsCooldownReady => _cooldown.IsReady;

        /// <inheritdoc />
        public bool IsFinished { get; protected set; }

        protected CharacterActionBase(
            string id,
            float duration = 0f,
            float cooldown = 0f,
            int priority = 0,
            ActionLockFlags lockFlags = ActionLockFlags.None)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Action ID must not be null or empty.", nameof(id));
            }

            Id = id;
            Duration = Math.Max(0f, duration);
            Priority = priority;
            LockFlags = lockFlags;
            _cooldown = new Cooldown(Math.Max(0f, cooldown), startReady: true);
        }

        /// <inheritdoc />
        public virtual bool CanStart(CharacterContext context)
        {
            return _cooldown.IsReady;
        }

        /// <inheritdoc />
        public virtual bool CanBeInterruptedBy(ICharacterAction incoming)
        {
            return incoming != null && incoming.Priority > Priority;
        }

        /// <inheritdoc />
        public virtual void TickCooldown(float deltaTime)
        {
            _cooldown.Tick(deltaTime);
        }

        /// <inheritdoc />
        public void OnStart(CharacterContext context)
        {
            Elapsed = 0f;
            IsFinished = false;
            if (_cooldown.Duration > 0f)
            {
                _cooldown.Trigger();
            }

            HandleStart(context);
        }

        /// <inheritdoc />
        public void OnTick(CharacterContext context, float deltaTime)
        {
            Elapsed += deltaTime;
            HandleTick(context, deltaTime);

            if (Duration > 0f && Elapsed >= Duration)
            {
                IsFinished = true;
            }
        }

        /// <inheritdoc />
        public void OnEnd(CharacterContext context, bool interrupted)
        {
            HandleEnd(context, interrupted);
        }

        /// <summary>Override to run custom logic when the action starts.</summary>
        protected virtual void HandleStart(CharacterContext context) { }

        /// <summary>Override to run custom per-tick logic while the action is active.</summary>
        protected virtual void HandleTick(CharacterContext context, float deltaTime) { }

        /// <summary>Override to run cleanup logic when the action finishes or is interrupted.</summary>
        protected virtual void HandleEnd(CharacterContext context, bool interrupted) { }

        /// <summary>Marks this action as completed so the runner ends it on the current tick.</summary>
        protected void Complete()
        {
            IsFinished = true;
        }

        /// <summary>Resets the action's cooldown to ready immediately.</summary>
        public void ResetCooldown()
        {
            _cooldown.Reset();
        }
    }

    /// <summary>
    /// Groundedness precondition for data-driven actions.
    /// </summary>
    public enum ActionAirRule
    {
        Any = 0,
        GroundedOnly = 1,
        AirborneOnly = 2
    }

    /// <summary>
    /// Data-driven <see cref="CharacterActionBase"/> supporting air-state rules, optional velocity clearing,
    /// and facing-aligned launch impulses for code-free authoring via <c>CharacterActionAsset</c> or C#.
    /// </summary>
    public sealed class SimpleCharacterAction : CharacterActionBase
    {
        private readonly Action<CharacterContext> _onStart;
        private readonly Action<CharacterContext, bool> _onEnd;

        public ActionAirRule AirRule { get; }
        public bool CancelVelocityOnStart { get; }
        public Vector3 LaunchImpulse { get; }
        public bool FlipImpulseWithFacing { get; }

        public SimpleCharacterAction(
            string id,
            float duration = 0f,
            float cooldown = 0f,
            int priority = 0,
            ActionLockFlags lockFlags = ActionLockFlags.None,
            ActionAirRule airRule = ActionAirRule.Any,
            bool cancelVelocityOnStart = false,
            Vector3 launchImpulse = default,
            bool flipImpulseWithFacing = true,
            Action<CharacterContext> onStart = null,
            Action<CharacterContext, bool> onEnd = null)
            : base(id, duration, cooldown, priority, lockFlags)
        {
            AirRule = airRule;
            CancelVelocityOnStart = cancelVelocityOnStart;
            LaunchImpulse = launchImpulse;
            FlipImpulseWithFacing = flipImpulseWithFacing;
            _onStart = onStart;
            _onEnd = onEnd;
        }

        public override bool CanStart(CharacterContext context)
        {
            if (!base.CanStart(context))
            {
                return false;
            }

            if (AirRule == ActionAirRule.GroundedOnly && !context.Motor.IsGrounded)
            {
                return false;
            }

            if (AirRule == ActionAirRule.AirborneOnly && context.Motor.IsGrounded)
            {
                return false;
            }

            return true;
        }

        protected override void HandleStart(CharacterContext context)
        {
            if (CancelVelocityOnStart)
            {
                context.Motor.CancelDash();
                context.Motor.SetVelocity(Vector3.Zero);
            }

            if (LaunchImpulse.LengthSquared() > 0f)
            {
                Vector3 impulse = ApplyFacingToImpulse(LaunchImpulse, context);
                context.Motor.ApplyImpulse(impulse);
            }

            _onStart?.Invoke(context);
        }

        protected override void HandleEnd(CharacterContext context, bool interrupted)
        {
            _onEnd?.Invoke(context, interrupted);
        }

        private Vector3 ApplyFacingToImpulse(Vector3 impulse, CharacterContext context)
        {
            if (!FlipImpulseWithFacing)
            {
                return impulse;
            }

            Vector3 facing = context.Motor.FacingDirection;
            switch (context.Config.Mode)
            {
                case MovementMode.SideScroller2D:
                    if (facing.X < 0f)
                    {
                        impulse.X = -impulse.X;
                    }
                    return impulse;

                case MovementMode.TopDown2D:
                    float len2 = (float)Math.Sqrt(facing.X * facing.X + facing.Y * facing.Y);
                    if (len2 > 0.0001f)
                    {
                        float fx = facing.X / len2;
                        float fy = facing.Y / len2;
                        float rx = fx * impulse.X - fy * impulse.Y;
                        float ry = fy * impulse.X + fx * impulse.Y;
                        return new Vector3(rx, ry, impulse.Z);
                    }
                    return impulse;

                case MovementMode.ThirdPerson3D:
                default:
                    float len3 = (float)Math.Sqrt(facing.X * facing.X + facing.Z * facing.Z);
                    if (len3 > 0.0001f)
                    {
                        float fx = facing.X / len3;
                        float fz = facing.Z / len3;
                        // Forward basis +Z -> (fx, fz), Right basis +X -> (fz, -fx)
                        float rx = fz * impulse.X + fx * impulse.Z;
                        float rz = -fx * impulse.X + fz * impulse.Z;
                        return new Vector3(rx, impulse.Y, rz);
                    }
                    return impulse;
            }
        }
    }

    /// <summary>
    /// Delegate-backed <see cref="CharacterActionBase"/> for quick prototyping and unit tests.
    /// </summary>
    public sealed class DelegateCharacterAction : CharacterActionBase
    {
        private readonly Func<CharacterContext, bool> _canStart;
        private readonly Action<CharacterContext> _onStart;
        private readonly Action<CharacterContext, float> _onTick;
        private readonly Action<CharacterContext, bool> _onEnd;

        public DelegateCharacterAction(
            string id,
            float duration = 0f,
            float cooldown = 0f,
            int priority = 0,
            ActionLockFlags lockFlags = ActionLockFlags.None,
            Func<CharacterContext, bool> canStart = null,
            Action<CharacterContext> onStart = null,
            Action<CharacterContext, float> onTick = null,
            Action<CharacterContext, bool> onEnd = null)
            : base(id, duration, cooldown, priority, lockFlags)
        {
            _canStart = canStart;
            _onStart = onStart;
            _onTick = onTick;
            _onEnd = onEnd;
        }

        public override bool CanStart(CharacterContext context)
        {
            if (!base.CanStart(context))
            {
                return false;
            }

            return _canStart == null || _canStart(context);
        }

        protected override void HandleStart(CharacterContext context) => _onStart?.Invoke(context);
        protected override void HandleTick(CharacterContext context, float deltaTime) => _onTick?.Invoke(context, deltaTime);
        protected override void HandleEnd(CharacterContext context, bool interrupted) => _onEnd?.Invoke(context, interrupted);
    }

    /// <summary>
    /// Manages registered <see cref="ICharacterAction"/> instances, active action execution,
    /// priority interruption, cooldown ticking, and buffered action triggers.
    /// </summary>
    public sealed class CharacterActionRunner
    {
        private readonly Dictionary<string, ICharacterAction> _actions = new Dictionary<string, ICharacterAction>(StringComparer.Ordinal);
        private readonly List<ICharacterAction> _actionList = new List<ICharacterAction>(8);
        private readonly InputBuffer _buffer = new InputBuffer();
        private readonly List<string> _bufferedOrder = new List<string>(4);
        private readonly IEventBus _eventBus;

        /// <summary>Currently executing action, or null if idle.</summary>
        public ICharacterAction ActiveAction { get; private set; }

        /// <summary>True if an action is currently executing.</summary>
        public bool IsActionActive => ActiveAction != null;

        /// <summary>Lock flags imposed by the currently active action, or <see cref="ActionLockFlags.None"/>.</summary>
        public ActionLockFlags ActiveLockFlags => ActiveAction != null ? ActiveAction.LockFlags : ActionLockFlags.None;

        /// <summary>Number of registered actions.</summary>
        public int RegisteredCount => _actions.Count;

        /// <summary>Fired whenever an action starts executing.</summary>
        public event Action<CharacterActionStartedEvent> OnActionStarted;

        /// <summary>Fired whenever an action finishes naturally or is interrupted.</summary>
        public event Action<CharacterActionEndedEvent> OnActionEnded;

        public CharacterActionRunner(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// Registers an action by its <see cref="ICharacterAction.Id"/>. Throws if an action with the same ID already exists.
        /// </summary>
        public void Register(ICharacterAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (string.IsNullOrEmpty(action.Id))
            {
                throw new ArgumentException("Action ID must not be null or empty.", nameof(action));
            }

            if (_actions.ContainsKey(action.Id))
            {
                throw new InvalidOperationException($"An action with ID '{action.Id}' is already registered.");
            }

            _actions.Add(action.Id, action);
            _actionList.Add(action);
        }

        /// <summary>
        /// Unregisters an action by ID. Returns true if found and removed.
        /// </summary>
        public bool Unregister(string actionId)
        {
            if (string.IsNullOrEmpty(actionId) || !_actions.TryGetValue(actionId, out ICharacterAction action))
            {
                return false;
            }

            _actions.Remove(actionId);
            _actionList.Remove(action);
            _buffer.Consume(actionId);
            _bufferedOrder.Remove(actionId);
            return true;
        }

        /// <summary>
        /// Retrieves a registered action by ID, or returns false if not found.
        /// </summary>
        public bool TryGetAction(string actionId, out ICharacterAction action)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                action = null;
                return false;
            }

            return _actions.TryGetValue(actionId, out action);
        }

        /// <summary>
        /// Attempts to start <paramref name="actionId"/> immediately; if it cannot start now and
        /// <paramref name="bufferWindow"/> &gt; 0, buffers the request to retry on subsequent ticks.
        /// </summary>
        public bool TryStart(string actionId, CharacterContext context, float bufferWindow = 0f)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(actionId) || !_actions.TryGetValue(actionId, out ICharacterAction candidate))
            {
                return false;
            }

            if (TryStartInternal(candidate, context))
            {
                _buffer.Consume(actionId);
                _bufferedOrder.Remove(actionId);
                return true;
            }

            if (bufferWindow > 0f)
            {
                _buffer.Buffer(actionId, bufferWindow);
                if (!_bufferedOrder.Contains(actionId))
                {
                    _bufferedOrder.Add(actionId);
                }
            }

            return false;
        }

        /// <summary>
        /// Buffers an action request to be attempted on the next <see cref="Tick"/>.
        /// </summary>
        public void RequestAction(string actionId, float bufferWindow = 0.12f)
        {
            if (string.IsNullOrEmpty(actionId) || !_actions.ContainsKey(actionId))
            {
                return;
            }

            float window = bufferWindow > 0f ? bufferWindow : 0.001f;
            _buffer.Buffer(actionId, window);
            if (!_bufferedOrder.Contains(actionId))
            {
                _bufferedOrder.Add(actionId);
            }
        }

        /// <summary>
        /// Cancels the currently active action, invoking <see cref="ICharacterAction.OnEnd"/> with <c>interrupted: true</c>.
        /// </summary>
        public bool CancelActive(CharacterContext context)
        {
            if (ActiveAction == null)
            {
                return false;
            }

            EndActive(context, interrupted: true);
            return true;
        }

        /// <summary>
        /// Advances all action cooldowns, ticks the active action, and evaluates any buffered action requests.
        /// </summary>
        public void Tick(CharacterContext context, float deltaTime)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            // 1. Tick cooldowns on all registered actions
            for (int i = 0; i < _actionList.Count; i++)
            {
                _actionList[i].TickCooldown(deltaTime);
            }

            // 2. Tick active action and end if finished
            if (ActiveAction != null)
            {
                ActiveAction.OnTick(context, deltaTime);
                if (ActiveAction != null && ActiveAction.IsFinished)
                {
                    EndActive(context, interrupted: false);
                }
            }

            // 3. Try buffered action requests before decrementing their windows
            TryConsumeBuffered(context);

            // 4. Advance input buffer timers and prune expired entries
            _buffer.Tick(deltaTime);
            for (int i = _bufferedOrder.Count - 1; i >= 0; i--)
            {
                if (!_buffer.HasBuffered(_bufferedOrder[i]))
                {
                    _bufferedOrder.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Clears all buffered action requests.
        /// </summary>
        public void ClearBuffer()
        {
            _buffer.Clear();
            _bufferedOrder.Clear();
        }

        private void TryConsumeBuffered(CharacterContext context)
        {
            for (int i = 0; i < _bufferedOrder.Count; i++)
            {
                string id = _bufferedOrder[i];
                if (!_buffer.HasBuffered(id))
                {
                    continue;
                }

                if (_actions.TryGetValue(id, out ICharacterAction candidate) && TryStartInternal(candidate, context))
                {
                    _buffer.Consume(id);
                    _bufferedOrder.RemoveAt(i);
                    return;
                }
            }
        }

        private bool TryStartInternal(ICharacterAction candidate, CharacterContext context)
        {
            if (!candidate.CanStart(context))
            {
                return false;
            }

            if (ActiveAction != null)
            {
                if (!ActiveAction.CanBeInterruptedBy(candidate))
                {
                    return false;
                }

                EndActive(context, interrupted: true);
            }

            ActiveAction = candidate;
            candidate.OnStart(context);

            var startedEvent = new CharacterActionStartedEvent(candidate.Id, candidate);
            OnActionStarted?.Invoke(startedEvent);
            _eventBus?.Publish(in startedEvent);

            if (candidate.IsFinished)
            {
                EndActive(context, interrupted: false);
            }

            return true;
        }

        private void EndActive(CharacterContext context, bool interrupted)
        {
            ICharacterAction finished = ActiveAction;
            ActiveAction = null;
            finished.OnEnd(context, interrupted);

            var endedEvent = new CharacterActionEndedEvent(finished.Id, finished, interrupted);
            OnActionEnded?.Invoke(endedEvent);
            _eventBus?.Publish(in endedEvent);
        }
    }
}
