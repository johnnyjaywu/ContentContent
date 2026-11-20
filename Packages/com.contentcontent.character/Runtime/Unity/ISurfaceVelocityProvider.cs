#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Implemented by moving platforms or conveyor surfaces so <see cref="CharacterMotor2D"/>
    /// can inherit surface velocity cleanly without double-adding momentum.
    /// </summary>
    public interface ISurfaceVelocityProvider
    {
        Vector2 SurfaceVelocity { get; }
    }
}
#endif
