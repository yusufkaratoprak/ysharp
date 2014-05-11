using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    using Machines;

    public static class WatchingTvSample
    {
        public enum Status { Unplugged, Off, On, Disposed }

        public class DeviceTransitionAttribute : TransitionAttribute
        {
            public Status From { get; set; }
            public string When { get; set; }
            public Status Goto { get; set; }
            public object With { get; set; }
        }

        // State<Status> is a shortcut for / derived from State<Status, string>,
        // which in turn is a shortcut for / derived from State<Status, string, object> :
        public class Device : State<Status>
        {
            // For convenience, enter the start state when the parameterless constructor executes :
            public Device() : base(Status.Unplugged) { }
        }

        [DeviceTransition(From = Status.Unplugged, When = "Plug", Goto = Status.Off)]
        [DeviceTransition(From = Status.Unplugged, When = "Dispose", Goto = Status.Disposed)]
        [DeviceTransition(From = Status.Off, When = "Switch On", Goto = Status.On)]
        [DeviceTransition(From = Status.Off, When = "Unplug", Goto = Status.Unplugged)]
        [DeviceTransition(From = Status.Off, When = "Dispose", Goto = Status.Disposed)]
        [DeviceTransition(From = Status.On, When = "Switch Off", Goto = Status.Off)]
        [DeviceTransition(From = Status.On, When = "Unplug", Goto = Status.Unplugged)]
        [DeviceTransition(From = Status.On, When = "Dispose", Goto = Status.Disposed)]
        public class Television : Device
        {
            // Executed before and after every state transition :
            protected override void OnChange(ExecutionStep step, Status value, string info, object args)
            {
                if (step == ExecutionStep.EnterState)
                {
                    // 'value' is the state value we have transitioned FROM :
                    Console.WriteLine("\t{0} -- {1} -> {2}", value, info, this);
                }
            }

            public override string ToString() { return Value.ToString(); }
        }

        public static void Run()
        {
            Console.Clear();

            // Create the TV state machine set in its start state :
            var tv = new Television();

            // Trigger state transitions without argument
            // ('args' ignored by the state machine anyway) :
            tv.MoveNext("Plug");
            tv.MoveNext("Switch On");
            tv.MoveNext("Switch Off");
            tv.MoveNext("Switch On");
            tv.MoveNext("Switch Off");
            tv.MoveNext("Unplug");
            tv.MoveNext("Dispose"); // MoveNext(...) returns null iff IsFinal == true

            Console.WriteLine();
            Console.WriteLine("Is the TV's state '{0}' a final state? {1}", tv.Value, tv.IsFinal);

            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}