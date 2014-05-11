using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    using Machines;

    public static class ThatsLifeSample
    {
        public enum Status { Unborn, Child, Unemployed, Employed, Retired, Dead }

        public class PersonTransitionAttribute : TransitionAttribute
        {
            public Status From { get; set; }
            public string When { get; set; }
            public Status Goto { get; set; }
            public object With { get; set; }
        }

        [PersonTransition(From = Status.Unborn, When = "Birth", Goto = Status.Child)]
        [PersonTransition(From = Status.Unborn, When = "Death", Goto = Status.Dead)]
        [PersonTransition(From = Status.Child, When = "Graduation", Goto = Status.Unemployed)]
        [PersonTransition(From = Status.Child, When = "Death", Goto = Status.Dead)]
        [PersonTransition(From = Status.Unemployed, When = "Graduation", Goto = Status.Unemployed)]
        [PersonTransition(From = Status.Unemployed, When = "Employment", Goto = Status.Employed)]
        [PersonTransition(From = Status.Unemployed, When = "Death", Goto = Status.Dead)]
        [PersonTransition(From = Status.Employed, When = "Lay off", Goto = Status.Unemployed)]
        [PersonTransition(From = Status.Employed, When = "Resignation", Goto = Status.Unemployed)]
        [PersonTransition(From = Status.Employed, When = "Retirement", Goto = Status.Retired)]
        [PersonTransition(From = Status.Employed, When = "Death", Goto = Status.Dead)]
        [PersonTransition(From = Status.Retired, When = "Death", Goto = Status.Dead)]
        public class Person : State<Status, string, DateTime>
        {
            private static readonly IDictionary<string, string> VerbToNoun = new Dictionary<string, string>
            {
                { "born", "Birth" },
                { "graduate", "Graduation" },
                { "work", "Employment" },
                { "laid off", "Lay off" },
                { "resign", "Resignation" },
                { "retire", "Retirement" },
                { "die", "Death" }
            };

            // For convenience, map some verbs to the corresponding valid transition nouns
            // (this will be taken into account during calls to IState<...>.MoveNext(...)) :
            protected override KeyValuePair<string, DateTime> Prepare(KeyValuePair<string, DateTime> input)
            {
                return base.Prepare(new KeyValuePair<string, DateTime>(VerbToNoun.ContainsKey(input.Key) ? VerbToNoun[input.Key] : input.Key, input.Value));
            }

            // Executed before and after every state transition :
            protected override void OnChange(ExecutionStep step, Status value, string info, DateTime args)
            {
                if (step == ExecutionStep.EnterState)
                {
                    var timeStamp = String.Format("\t\t(@ {0})", (args != default(DateTime)) ? args : DateTime.Now);
                    // 'value' is the state value that we have transitioned FROM :
                    Console.WriteLine("\t{0} -- {1} -> {2}{3}", value, info, this, timeStamp);
                }
            }

            public override string ToString() { return Value.ToString(); }
        }

        public static void Run()
        {
            Console.Clear();

            // Create a person state machine instance, and return it, set in some start state :
            var joe = new Person().Start(Status.Unborn);
            bool done;

            // Holds iff the chosen start state isn't a final state :
            System.Diagnostics.Debug.Assert(joe != null, "The chosen start state is a final state!");

            // Trigger state transitions with or without the (optional) DateTime argument :
            done =
                (
                    joe.
                        MoveNext("born", new DateTime(1963, 2, 1)). // equivalent to : joe.MoveNext("Birth", ...)
                        MoveNext("graduate", new DateTime(1980, 6, 5)).
                        MoveNext("work", new DateTime(1981, 7, 6)).
                        MoveNext("Lay off", new DateTime(1982, 8, 7)). // equivalent to : joe.MoveNext("laid off", ...)
                        MoveNext("work", new DateTime(1983, 9, 8)).
                        MoveNext("retire") // MoveNext(...) returns null iff joe.IsFinal == true
                    == null
                );

            Console.WriteLine();
            Console.WriteLine("Is Joe's state '{0}' a final state? {1}", joe.Value, done);

            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}