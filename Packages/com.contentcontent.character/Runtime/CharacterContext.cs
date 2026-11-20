using System;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Events;

namespace ContentContent.Character
{
    /// <summary>
    /// Shared context exposed to locomotion HFSM states (<see cref="ContentContent.Core.Fsm.StateBase{TContext}"/>)
    /// and extensible character actions (<see cref="ICharacterAction"/>).
    /// </summary>
    public sealed class CharacterContext
    {
        /// <summary>Active character configuration.</summary>
        public CharacterConfig Config { get; }

        /// <summary>Underlying kinematic motor.</summary>
        public CharacterMotor Motor { get; }

        /// <summary>Action runner managing cooldown-gated actions and movement locks.</summary>
        public CharacterActionRunner Actions { get; }

        /// <summary>Shared Core <see cref="Blackboard"/> synchronized with character runtime values.</summary>
        public Blackboard Blackboard { get; }

        /// <summary>Optional Core <see cref="IEventBus"/> for publishing/subscribing to struct events.</summary>
        public IEventBus EventBus { get; }

        /// <summary>Latest per-tick input snapshot.</summary>
        public CharacterInput CurrentInput { get; internal set; }

        /// <summary>Latest per-tick environment sensor snapshot.</summary>
        public MotorSensors CurrentSensors { get; internal set; }

        public CharacterContext(
            CharacterConfig config,
            CharacterMotor motor,
            CharacterActionRunner actions,
            Blackboard blackboard,
            IEventBus eventBus = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Motor = motor ?? throw new ArgumentNullException(nameof(motor));
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            EventBus = eventBus;
        }
    }
}
