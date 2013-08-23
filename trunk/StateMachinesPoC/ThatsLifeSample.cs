using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Machines;

namespace ThatsLife
{
	// First we define the meaningful set of values for life status:
    public enum LifeStatus { Unborn, Child, Unemployed, Employed, Retired, Dead }

	// Next we define a metadata attribute to decorate a descendant of State<...>
	// with the only allowed state transitions;
	// note the expected read-write properties MUST BE named "From", "When", "Goto", and "With"
	// "From" denotes the state we're transitioning AWAY FROM
	// "Goto" denotes the state we're transitioning TO
	// "When" denotes the trigger/signal value of the transition
	// "With" is the name of the method (possibly fully qualified type + member name)
	// executed during state transitions as they occur
	// On the types:
	// "From" is of the type of state values' type (Status in this example)
	// "When" is of the type of trigger/signal, can be a custom enum as well, but here we default to string
	// "Goto" is of the same type as "From"
	// "With" is string
	public class LifeTransitionAttribute : TransitionAttribute
	{
		public LifeStatus From { get; set; }
		public string When { get; set; }
		public LifeStatus Goto { get; set; }
		public string With { get; set; }
	}

	// Next we define the state type proper:
	[LifeTransition(From = LifeStatus.Unborn,       When = "Birth",         Goto = LifeStatus.Child,        With = "StateChange")]
	[LifeTransition(From = LifeStatus.Unborn,       When = "Death",         Goto = LifeStatus.Dead,         With = "StateChange")]
	[LifeTransition(From = LifeStatus.Child,        When = "Death",         Goto = LifeStatus.Dead,         With = "StateChange")]
	[LifeTransition(From = LifeStatus.Child,        When = "Graduation",    Goto = LifeStatus.Unemployed,   With = "StateChange")]
	[LifeTransition(From = LifeStatus.Unemployed,   When = "Employment",    Goto = LifeStatus.Employed,     With = "StateChange")]
	[LifeTransition(From = LifeStatus.Unemployed,   When = "Death",         Goto = LifeStatus.Dead,         With = "StateChange")]
	[LifeTransition(From = LifeStatus.Employed,     When = "Death",         Goto = LifeStatus.Dead,         With = "StateChange")]
	[LifeTransition(From = LifeStatus.Employed,     When = "Retirement",    Goto = LifeStatus.Retired,      With = "StateChange")]
	[LifeTransition(From = LifeStatus.Retired,      When = "Death",         Goto = LifeStatus.Dead,         With = "StateChange")]
	public class Mankind : State<LifeStatus>
	{
		public static void StateChange(IState<LifeStatus> state, LifeStatus from, string trigger, LifeStatus to, object args)
		{
			Console.WriteLine("\t\t\tFrom: {0} --- (trigger: {1}) --> To: {2}", from, trigger, to);
		}
	}

	// We also define a signal source of triggers/signals, an IObservable<string> here:
    public class Life : SignalSource { }

	// Finally, the state machine proper, compatible with the above:
    public class Person : Machine<Mankind, LifeStatus> { }

	public static class Example
	{
		public static void Run()
		{
            var JohnsLife = new Life();
			var John = new Person().Using(JohnsLife).Start();
			Console.WriteLine("Simulation 0:");
			// We use the signal source that the start state (and others) of the state machine
			// is an observer of (IObserver<string>):
			JohnsLife.Emit("Birth");
			JohnsLife.Emit("Graduation");
			JohnsLife.Emit("Employment");
			JohnsLife.Emit("Retirement");
			JohnsLife.Emit("Death");
		}
	}
}