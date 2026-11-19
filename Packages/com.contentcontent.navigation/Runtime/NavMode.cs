namespace ContentContent.Navigation
{
    /// <summary>
    /// Dimensionality and plane orientation of a <see cref="NavGrid"/>.
    /// </summary>
    public enum NavMode
    {
        /// <summary>2D navigation on the XY plane (Depth = 1).</summary>
        PlanarXY = 0,

        /// <summary>2D ground navigation on the XZ plane (Height = 1).</summary>
        PlanarXZ = 1,

        /// <summary>Full volumetric 3D navigation across X, Y, and Z.</summary>
        Volumetric3D = 2
    }
}
