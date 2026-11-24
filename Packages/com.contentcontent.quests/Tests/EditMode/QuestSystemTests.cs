using System.Collections.Generic;
using NUnit.Framework;

namespace ContentContent.Quests.Tests
{
    public class QuestSystemTests
    {
        [Test]
        public void QuestJournal_SequentialAndParallelObjectives_EnforceOrderingAndSignalFanOut()
        {
            var journal = new QuestJournal();

            var mainQuest = new QuestDefinition(
                id: "main_01",
                title: "Repair the Beacon",
                objectiveMode: ObjectiveMode.Sequential,
                objectives: new[]
                {
                    new ObjectiveDefinition("talk_elder", "Talk to Elder", targetCount: 1, signalKey: "talk_elder"),
                    new ObjectiveDefinition("gather_cores", "Gather Power Cores", targetCount: 2, signalKey: "core_pickup"),
                    new ObjectiveDefinition("bonus_lore", "Find Optional Tome", targetCount: 1, isOptional: true, signalKey: "core_pickup")
                });

            var sideQuest = new QuestDefinition(
                id: "side_cores",
                title: "Core Collector",
                objectiveMode: ObjectiveMode.AllParallel,
                objectives: new[]
                {
                    new ObjectiveDefinition("collect_1", "Collect 1 Core", targetCount: 1, signalKey: "core_pickup")
                });

            journal.RegisterQuest(mainQuest);
            journal.RegisterQuest(sideQuest);

            Assert.IsTrue(journal.TryStartQuest("main_01"));
            Assert.IsTrue(journal.TryStartQuest("side_cores"));

            // Step 1 in Sequential quest is locked until Step 0 completes; parallel side quest advances cleanly
            int updatedEarly = journal.ReportSignal("core_pickup", 1);
            Assert.AreEqual(1, updatedEarly);
            Assert.AreEqual(QuestStatus.Completed, journal.GetStatus("side_cores"));
            Assert.IsTrue(journal.TryGetState("main_01", out QuestState mainState));
            Assert.AreEqual(0, mainState.Objectives[1].CurrentCount);

            // Completing Step 0 unlocks Step 1 without auto-advancing Step 1
            journal.ReportSignal("talk_elder", 1);
            Assert.IsTrue(mainState.IsObjectiveUnlocked(1));
            Assert.AreEqual(0, mainState.Objectives[1].CurrentCount);

            // Step 1 completes on 2 cores even if optional Step 2 isn't complete
            journal.ReportSignal("core_pickup", 1);
            Assert.AreEqual(QuestStatus.Active, mainState.Status);
            journal.ReportSignal("core_pickup", 1);
            Assert.AreEqual(QuestStatus.Completed, mainState.Status);
            Assert.AreEqual(1, mainState.CompletionCount);
        }

        [Test]
        public void QuestJournal_PrerequisitesRepeatableFailureAndSnapshots()
        {
            var journal = new QuestJournal();
            var intro = new QuestDefinition(
                id: "intro",
                objectives: new[] { new ObjectiveDefinition("reach_town", targetCount: 1) });
            var bounty = new QuestDefinition(
                id: "bounty",
                prerequisiteQuestIds: new[] { "intro" },
                objectives: new[] { new ObjectiveDefinition("slay_bandit", targetCount: 3, signalKey: "bandit_slain") },
                repeatable: true);

            journal.RegisterQuest(intro);
            journal.RegisterQuest(bounty);

            Assert.IsFalse(journal.CanStart("bounty"));
            Assert.IsTrue(journal.TryStartQuest("intro"));
            Assert.IsTrue(journal.ReportProgress("intro", "reach_town", 1));
            Assert.IsTrue(journal.CanStart("bounty"));

            Assert.IsTrue(journal.TryStartQuest("bounty"));
            journal.ReportSignal("bandit_slain", 2);

            List<QuestSnapshot> snapshots = journal.ExportSnapshots();
            Assert.IsTrue(journal.TryFailQuest("bounty"));
            Assert.AreEqual(QuestStatus.Failed, journal.GetStatus("bounty"));

            // Repeatable quest can restart after failure (or completion)
            Assert.IsTrue(journal.TryStartQuest("bounty"));
            Assert.AreEqual(0, journal.TryGetState("bounty", out QuestState resetState) ? resetState.Objectives[0].CurrentCount : -1);

            // Restore saved mid-quest snapshot
            journal.ImportSnapshots(snapshots);
            Assert.AreEqual(QuestStatus.Active, journal.GetStatus("bounty"));
            Assert.IsTrue(journal.TryGetState("bounty", out QuestState restored));
            Assert.AreEqual(2, restored.Objectives[0].CurrentCount);
        }

        [Test]
        public void QuestDefinition_Validate_CatchesMissingAndSelfPrerequisiteIssues()
        {
            var bad = new QuestDefinition(
                id: "bad_quest",
                prerequisiteQuestIds: new[] { "bad_quest" },
                objectives: new ObjectiveDefinition[0]);

            List<string> issues = bad.Validate();
            Assert.GreaterOrEqual(issues.Count, 2);
        }
    }
}
