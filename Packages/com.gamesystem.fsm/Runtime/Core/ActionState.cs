using System;

namespace GameSystem.Fsm
{
    /// <summary>
    /// Lightweight delegate-based state for quick state definitions without creating a dedicated subclass.
    /// </summary>
    public sealed class ActionState : IState
    {
        private readonly Action _onEnter;
        private readonly Action<float> _onTick;
        private readonly Action<float> _onFixedTick;
        private readonly Action _onExit;

        public string Name { get; }

        public ActionState(
            string name = null,
            Action onEnter = null,
            Action<float> onTick = null,
            Action<float> onFixedTick = null,
            Action onExit = null)
        {
            Name = string.IsNullOrEmpty(name) ? nameof(ActionState) : name;
            _onEnter = onEnter;
            _onTick = onTick;
            _onFixedTick = onFixedTick;
            _onExit = onExit;
        }

        public void OnEnter() => _onEnter?.Invoke();
        public void OnTick(float deltaTime) => _onTick?.Invoke(deltaTime);
        public void OnFixedTick(float fixedDeltaTime) => _onFixedTick?.Invoke(fixedDeltaTime);
        public void OnExit() => _onExit?.Invoke();

        public override string ToString() => Name;
    }
}
