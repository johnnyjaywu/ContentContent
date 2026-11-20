namespace ContentContent.World
{
    /// <summary>
    /// Implemented by room-scoped entities (enemies, breakable switches, crumble platforms)
    /// that reset deterministically when re-entering a room or respawning at a checkpoint.
    /// </summary>
    public interface IResettable
    {
        void ResetState();
    }
}
