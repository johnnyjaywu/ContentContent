using System;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Pairs a <see cref="CharacterActionAsset"/> with Inspector <see cref="UnityEvent"/> callbacks
    /// so designers can trigger audio, VFX, hitboxes, or animations without code.
    /// </summary>
    [Serializable]
    public class CharacterActionBinding
    {
        [Tooltip("Action definition registered automatically when the motor initializes.")]
        [SerializeField] private CharacterActionAsset _action;

        [Tooltip("Invoked once when this action starts executing.")]
        [SerializeField] private UnityEvent _onStarted;

        [Tooltip("Invoked when this action ends (both natural completion and interruption, useful for disabling hitboxes).")]
        [SerializeField] private UnityEvent _onEnded;

        [Tooltip("Invoked additionally if this action was interrupted by a higher-priority action before finishing.")]
        [SerializeField] private UnityEvent _onInterrupted;

        public CharacterActionAsset Action
        {
            get => _action;
            set => _action = value;
        }

        public UnityEvent OnStarted => _onStarted;
        public UnityEvent OnEnded => _onEnded;
        public UnityEvent OnInterrupted => _onInterrupted;
    }
}
