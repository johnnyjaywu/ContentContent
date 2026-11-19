using System;
using System.Collections.Generic;
using ContentContent.Core.Commands;
using NUnit.Framework;

namespace ContentContent.Core.Tests
{
    public class CommandsTests
    {
        private sealed class Counter
        {
            public int Value;
        }

        private sealed class AddCommand : IMergeableCommand
        {
            private readonly Counter _counter;
            public int Delta { get; private set; }
            public int GroupId { get; }

            public AddCommand(Counter counter, int delta, int groupId = 0)
            {
                _counter = counter;
                Delta = delta;
                GroupId = groupId;
            }

            public void Execute() => _counter.Value += Delta;
            public void Undo() => _counter.Value -= Delta;

            public bool TryMerge(ICommand next)
            {
                if (next is AddCommand other && other._counter == _counter && other.GroupId == GroupId && GroupId != 0)
                {
                    Delta += other.Delta;
                    return true;
                }

                return false;
            }
        }

        [Test]
        public void ExecuteUndoRedo_AndBoundedEviction()
        {
            var counter = new Counter();
            var history = new CommandHistory(maxCapacity: 2);

            history.Execute(new ActionCommand(() => counter.Value += 1, () => counter.Value -= 1));
            history.Execute(new ActionCommand(() => counter.Value += 10, () => counter.Value -= 10));
            history.Execute(new ActionCommand(() => counter.Value += 100, () => counter.Value -= 100));

            Assert.AreEqual(111, counter.Value);
            Assert.AreEqual(2, history.UndoCount);

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(11, counter.Value);
            Assert.IsTrue(history.Undo());
            Assert.AreEqual(1, counter.Value);
            Assert.IsFalse(history.Undo()); // First command was evicted

            Assert.IsTrue(history.Redo());
            Assert.AreEqual(11, counter.Value);
            Assert.AreEqual(1, history.RedoCount);

            // Executing a new command clears redo stack
            history.Execute(new ActionCommand(() => counter.Value += 5, () => counter.Value -= 5));
            Assert.AreEqual(0, history.RedoCount);
            Assert.AreEqual(16, counter.Value);
        }

        [Test]
        public void MergeableCommands_CoalesceIntoSingleUndoStep()
        {
            var counter = new Counter();
            var history = new CommandHistory();

            history.Execute(new AddCommand(counter, 2, groupId: 1));
            history.Execute(new AddCommand(counter, 3, groupId: 1));
            history.Execute(new AddCommand(counter, 5, groupId: 1));

            Assert.AreEqual(10, counter.Value);
            Assert.AreEqual(1, history.UndoCount);

            history.Undo();
            Assert.AreEqual(0, counter.Value);

            history.Redo();
            Assert.AreEqual(10, counter.Value);
        }

        [Test]
        public void Transaction_CommitsAsSingleStep_AndSupportsRollback()
        {
            var counter = new Counter();
            var history = new CommandHistory();

            using (history.BeginTransaction())
            {
                history.Execute(new AddCommand(counter, 4));
                history.Execute(new AddCommand(counter, 6));
            }

            Assert.AreEqual(10, counter.Value);
            Assert.AreEqual(1, history.UndoCount);

            history.Undo();
            Assert.AreEqual(0, counter.Value);

            using (var tx = history.BeginTransaction())
            {
                history.Execute(new AddCommand(counter, 50));
                Assert.AreEqual(50, counter.Value);
                tx.Rollback();
            }

            Assert.AreEqual(0, counter.Value);
            Assert.AreEqual(0, history.UndoCount);
        }

        [Test]
        public void CommandQueue_ExecutesFifo_AndForwardsToHistory()
        {
            var log = new List<string>();
            var history = new CommandHistory();
            var queue = new CommandQueue(history);

            queue.Enqueue(new ActionCommand(() => log.Add("A"), () => log.Add("-A")));
            queue.Enqueue(new ActionCommand(() => log.Add("B"), () => log.Add("-B")));

            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(2, queue.ExecuteAll());
            Assert.IsTrue(queue.IsEmpty);
            CollectionAssert.AreEqual(new[] { "A", "B" }, log);

            history.Undo();
            CollectionAssert.AreEqual(new[] { "A", "B", "-B" }, log);
        }
    }
}
