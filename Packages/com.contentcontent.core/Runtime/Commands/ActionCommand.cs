using System;

namespace ContentContent.Core.Commands
{
    /// <summary>
    /// Lightweight delegate-backed <see cref="ICommand"/> for simple reversible actions.
    /// </summary>
    public sealed class ActionCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public ActionCommand(Action execute, Action undo)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            if (undo == null) throw new ArgumentNullException(nameof(undo));

            _execute = execute;
            _undo = undo;
        }

        public void Execute() => _execute();
        public void Undo() => _undo();
    }
}
