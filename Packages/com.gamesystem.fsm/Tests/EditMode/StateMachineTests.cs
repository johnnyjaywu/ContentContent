using System.Collections.Generic;
using NUnit.Framework;

namespace GameSystem.Fsm.Tests
{
    public class StateMachineTests
    {
        [Test]
        public void Transitions_InvokeLifecycleInExpectedOrder()
        {
            var log = new List<string>();
            bool shouldMove = false;

            var idle = new ActionState(
                name: "Idle",
                onEnter: () => log.Add("Idle.Enter"),
                onTick: _ => log.Add("Idle.Tick"),
                onExit: () => log.Add("Idle.Exit"));

            var move = new ActionState(
                name: "Move",
                onEnter: () => log.Add("Move.Enter"),
                onTick: _ => log.Add("Move.Tick"),
                onExit: () => log.Add("Move.Exit"));

            var fsm = new StateMachine(idle);
            fsm.AddTransition(idle, move, () => shouldMove);

            fsm.OnEnter();
            fsm.OnTick(0.016f);

            shouldMove = true;
            fsm.OnTick(0.016f);

            CollectionAssert.AreEqual(
                new[] { "Idle.Enter", "Idle.Tick", "Idle.Exit", "Move.Enter", "Move.Tick" },
                log);
        }

        [Test]
        public void TransitionPriority_SelectsHighestPriorityFirst()
        {
            var idle = new ActionState("Idle");
            var lowTarget = new ActionState("Low");
            var highTarget = new ActionState("High");

            var fsm = new StateMachine(idle);
            fsm.AddTransition(idle, lowTarget, () => true, priority: 1);
            fsm.AddTransition(idle, highTarget, () => true, priority: 10);

            fsm.OnEnter();
            fsm.OnTick(0.016f);

            Assert.AreSame(highTarget, fsm.CurrentState);
        }

        [Test]
        public void AnyTransition_OverridesStateSpecificTransitionsAndSkipsSelf()
        {
            bool isDead = false;
            int deadEnters = 0;

            var idle = new ActionState("Idle");
            var dead = new ActionState("Dead", onEnter: () => deadEnters++);

            var fsm = new StateMachine(idle);
            fsm.AddAnyTransition(dead, () => isDead);

            fsm.OnEnter();
            isDead = true;
            fsm.OnTick(0.016f);
            fsm.OnTick(0.016f);

            Assert.AreSame(dead, fsm.CurrentState);
            Assert.AreEqual(1, deadEnters);
        }

        [Test]
        public void HierarchicalStateMachine_EntersTicksAndExitsNestedSubStates()
        {
            var log = new List<string>();
            bool enterCombat = false;
            bool exitCombat = false;

            var patrol = new ActionState("Patrol",
                onEnter: () => log.Add("Patrol.Enter"),
                onExit: () => log.Add("Patrol.Exit"));

            var aim = new ActionState("Aim",
                onEnter: () => log.Add("Aim.Enter"),
                onTick: _ => log.Add("Aim.Tick"),
                onExit: () => log.Add("Aim.Exit"));

            var combatSubFsm = new StateMachine(aim);

            var rootFsm = new StateMachine(patrol);
            rootFsm.AddTransition(patrol, combatSubFsm, () => enterCombat);
            rootFsm.AddTransition(combatSubFsm, patrol, () => exitCombat);

            rootFsm.OnEnter();
            enterCombat = true;
            rootFsm.OnTick(0.016f);

            enterCombat = false;
            exitCombat = true;
            rootFsm.OnTick(0.016f);

            CollectionAssert.AreEqual(
                new[] { "Patrol.Enter", "Patrol.Exit", "Aim.Enter", "Aim.Tick", "Aim.Exit", "Patrol.Enter" },
                log);
        }
    }
}
