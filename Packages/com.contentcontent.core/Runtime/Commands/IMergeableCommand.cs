namespace ContentContent.Core.Commands
{
    /// <summary>
    /// Optional interface for commands that can coalesce consecutive edits (e.g., slider drags or brush strokes)
    /// into a single undo/redo step inside <see cref="CommandHistory"/>.
    /// </summary>
    public interface IMergeableCommand : ICommand
    {
        /// <summary>
        /// Attempts to absorb <paramref name="next"/> (which has already been executed) into this command.
        /// Return true if merged; false to record <paramref name="next"/> as a separate history entry.
        /// </summary>
        bool TryMerge(ICommand next);
    }
}
