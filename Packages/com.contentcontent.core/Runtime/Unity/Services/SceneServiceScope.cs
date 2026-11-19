using UnityEngine;

namespace ContentContent.Core.Services.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter that owns a <see cref="ServiceContainer"/> scope tied to a
    /// GameObject or scene lifecycle, optionally inheriting from a parent <see cref="SceneServiceScope"/>.
    /// </summary>
    public class SceneServiceScope : MonoBehaviour
    {
        [SerializeField] private SceneServiceScope _parentScope;

        private ServiceContainer _container;

        /// <summary>The underlying <see cref="ServiceContainer"/> (lazily initialized).</summary>
        public ServiceContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = _parentScope != null
                        ? _parentScope.Container.CreateScope()
                        : new ServiceContainer();
                }

                return _container;
            }
        }

        private void OnDestroy()
        {
            _container?.Dispose();
        }
    }
}
