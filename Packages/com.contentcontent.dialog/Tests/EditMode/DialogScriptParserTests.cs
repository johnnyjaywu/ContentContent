using System.Collections.Generic;
using NUnit.Framework;

namespace ContentContent.Dialog.Tests
{
    public class DialogScriptParserTests
    {
        private const string CityGateScript = @"
// Sample Yarn-style dialog script
<<declare $gold = 60>>
<<declare $met_guard = false>>
<<declare $gate_open = false>>

title: gate_arrival
---
Narrator: Rain drums against the gate. You have {$gold} gold.
<<jump guard_greeting>>
===

title: guard_greeting
---
<<set $met_guard to true>>
Gate Guard: Halt, traveler. (Gold: {$gold})
-> Offer 50 gold <<if $gold >= 50 and !$gate_open>>
    <<set $gold = $gold - 50>>
    <<play_sfx coins_clink>>
    <<jump guard_bribed>>
-> Ask for work
    <<set $gold += 20>>
    Gate Guard: Take 20 gold and stay out of trouble.
    <<jump guard_greeting>>
-> Step through the open gate <<if $gate_open>>
    Narrator: You enter the city.
-> Leave
    Gate Guard: Wise choice.
===

title: guard_bribed
---
<<set $gate_open = true>>
<<open_gate ""north gate"">>
Gate Guard: Fine. You have {$gold} gold left.
<<jump guard_greeting>>
===
";

        [Test]
        public void Parse_CityGateScript_ValidatesAndRunsEndToEnd()
        {
            DialogGraph graph = DialogScriptParser.Parse(CityGateScript, "city_gate");
            List<DialogValidationIssue> issues = graph.Validate();
            Assert.IsEmpty(issues, string.Join("; ", issues));

            var commands = new List<string>();
            var lines = new List<DialogLineViewModel>();
            var runner = new DialogRunner();
            runner.RegisterCommandHandler("play_sfx", c => commands.Add($"{c.Name}:{c.Arguments[0]}"));
            runner.RegisterCommandHandler("open_gate", c => commands.Add($"{c.Name}:{c.Arguments[0]}"));
            runner.OnLineShown += line => lines.Add(line);

            runner.Start(graph);

            Assert.AreEqual("Narrator", lines[0].Speaker);
            Assert.AreEqual("Rain drums against the gate. You have 60 gold.", lines[0].Text);

            runner.Continue(); // -> guard_greeting
            Assert.IsTrue(runner.Variables.GetBool("met_guard"));
            Assert.AreEqual(3, runner.CurrentChoices.Count); // Bribe, Ask for work, Leave (gate_open is false)

            runner.SelectChoice(0); // Bribe -> guard_bribed
            Assert.AreEqual(10, runner.Variables.GetInt("gold"));
            Assert.IsTrue(runner.Variables.GetBool("gate_open"));
            Assert.AreEqual("Fine. You have 10 gold left.", lines[lines.Count - 1].Text);
            CollectionAssert.AreEqual(new[] { "play_sfx:coins_clink", "open_gate:north gate" }, commands);

            runner.Continue(); // back to guard_greeting
            Assert.AreEqual(3, runner.CurrentChoices.Count); // Ask for work, Step through open gate, Leave
            Assert.AreEqual("Step through the open gate", runner.CurrentChoices[1].Text);

            runner.SelectChoice(1); // inline reply node -> end
            Assert.AreEqual("You enter the city.", lines[lines.Count - 1].Text);

            runner.Continue();
            Assert.IsFalse(runner.IsRunning);
        }

        [Test]
        public void Parse_MultiLineNode_ChainsLinesAutomatically()
        {
            const string script = @"
title: intro
---
Narrator: First line.
Guard: Second line.
===
";
            DialogGraph graph = DialogScriptParser.Parse(script);
            Assert.IsEmpty(graph.Validate());

            var lines = new List<DialogLineViewModel>();
            var runner = new DialogRunner();
            runner.OnLineShown += l => lines.Add(l);
            runner.Start(graph);

            Assert.AreEqual("First line.", lines[0].Text);
            runner.Continue();
            Assert.AreEqual("Second line.", lines[1].Text);
            runner.Continue();
            Assert.IsFalse(runner.IsRunning);
        }

        [Test]
        public void Parse_LineTag_UsesExplicitLocalizationKey()
        {
            const string script = @"
title: greet
---
Guard: Fallback display text #line:DLG_GREET_01
-> Yes please #line:OPT_YES
    <<stop>>
===
";
            var loc = new DictionaryLocalizer(new Dictionary<string, string>
            {
                { "Guard", "Wache" },
                { "DLG_GREET_01", "Hallo Reisender!" },
                { "OPT_YES", "Ja bitte" }
            });

            DialogGraph graph = DialogScriptParser.Parse(script);
            var lines = new List<DialogLineViewModel>();
            var runner = new DialogRunner(localizer: loc);
            runner.OnLineShown += l => lines.Add(l);
            runner.Start(graph);

            Assert.AreEqual("Wache", lines[0].Speaker);
            Assert.AreEqual("Hallo Reisender!", lines[0].Text);
            Assert.AreEqual("Ja bitte", runner.CurrentChoices[0].Text);
        }

        [Test]
        public void Parse_UnclosedNode_ThrowsWithLineNumber()
        {
            const string broken = "title: bad\n---\nGuard: Oops";
            var ex = Assert.Throws<DialogScriptParseException>(() => DialogScriptParser.Parse(broken));
            Assert.AreEqual(1, ex.LineNumber);
        }
    }
}
