using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Machines;

namespace WatchingTVSampleAdvanced
{
	public enum TVEvent
	{
		Plug,
		SwitchOn,
		SwitchOff,
		Unplug,
		Destroy
	}

	public class TVState : State<TVState, TVEvent, DateTime>
	{
		public static readonly TVState Unplugged = new TVState("unplugged");
		public static readonly TVState Off = new TVState("turned off");
		public static readonly TVState On = new TVState("turned on");
		public static readonly TVState Disposed = new TVState("disposed");

		public TVState() : this(true) { }
		public TVState(string name) : this(false) { Name = name; Value = this; }
		public TVState(bool define)
		{
			if (define)
			{
				var definition = new[]
				{
					new { From = Unplugged,		When = TVEvent.Destroy,		Goto = Disposed,	With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = Off,			When = TVEvent.Destroy,		Goto = Disposed,	With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = On,			When = TVEvent.Destroy,		Goto = Disposed,	With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = Unplugged,		When = TVEvent.Plug,		Goto = Off,			With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = Off,			When = TVEvent.SwitchOn,	Goto = On,			With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = Off,			When = TVEvent.Unplug,		Goto = Unplugged,	With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = On,			When = TVEvent.SwitchOff,	Goto = Off,			With = "WatchingTVSampleAdvanced.Television.StateChange" },
					new { From = On,			When = TVEvent.Unplug,		Goto = Unplugged,	With = "WatchingTVSampleAdvanced.Television.StateChange" }
				};
				Build(definition, null);
			}
		}

		public string Name { get; private set; }
	}

	public class Television : Machine<TVState, TVState, TVEvent, DateTime>
	{
		public static void StateChange(IState<TVState> state, TVState from, TVEvent trigger, TVState to, DateTime timeStamp)
		{
			Console.WriteLine("\t\t\tFrom: {0} --- (trigger: {1}({2})) --> To: {3}", from.Name, trigger, timeStamp, to.Name);
		}
	}

	public class TVRemote : SignalSource<TVEvent, DateTime> { }

	public static class Example
	{
		public static void Run()
		{
			ISignalling<TVEvent, DateTime> remote = new TVRemote();
			Console.WriteLine("Simulation 5:");
			// Notice how, by instantiating three distinct Television,
			// observers of the same remote control, it's a signal broadcast to all three:
			var states = (
				from state in new[]
				{
					new Tuple<TVState, TVState, TVState>
					(
						(TVState)new Television().Using(remote).Start(TVState.Unplugged),
						(TVState)new Television().Using(remote).Start(TVState.Unplugged),
						(TVState)new Television().Using(remote).Start(TVState.Unplugged)
					)
				}
				from signal in new[] { TVEvent.Plug, TVEvent.SwitchOn, TVEvent.Destroy }
				let action = remote.Emit(signal, DateTime.Now)
				select state
			).ToArray();

			Console.ReadLine();
		}
	}
}