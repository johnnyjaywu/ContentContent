using System;
using System.Collections.Generic;

namespace ContentContent.Core.Commands
{
    /// <summary>
    /// Groups multiple <see cref="ICommand"/> instances into one atomic step:
    /// executes forward in order and undoes in reverse order.
    /// </summary>
    public sealed class CompositeCommand : ICommand
    {
        private readonly List<ICommand> _commands;

        public int Count => _commands.Count;

        public CompositeCommand(IEnumerable<ICommand> commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            _commands = new List<ICommand>();
            foreach (var cmd in commands)
            {
                if (cmd == null)
                    throw new ArgumentException("CompositeCommand cannot contain null commands.", nameof(commands));
                _commands.Add(cmd);
            }
        }

        public void Execute()
        {
            for (int i = 0; i < _commands.Count; i++)
            {
                _commands[i].Execute();
            }
        }

        public void Undo()
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }
    }
}
