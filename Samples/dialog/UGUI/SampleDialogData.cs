using System.Collections.Generic;
using ContentContent.Dialog;

namespace ContentContent.Dialog.Samples.UGUI
{
    /// <summary>
    /// Builds the sample "City Gate" branching dialog graph and localization table.
    /// Demonstrates variables, conditional choices, transition mutations (revisit-safe costs),
    /// {variable} interpolation, and custom command dispatch.
    /// </summary>
    public static class SampleDialogData
    {
        public static DialogGraph CreateGraph()
        {
            var defaults = new Dictionary<string, DialogValue>
            {
                { "gold", DialogValue.FromInt(60) },
                { "met_guard", DialogValue.FromBool(false) },
                { "gate_open", DialogValue.FromBool(false) }
            };

            var nodes = new[]
            {
                new DialogNode(
                    id: "gate_arrival",
                    speaker: "SPEAKER_NARRATOR",
                    lineKey: "LINE_ARRIVAL",
                    nextNodeId: "guard_greeting"),

                new DialogNode(
                    id: "guard_greeting",
                    speaker: "SPEAKER_GUARD",
                    lineKey: "LINE_GUARD_GREETING",
                    onEnterMutations: new[]
                    {
                        new DialogMutation("met_guard", DialogMutationOperator.Set, DialogValue.FromBool(true))
                    },
                    choices: new[]
                    {
                        new DialogChoice(
                            textKey: "CHOICE_BRIBE",
                            targetNodeId: "guard_bribed",
                            conditions: new[]
                            {
                                new DialogCondition("gold", DialogComparisonOperator.GreaterOrEqual, DialogValue.FromInt(50)),
                                new DialogCondition("gate_open", DialogComparisonOperator.Equal, DialogValue.FromBool(false))
                            },
                            mutations: new[]
                            {
                                new DialogMutation("gold", DialogMutationOperator.Subtract, DialogValue.FromInt(50))
                            },
                            commands: new[]
                            {
                                new DialogCommand("play_sfx", "coins_clink")
                            }),

                        new DialogChoice(
                            textKey: "CHOICE_ASK_WORK",
                            targetNodeId: "guard_tip"),

                        new DialogChoice(
                            textKey: "CHOICE_PASS_THROUGH",
                            targetNodeId: "enter_city",
                            conditions: new[]
                            {
                                new DialogCondition("gate_open", DialogComparisonOperator.Equal, DialogValue.FromBool(true))
                            }),

                        new DialogChoice(
                            textKey: "CHOICE_LEAVE",
                            targetNodeId: "walk_away")
                    }),

                new DialogNode(
                    id: "guard_tip",
                    speaker: "SPEAKER_GUARD",
                    lineKey: "LINE_GUARD_TIP",
                    onEnterMutations: new[]
                    {
                        new DialogMutation("gold", DialogMutationOperator.Add, DialogValue.FromInt(20))
                    },
                    onEnterCommands: new[]
                    {
                        new DialogCommand("grant_bonus", "20")
                    },
                    nextNodeId: "guard_greeting"),

                new DialogNode(
                    id: "guard_bribed",
                    speaker: "SPEAKER_GUARD",
                    lineKey: "LINE_GUARD_BRIBED",
                    onEnterMutations: new[]
                    {
                        new DialogMutation("gate_open", DialogMutationOperator.Set, DialogValue.FromBool(true))
                    },
                    onEnterCommands: new[]
                    {
                        new DialogCommand("open_gate", "north_gate")
                    },
                    nextNodeId: "guard_greeting"),

                new DialogNode(
                    id: "enter_city",
                    speaker: "SPEAKER_NARRATOR",
                    lineKey: "LINE_ENTER_CITY",
                    isEnd: true),

                new DialogNode(
                    id: "walk_away",
                    speaker: "SPEAKER_GUARD",
                    lineKey: "LINE_WALK_AWAY",
                    isEnd: true)
            };

            return new DialogGraph("city_gate_sample", "gate_arrival", nodes, defaults);
        }

        public static IDialogLocalizer CreateLocalizer()
        {
            var table = new Dictionary<string, string>
            {
                { "SPEAKER_NARRATOR", "Narrator" },
                { "SPEAKER_GUARD", "Gate Guard" },
                { "LINE_ARRIVAL", "Rain drums against the iron portcullis. You count {gold} gold in your pouch." },
                { "LINE_GUARD_GREETING", "Halt, traveler. The North Gate is sealed tonight. What business brings you here? (Gold: {gold})" },
                { "LINE_GUARD_TIP", "Drop this lost coin purse at the watchpost? Actually, keep the 20 gold finder's fee. Now you have {gold} gold." },
                { "LINE_GUARD_BRIBED", "Fifty gold... fair enough. Step lively before the captain spots us. You have {gold} gold left." },
                { "LINE_ENTER_CITY", "The heavy iron gate groans open. Warm lantern light spills onto the cobblestones ahead." },
                { "LINE_WALK_AWAY", "Wise choice. Come back at dawn." },
                { "CHOICE_BRIBE", "Offer 50 gold to open the gate" },
                { "CHOICE_ASK_WORK", "Ask if there is any work (+20 gold)" },
                { "CHOICE_PASS_THROUGH", "Step through the open gate" },
                { "CHOICE_LEAVE", "Turn back into the rain" }
            };

            return new DictionaryLocalizer(table);
        }
    }
}
