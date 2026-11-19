using System;
using System.Collections.Generic;

namespace ContentContent.Core.Commands
{
    /// <summary>
    /// Bounded undo/redo command history with <see cref="IMergeableCommand"/> coalescing
    /// and atomic transaction grouping.
    /// </summary>
    public class CommandHistory
    {
        /// <summary>
        /// Disposable scope returned by <see cref="BeginTransaction"/>.
        /// Commits on <see cref="Dispose"/> unless <see cref="Rollback"/> was called.
        /// </summary>
        public struct TransactionScope : IDisposable
        {
            private CommandHistory _history;
            private bool _completed;

            internal TransactionScope(CommandHistory history)
            {
                _history = history;
                _completed = false;
            }

            /// <summary>Rolls back all commands executed in this transaction.</summary>
            public void Rollback()
            {
                if (_completed || _history == null)
                    return;

                _completed = true;
                _history.RollbackTransaction();
                _history = null;
            }

            /// <summary>Commits the transaction if it has not been rolled back.</summary>
            public void Dispose()
            {
                if (_completed || _history == null)
                    return;

                _completed = true;
                _history.CommitTransaction();
                _history = null;
            }
        }

        private readonly ICommand[] _undoRing;
        private int _undoHead;
        private int _undoCount;
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        private int _transactionDepth;
        private List<ICommand> _transactionCommands;

        /// <summary>Maximum number of undo steps retained before evicting the oldest entry.</summary>
        public int MaxCapacity => _undoRing.Length;

        /// <summary>Number of steps currently available to undo.</summary>
        public int UndoCount => _undoCount;

        /// <summary>Number of steps currently available to redo.</summary>
        public int RedoCount => _redoStack.Count;

        /// <summary>True if at least one command can be undone.</summary>
        public bool CanUndo => _undoCount > 0 && _transactionDepth == 0;

        /// <summary>True if at least one command can be redone.</summary>
        public bool CanRedo => _redoStack.Count > 0 && _transactionDepth == 0;

        /// <summary>True while one or more transactions are open.</summary>
        public bool IsInTransaction => _transactionDepth > 0;

        /// <summary>Raised whenever commands are executed, undone, redone, committed, or cleared.</summary>
        public event Action OnHistoryChanged;

        public CommandHistory(int maxCapacity = 100)
        {
            if (maxCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be greater than 0.");

            _undoRing = new ICommand[maxCapacity];
        }

        /// <summary>
        /// Executes <paramref name="command"/> and records it in history (or the active transaction).
        /// </summary>
        public void Execute(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Execute();
            _redoStack.Clear();

            if (_transactionDepth > 0)
            {
                _transactionCommands.Add(command);
                return;
            }

            if (_undoCount > 0)
            {
                ICommand top = PeekUndo();
                if (top is IMergeableCommand mergeable && mergeable.TryMerge(command))
                {
                    OnHistoryChanged?.Invoke();
                    return;
                }
            }

            PushUndo(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>Undoes the most recent command and pushes it onto the redo stack.</summary>
        public bool Undo()
        {
            if (_transactionDepth > 0)
                throw new InvalidOperationException("Cannot call Undo while a transaction is active.");

            if (_undoCount == 0)
                return false;

            ICommand command = PopUndo();
            command.Undo();
            _redoStack.Push(command);
            OnHistoryChanged?.Invoke();
            return true;
        }

        /// <summary>Re-executes the most recently undone command and pushes it back onto the undo history.</summary>
        public bool Redo()
        {
            if (_transactionDepth > 0)
                throw new InvalidOperationException("Cannot call Redo while a transaction is active.");

            if (_redoStack.Count == 0)
                return false;

            ICommand command = _redoStack.Pop();
            command.Execute();
            PushUndo(command);
            OnHistoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Begins a transaction that groups all commands executed until disposal into one atomic undo step.
        /// Nested transactions flatten into the outermost transaction.
        /// </summary>
        public TransactionScope BeginTransaction()
        {
            if (_transactionDepth == 0)
            {
                if (_transactionCommands == null)
                {
                    _transactionCommands = new List<ICommand>();
                }
                else
                {
                    _transactionCommands.Clear();
                }
            }

            _transactionDepth++;
            return new TransactionScope(this);
        }

        /// <summary>Clears all undo and redo history.</summary>
        public void Clear()
        {
            if (_transactionDepth > 0)
                throw new InvalidOperationException("Cannot clear CommandHistory while a transaction is active.");

            Array.Clear(_undoRing, 0, _undoRing.Length);
            _undoHead = 0;
            _undoCount = 0;
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }

        private void CommitTransaction()
        {
            if (_transactionDepth <= 0)
                return;

            _transactionDepth--;
            if (_transactionDepth > 0)
                return;

            if (_transactionCommands.Count == 0)
                return;

            ICommand entry = _transactionCommands.Count == 1
                ? _transactionCommands[0]
                : new CompositeCommand(_transactionCommands);

            _transactionCommands.Clear();
            PushUndo(entry);
            OnHistoryChanged?.Invoke();
        }

        private void RollbackTransaction()
        {
            if (_transactionDepth <= 0)
                return;

            _transactionDepth--;
            if (_transactionDepth > 0)
                return;

            for (int i = _transactionCommands.Count - 1; i >= 0; i--)
            {
                _transactionCommands[i].Undo();
            }

            _transactionCommands.Clear();
        }

        private void PushUndo(ICommand command)
        {
            if (_undoCount < _undoRing.Length)
            {
                int index = (_undoHead + _undoCount) % _undoRing.Length;
                _undoRing[index] = command;
                _undoCount++;
            }
            else
            {
                // Overwrite oldest entry at _undoHead and advance head
                _undoRing[_undoHead] = command;
                _undoHead = (_undoHead + 1) % _undoRing.Length;
            }
        }

        private ICommand PeekUndo()
        {
            int topIndex = (_undoHead + _undoCount - 1) % _undoRing.Length;
            return _undoRing[topIndex];
        }

        private ICommand PopUndo()
        {
            int topIndex = (_undoHead + _undoCount - 1) % _undoRing.Length;
            ICommand cmd = _undoRing[topIndex];
            _undoRing[topIndex] = null;
            _undoCount--;
            return cmd;
        }
    }
}
