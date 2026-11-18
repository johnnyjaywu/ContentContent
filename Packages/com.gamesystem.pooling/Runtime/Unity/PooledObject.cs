using System;
using UnityEngine;

namespace GameSystem.Pooling.Unity
{
    /// <summary>
    /// Optional component attached to pooled GameObjects allowing them to release themselves
    /// back to their owning pool manually or after a timed delay.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        private Action<GameObject> _releaseAction;
        private float _remainingLifetime;
        private bool _hasLifetime;

        internal void Bind(Action<GameObject> releaseAction)
        {
            _releaseAction = releaseAction;
        }

        /// <summary>
        /// Schedules this object to automatically return to its pool after <paramref name="seconds"/>.
        /// </summary>
        public void DespawnAfter(float seconds)
        {
            if (seconds <= 0f)
            {
                ReleaseToPool();
                return;
            }

            _remainingLifetime = seconds;
            _hasLifetime = true;
        }

        /// <summary>
        /// Cancels any scheduled timed despawn.
        /// </summary>
        public void CancelTimedDespawn()
        {
            _hasLifetime = false;
            _remainingLifetime = 0f;
        }

        /// <summary>
        /// Returns this GameObject to its owning pool, or destroys it if unbound.
        /// </summary>
        public void ReleaseToPool()
        {
            _hasLifetime = false;
            if (_releaseAction != null)
            {
                _releaseAction(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!_hasLifetime)
                return;

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                ReleaseToPool();
            }
        }
    }
}
