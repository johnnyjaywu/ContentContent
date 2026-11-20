using UnityEngine;
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Conversion helpers between <see cref="UnityEngine.Vector3"/>/<see cref="UnityEngine.Vector2"/>
    /// and <see cref="System.Numerics.Vector3"/>.
    /// </summary>
    public static class CharacterVectorExtensions
    {
        public static NumVector3 ToNumerics(this Vector3 v) => new NumVector3(v.x, v.y, v.z);

        public static NumVector3 ToNumerics(this Vector2 v) => new NumVector3(v.x, v.y, 0f);

        public static Vector3 ToUnity(this NumVector3 v) => new Vector3(v.X, v.Y, v.Z);

        public static Vector2 ToUnity2D(this NumVector3 v) => new Vector2(v.X, v.Y);
    }
}
