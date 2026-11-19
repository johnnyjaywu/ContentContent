using System;
using System.Collections.Generic;

namespace ContentContent.Core.Commands
{
    /// <summary>
    /// FIFO queue for deferred or turn-based command execution, optionally forwarding
    /// executed commands into a <see cref="CommandHistory"/> for undo/redo support.
    /// </summary>
    public class CommandQueue
    {
        private readonly Queue<ICommand> _queue = new Queue<ICommand>();
        private readonly CommandHistory _history;

        /// <summary>Number of queued commands waiting to execute.</summary>
        public int Count => _queue.Count;

        /// <summary>True when there are no queued commands.</summary>
        public bool IsEmpty => _queue.Count == 0;

        public CommandQueue(CommandHistory history = null)
        {
            _history = history;
        }

        /// <summary>Enqueues <paramref name="command"/> for deferred execution.</summary>
        public void Enqueue(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _queue.Enqueue(command);
        }

        /// <summary>Dequeues and executes the next command. Returns false if the queue is empty.</summary>
        public bool ExecuteNext()
        {
            if (_queue.Count == 0)
                return false;

            ICommand command = _queue.Dequeue();
            if (_history != null)
            {
                _history.Execute(command);
            }
            else
            {
                command.Execute();
            }

            return true;
        }

        /// <summary>Executes all currently queued commands in FIFO order and returns the count executed.</summary>
        public int ExecuteAll()
        {
            int executed = 0;
            while (_queue.Count > 0)
            {
                ExecuteNext();
                executed++;
            }

            return executed;
        }

        /// <summary>Discards all pending commands without executing them.</summary>
        public void Clear()
        {
            _queue.Clear();
        }
    }
}
