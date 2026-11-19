using System.Collections.Generic;
using NUnit.Framework;

namespace ContentContent.Dialog.Tests
{
    public class DialogSystemTests
    {
        private sealed class RecordingView : IDialogView
        {
            public bool Started;
            public bool Ended;
            public readonly List<DialogLineViewModel> Lines = new List<DialogLineViewModel>();
            public IReadOnlyList<DialogChoiceViewModel> LastChoices;

            public void OnDialogStarted(DialogGraph graph) => Started = true;
            public void ShowLine(DialogLineViewModel line) => Lines.Add(line);
            public void ShowChoices(IReadOnlyList<DialogChoiceViewModel> choices) => LastChoices = choices;
            public void OnDialogEnded(DialogGraph graph) => Ended = true;
        }

        private static DialogGraph BuildGuardGraph()
        {
            var defaults = new Dictionary<string, DialogValue>
            {
                { "gold", DialogValue.FromInt(60) },
                { "gate_open", DialogValue.FromBool(false) }
            };

            var nodes = new[]
            {
                new DialogNode(
                    id: "guard_intro",
                    speaker: "Guard",
                    lineKey: "DLG_GUARD_INTRO",
                    choices: new[]
                    {
                        new DialogChoice(
                            textKey: "OPT_BRIBE",
                            targetNodeId: "bribed",
                            conditions: new[] { new DialogCondition("gold", DialogComparisonOperator.GreaterOrEqual, DialogValue.FromInt(50)) },
                            mutations: new[] { new DialogMutation("gold", DialogMutationOperator.Subtract, DialogValue.FromInt(50)) },
                            commands: new[] { new DialogCommand("play_sfx", "coins") }),
                        new DialogChoice(
                            textKey: "OPT_LEAVE",
                            targetNodeId: "leave")
                    }),
                new DialogNode(
                    id: "bribed",
                    speaker: "Guard",
                    lineKey: "DLG_GUARD_BRIBED",
                    isEnd: true,
                    onEnterMutations: new[] { new DialogMutation("gate_open", DialogMutationOperator.Set, DialogValue.FromBool(true)) },
                    onEnterCommands: new[] { new DialogCommand("open_gate") }),
                new DialogNode(
                    id: "leave",
                    speaker: "Guard",
                    lineKey: "DLG_GUARD_LEAVE",
                    isEnd: true)
            };

            return new DialogGraph("guard_scene", "guard_intro", nodes, defaults);
        }

        [Test]
        public void BranchingChoice_AppliesTransitionMutationAndCommands()
        {
            var strings = new Dictionary<string, string>
            {
                { "Guard", "Gate Guard" },
                { "DLG_GUARD_INTRO", "You have {gold} gold. None shall pass." },
                { "OPT_BRIBE", "Offer 50 gold" },
                { "OPT_LEAVE", "Leave" },
                { "DLG_GUARD_BRIBED", "Fine. You have {gold} gold left." },
                { "DLG_GUARD_LEAVE", "Move along." }
            };

            var view = new RecordingView();
            var runner = new DialogRunner(localizer: new DictionaryLocalizer(strings), view: view);
            var commands = new List<string>();
            runner.RegisterCommandHandler("play_sfx", c => commands.Add($"{c.Name}:{c.Arguments[0]}"));
            runner.RegisterCommandHandler("open_gate", c => commands.Add(c.Name));

            DialogGraph graph = BuildGuardGraph();
            Assert.IsEmpty(graph.Validate());

            runner.Start(graph);

            Assert.IsTrue(view.Started);
            Assert.AreEqual(1, view.Lines.Count);
            Assert.AreEqual("Gate Guard", view.Lines[0].Speaker);
            Assert.AreEqual("You have 60 gold. None shall pass.", view.Lines[0].Text);
            Assert.AreEqual(2, view.LastChoices.Count);

            runner.SelectChoice(0);

            Assert.AreEqual(10, runner.Variables.GetInt("gold"));
            Assert.IsTrue(runner.Variables.GetBool("gate_open"));
            Assert.AreEqual("Fine. You have 10 gold left.", view.Lines[1].Text);
            CollectionAssert.AreEqual(new[] { "play_sfx:coins", "open_gate" }, commands);

            runner.Continue();
            Assert.IsTrue(view.Ended);
            Assert.IsFalse(runner.IsRunning);
        }

        [Test]
        public void ChoiceCondition_HidesUnavailableChoice()
        {
            var store = new DialogVariableStore();
            store.SetInt("gold", 20);

            var view = new RecordingView();
            var runner = new DialogRunner(variables: store, view: view);
            runner.Start(BuildGuardGraph());

            Assert.AreEqual(1, view.LastChoices.Count);
            Assert.AreEqual("OPT_LEAVE", view.LastChoices[0].Text);
        }

        [Test]
        public void Localizer_FallsBackAndMarksMissingKeys()
        {
            var primary = new Dictionary<string, string> { { "A", "Alpha" } };
            var fallback = new Dictionary<string, string> { { "B", "Beta" } };
            var localizer = new DictionaryLocalizer(primary, fallback);

            Assert.AreEqual("Alpha", localizer.Localize("A"));
            Assert.AreEqual("Beta", localizer.Localize("B"));
            Assert.AreEqual("<MISSING:C>", localizer.Localize("C"));
        }

        [Test]
        public void Validate_DetectsBrokenTargetsDeadEndsAndUnreachableNodes()
        {
            var graph = new DialogGraph("broken", "start", new[]
            {
                new DialogNode("start", lineKey: "hi", nextNodeId: "missing_node"),
                new DialogNode("orphan", lineKey: "stuck")
            });

            List<DialogValidationIssue> issues = graph.Validate();
            Assert.IsNotEmpty(issues);
            Assert.IsTrue(issues.Exists(i => i.Message.Contains("missing_node")));
            Assert.IsTrue(issues.Exists(i => i.NodeId == "orphan" && i.Message.Contains("Dead end")));
            Assert.IsTrue(issues.Exists(i => i.NodeId == "orphan" && i.Message.Contains("unreachable")));
        }

        [Test]
        public void VariableStore_SnapshotAndRestore_RoundTrips()
        {
            var store = new DialogVariableStore();
            store.SetInt("gold", 42);
            store.SetBool("met_guard", true);
            store.SetString("mood", "calm");

            var snap = store.Snapshot();
            store.Clear();
            Assert.IsFalse(store.Has("gold"));

            store.Restore(snap);
            Assert.AreEqual(42, store.GetInt("gold"));
            Assert.IsTrue(store.GetBool("met_guard"));
            Assert.AreEqual("calm", store.GetString("mood"));
        }
    }
}
