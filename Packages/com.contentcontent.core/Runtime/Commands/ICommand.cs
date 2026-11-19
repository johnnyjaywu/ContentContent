namespace ContentContent.Core.Commands
{
    /// <summary>
    /// Encapsulates a reversible action that can be executed and undone.
    /// </summary>
    public interface ICommand
    {
        /// <summary>Applies the command's effect.</summary>
        void Execute();

        /// <summary>Reverts the command's effect.</summary>
        void Undo();
    }
}
