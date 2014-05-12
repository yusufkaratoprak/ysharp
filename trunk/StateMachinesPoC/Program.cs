using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    using Machines;

    public class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                char choice;
                Console.Clear();
                Console.WriteLine("Press \"1\" for \"That's Life\" sample");
                Console.WriteLine("Press \"2\" for \"Watching TV\" sample");
                Console.WriteLine("Press \"3\" for \"Watching TV Advanced\" sample");
                Console.WriteLine("Press \"4\" for \"SO question 5923767\" : Juliet's example");
                Console.WriteLine("Press \"5\" for \"SO question 5923767\" : Pete Stensønes's example");
                Console.WriteLine();
                Console.WriteLine("Press ESC to exit");
                if ((choice = Console.ReadKey().KeyChar) != 27)
                    switch (choice)
                    {
                        case '1':
                            ThatsLifeSample.Run();
                            break;
                        case '2':
                            WatchingTvSample.Run();
                            break;
                        case '3':
                            WatchingTvSampleAdvanced.Run();
                            break;
                        case '4':
                            SO_5923767_Juliet.Run();
                            break;
                        case '5':
                            SO_5923767_Pete.Run();
                            break;
                        default:
                            break;
                    }
                else
                    break;
            }
        }
    }

    // Translations of two answers to "Simple state machine example in C#?" on StackOverflow
    // ( http://stackoverflow.com/questions/5923767/simple-state-machine-example-in-c )
    //
    // Note we follow the convention used de facto in Juliet's and Pete's examples of reporting only
    // after entry into the new (destination) state (of the active transition) :

    // Translation of :
    // http://stackoverflow.com/a/5924053/1409653
    // (Juliet's answer)
    public static class SO_5923767_Juliet
    {
        public enum ProcessState { Inactive, Active, Paused, Terminated }
        public enum Command { Begin, End, Pause, Resume, Exit }
        public class Process : State<ProcessState, Command>
        {
            public Process()
            {
                Build(new[] {
new Transition<ProcessState, Command> { From = ProcessState.Inactive, When = Command.Exit, Goto = ProcessState.Terminated },
new Transition<ProcessState, Command> { From = ProcessState.Inactive, When = Command.Begin, Goto = ProcessState.Active },
new Transition<ProcessState, Command> { From = ProcessState.Active, When = Command.End, Goto = ProcessState.Inactive },
new Transition<ProcessState, Command> { From = ProcessState.Active, When = Command.Pause, Goto = ProcessState.Paused },
new Transition<ProcessState, Command> { From = ProcessState.Paused, When = Command.End, Goto = ProcessState.Inactive },
new Transition<ProcessState, Command> { From = ProcessState.Paused, When = Command.Resume, Goto = ProcessState.Active }
                });
                Start(ProcessState.Inactive);
            }
            private static void OnEnter(ExecutionStep guard, Action action) { if (guard == ExecutionStep.EnterState) action(); }
            protected override void OnStart(ProcessState startState) { Console.WriteLine("In Ctor() : --> {0}", startState); }
            protected override void OnChange(ExecutionStep step, ProcessState value, Command trigger, object args) {
                OnEnter(step, () => {
                    Console.WriteLine("In OnChange(...) : {0} --{1}-> {2}", value, trigger, this.Value);
                });
            }
        }

        public static void Run()
        {
            Console.Clear();
            var p = new Process();
            p.MoveNext(Command.Begin);
            p.MoveNext(Command.Pause);
            p.MoveNext(Command.End);
            p.MoveNext(Command.Exit);
            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }

    // Translation of :
    // http://stackoverflow.com/a/5924286/1409653
    // (Pete Stensønes's answer)
    public static class SO_5923767_Pete
    {
        public enum States { Start, Standby, On }
        public enum Events { PlugIn, TurnOn, TurnOff, RemovePower }
        public class FiniteStateMachine : State<States, Events>
        {
            public FiniteStateMachine() {
                Build(new[] {
new Transition<States, Events> { From = States.Start, When = Events.PlugIn, Goto = States.Standby, With = PowerOn },
new Transition<States, Events> { From = States.Standby, When = Events.TurnOn, Goto = States.On, With = StandbyWhenOff },
new Transition<States, Events> { From = States.Standby, When = Events.RemovePower, Goto = States.Start, With = PowerOff },
new Transition<States, Events> { From = States.On, When = Events.TurnOff, Goto = States.Standby, With = StandbyWhenOn },
new Transition<States, Events> { From = States.On, When = Events.RemovePower, Goto = States.Start, With = PowerOff }
                });
                Start(States.Start);
            }

            protected override void OnStart(States startState) { Console.WriteLine("In Ctor() : --> {0}", startState); }
            private static void OnEnter(ExecutionStep guard, Action action) { if (guard == ExecutionStep.EnterState) action(); }
            private void PowerOn(IState<States> state, ExecutionStep step, States value, Events trigger, object args) {
                OnEnter(step, () => {
                    Console.WriteLine("In PowerOn(...) : {0} --{1}-> {2}", value, trigger, this.Value);
                });
            }
            private void PowerOff(IState<States> state, ExecutionStep step, States value, Events trigger, object args) {
                OnEnter(step, () => {
                    Console.WriteLine("In PowerOff(...) : {0} --{1}-> {2}", value, trigger, this.Value);
                });
            }
            private void StandbyWhenOn(IState<States> state, ExecutionStep step, States value, Events trigger, object args) {
                OnEnter(step, () => {
                    Console.WriteLine("In StandbyWhenOn(...) : {0} --{1}-> {2}", value, trigger, this.Value);
                });
            }
            private void StandbyWhenOff(IState<States> state, ExecutionStep step, States value, Events trigger, object args) {
                OnEnter(step, () => {
                    Console.WriteLine("In StandbyWhenOff(...) : {0} --{1}-> {2}", value, trigger, this.Value);
                });
            }
        }

        public static void Run()
        {
            Console.Clear();
            var fsm = new FiniteStateMachine();
            fsm.MoveNext(Events.PlugIn);
            fsm.MoveNext(Events.TurnOn);
            fsm.MoveNext(Events.TurnOff);
            fsm.MoveNext(Events.TurnOn);
            fsm.MoveNext(Events.RemovePower);
            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}