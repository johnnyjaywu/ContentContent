using System;
using UnityEngine;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Reusable <see cref="ScriptableObject"/> defining a cooldown-gated character action
    /// (such as an attack, roll, parry, or dive) without writing custom C# boilerplate.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterAction",
        menuName = "ContentContent/Character/Character Action",
        order = 1)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    public class CharacterActionAsset : ScriptableObject
    {
        [Tooltip("Unique identifier passed to TriggerAction or matched in CharacterInput2D/3D.")]
        [SerializeField] private string _actionId = "Attack";

        [Tooltip("How long this action stays active in seconds (0 = instant).")]
        [SerializeField] private float _duration = 0.25f;

        [Tooltip("Minimum delay in seconds before this action can start again.")]
        [SerializeField] private float _cooldown = 0.4f;

        [Tooltip("Higher priority actions can interrupt currently running lower-priority actions.")]
        [SerializeField] private int _priority = 1;

        [Tooltip("Movement and input capabilities locked while this action is executing.")]
        [SerializeField] private ActionLockFlags _lockFlags = ActionLockFlags.BlockMovement | ActionLockFlags.BlockTurn | ActionLockFlags.BlockJump;

        [Tooltip("Whether this action can start on the ground, in mid-air, or anywhere.")]
        [SerializeField] private ActionAirRule _airRule = ActionAirRule.Any;

        [Tooltip("If enabled, stops existing movement velocity and cancels active dashes when the action starts.")]
        [SerializeField] private bool _cancelVelocityOnStart;

        [Tooltip("Optional impulse applied when the action starts (e.g., lunge forward or hop upward).")]
        [SerializeField] private Vector3 _launchImpulse;

        [Tooltip("Rotates or mirrors horizontal launch impulse to match the character's current facing direction.")]
        [SerializeField] private bool _flipImpulseWithFacing = true;

        public string ActionId => _actionId;
        public float Duration => _duration;
        public float Cooldown => _cooldown;
        public int Priority => _priority;
        public ActionLockFlags LockFlags => _lockFlags;
        public ActionAirRule AirRule => _airRule;
        public bool CancelVelocityOnStart => _cancelVelocityOnStart;
        public Vector3 LaunchImpulse => _launchImpulse;
        public bool FlipImpulseWithFacing => _flipImpulseWithFacing;

        private void OnValidate()
        {
            _duration = Mathf.Max(0f, _duration);
            _cooldown = Mathf.Max(0f, _cooldown);
            if (string.IsNullOrWhiteSpace(_actionId))
            {
                _actionId = name;
            }
        }

        /// <summary>
        /// Builds a pure-C# <see cref="SimpleCharacterAction"/> configured from this asset.
        /// </summary>
        public SimpleCharacterAction BuildRuntimeAction(
            Action<CharacterContext> onStart = null,
            Action<CharacterContext, bool> onEnd = null)
        {
            string id = string.IsNullOrWhiteSpace(_actionId) ? name : _actionId.Trim();
            return new SimpleCharacterAction(
                id: id,
                duration: _duration,
                cooldown: _cooldown,
                priority: _priority,
                lockFlags: _lockFlags,
                airRule: _airRule,
                cancelVelocityOnStart: _cancelVelocityOnStart,
                launchImpulse: _launchImpulse.ToNumerics(),
                flipImpulseWithFacing: _flipImpulseWithFacing,
                onStart: onStart,
                onEnd: onEnd);
        }
    }
}
