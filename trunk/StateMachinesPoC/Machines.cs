using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machines
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class TransitionAttribute : Attribute { }

	public delegate void Handler<TValue, Trigger, TArgs>(IState<TValue> state, TValue from, Trigger trigger, TValue to, TArgs args);

	public sealed class Edge<TValue, Trigger, TArgs>
	{
		internal Edge(TValue source, TValue target, Handler<TValue, Trigger, TArgs> handler) { Source = source; Target = target; Handler = handler; }
		public Handler<TValue, Trigger, TArgs> Handler { get; private set; }
		public TValue Source { get; private set; }
		public TValue Target { get; private set; }
	}

	public struct Transition<TValue, Trigger, TArgs>
	{
		public TValue From { get; set; }
		public Trigger When { get; set; }
		public TValue Goto { get; set; }
		public Handler<TValue, Trigger, TArgs> With { get; set; }
	}

	public interface IStatus
	{
		bool HasValue { get; }
		object Status { get; }
	}

	public interface IStatus<TValue> : IStatus
	{
		TValue Value { get; }
	}

	public interface IState : IStatus, IEnumerable, IEnumerator, IDisposable
	{
		bool Consume(object input);
		IEnumerator Input { get; }
		bool Completed { get; }
		object Context { get; }
		bool IsFinal { get; }
	}

	public interface IState<TValue> : IStatus<TValue>, IEnumerable<TValue>, IEnumerator<TValue>, IState
	{
		IState<TValue> Build();
		IState<TValue> Build(object context);
		IState<TValue> Build(object context, bool ignoreAttributes);
		IState<TValue> Using();
		IState<TValue> Using(object context);
		IState<TValue> Using(object context, TValue start);
		IState<TValue> Start();
		IState<TValue> Start(TValue start);
	}

	public interface IState<TValue, Trigger> : IState<TValue>, IObserver<Trigger>
	{
		bool MoveNext(Trigger trigger);
	}

	public interface IState<TValue, Trigger, TArgs> : IState<TValue, Trigger>, IObserver<Tuple<Trigger, TArgs>>
	{
		bool MoveNext(KeyValuePair<Trigger, TArgs> signal);
		bool MoveNext(Tuple<Trigger, TArgs> signal);
		bool MoveNext(Trigger trigger, TArgs args);
	}

	public class State<TValue> : State<TValue, string> { }

	public class State<TValue, Trigger> : State<TValue, Trigger, object> { }

	public class State<TValue, Trigger, TArgs> : IState<TValue, Trigger, TArgs>
	{
		protected IDictionary<TValue, IDictionary<Trigger, Edge<TValue, Trigger, TArgs>>> Edges { get; private set; }
		protected TValue StartValue { get; private set; }
		public bool HasValue { get { return (Status != null); } }
		public object Status { get; protected set; }
		public TValue Value
		{
			get
			{
				if (!HasValue)
					throw new InvalidOperationException("undefined value");
				return (TValue)Status;
			}
			protected set
			{
				Status = value;
			}
		}
		public IEnumerator Input { get; private set; }
		public bool Completed { get { return (Context == null); } }
		public object Context { get; private set; }

		private void AddTransition(IDictionary<TValue, IDictionary<Trigger, Edge<TValue, Trigger, TArgs>>> edges, TValue source, Trigger trigger, TValue target, Handler<TValue, Trigger, TArgs> handler)
		{
			if (!edges.ContainsKey(source))
				edges.Add(source, new Dictionary<Trigger, Edge<TValue, Trigger, TArgs>>());
			edges[source].Add(trigger, new Edge<TValue, Trigger, TArgs>(source, target, handler));
		}

		private Transition<TValue, Trigger, TArgs> ParseTransition(object untyped)
		{
			var type = untyped.GetType();
			var with = type.GetProperty("With");
			var value = ((with != null) ? with.GetValue(untyped, null) : null);
			var source = (TValue)type.GetProperty("From").GetValue(untyped, null);
			var trigger = (Trigger)type.GetProperty("When").GetValue(untyped, null);
			var target = (TValue)type.GetProperty("Goto").GetValue(untyped, null);
			var method = ((with != null) ? ((value is string) ? (((string)value).Contains('.') ? Type.GetType(((string)value).Substring(0, ((string)value).LastIndexOf('.'))) : GetType()).GetMethod((((string)value).Contains('.') ? ((string)value).Substring(((string)value).LastIndexOf('.') + 1) : (string)value), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy) : null) : null);
			var handler = ((method != null) ? (Handler<TValue, Trigger, TArgs>)Delegate.CreateDelegate(typeof(Handler<TValue, Trigger, TArgs>), (!method.IsStatic ? this : null), method) : ((value is Delegate) ? (Handler<TValue, Trigger, TArgs>)value : null));
			if ((value is string) && (handler == null))
				throw new InvalidOperationException(String.Format("Transition handler method {0} not found", value));
			return new Transition<TValue, Trigger, TArgs> { From = source, When = trigger, Goto = target, With = handler };
		}

		protected void Attach(object context)
		{
			if (context != null)
			{
				Observe(null);
				Link(context);
				Context = context;
			}
		}

		protected virtual void Link(object context)
		{
			if (context is IObservable<Trigger>)
				Observe(((IObservable<Trigger>)context).Subscribe(this));
			else if (context is IObservable<Tuple<Trigger, TArgs>>)
				Observe(((IObservable<Tuple<Trigger, TArgs>>)context).Subscribe(this));
		}

		protected virtual void Unlink(object context)
		{
			bool finish = true;
			if (context is ISignalSource<Trigger>)
				finish = ((ISignalSource<Trigger>)context).IsDone(this);
			else if (context is ISignalSource<Tuple<Trigger, TArgs>>)
				finish = ((ISignalSource<Tuple<Trigger, TArgs>>)context).IsDone(this);
			if (finish && (context is IDisposable))
				((IDisposable)context).Dispose();
		}

		protected void Detach()
		{
			Detach(false);
		}

		protected void Detach(bool force)
		{
			if (force || IsFinal)
				Observe(null);
		}

		protected void Observe(object source)
		{
			Observe(source, true);
		}

		protected void Observe(object source, bool reattach)
		{
			object current = Context;
			if (source == null)
			{
				Context = source;
				if ((current != null) && reattach)
					Unlink(current);
				Input = null;
			}
			else
				Observe(null, false);
		}

		protected IState<TValue> Build(Transition<TValue, Trigger, TArgs>[] transitions, object context)
		{
			return Build(transitions, context, false);
		}

		protected IState<TValue> Build(Transition<TValue, Trigger, TArgs>[] transitions, object context, bool ignoreAttributes)
		{
			return Build(transitions as IEnumerable, context, ignoreAttributes);
		}

		protected IState<TValue> Build(IEnumerable transitions, object context)
		{
			return Build(transitions, context, false);
		}

		protected IState<TValue> Build(IEnumerable transitions, object context, bool ignoreAttributes)
		{
			if (Edges == null)
			{
				var edges = new Dictionary<TValue, IDictionary<Trigger, Edge<TValue, Trigger, TArgs>>>();
				if (!ignoreAttributes)
				{
					var attributes = GetType().GetCustomAttributes(typeof(TransitionAttribute), true);
					foreach (TransitionAttribute attribute in attributes)
					{
						var transition = ParseTransition(attribute);
						AddTransition(edges, transition.From, transition.When, transition.Goto, transition.With);
					}
				}
				if (transitions != null)
				{
					foreach (var untyped in transitions)
					{
						if (untyped != null)
						{
							var transition = ParseTransition(untyped);
							AddTransition(edges, transition.From, transition.When, transition.Goto, transition.With);
						}
					}
				}
				Edges = edges;
			}
			Attach(context);
			return this;
		}

		protected IState<TValue> Using(object context, object start)
		{
			if (!Accept(context))
				throw new InvalidOperationException(String.Format("context type or value not supported ({0})", ((context != null) ? context.GetType().FullName : "null")));
			Attach(context);
			Reset((start != null) ? (TValue)start : StartValue);
			return this;
		}

		protected virtual bool Accept(object context)
		{
			return ((context is IEnumerable<Trigger>) || (context is IObservable<Trigger>) || (context is IObservable<Tuple<Trigger, TArgs>>));
		}

		protected virtual bool Reset(TValue start)
		{
			Value = start;
			return HasValue;
		}

		protected virtual void OnStateChange(Trigger trigger, TArgs args, TValue next)
		{
		}

		protected virtual void OnStateChanged(TValue from, Trigger trigger, TArgs args)
		{
		}

		protected virtual bool? OnError(Exception error)
		{
			return null;
		}

		protected virtual bool? OnError(Exception error, Trigger trigger, TArgs args, TValue next)
		{
			return null;
		}

		public virtual void Reset()
		{
			Using();
		}

		public IState<TValue> Using()
		{
			return Using(null);
		}

		public IState<TValue> Using(object context)
		{
			return Using(context, default(TValue));
		}

		public IState<TValue> Using(object context, TValue start)
		{
			return Using(context, start as object);
		}

		public IState<TValue> Build()
		{
			return Build(null);
		}

		public IState<TValue> Build(object context)
		{
			return Build(context, false);
		}

		public IState<TValue> Build(object context, bool ignoreAttributes)
		{
			return Build(null, context, ignoreAttributes);
		}

		public IState<TValue> Start()
		{
			return Start(default(TValue));
		}

		public IState<TValue> Start(TValue start)
		{
			if (!Reset(start))
				throw new InvalidOperationException("no start state");
			StartValue = Current;
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return this;
		}

		public virtual void Dispose()
		{
			Observe(null);
		}

		public virtual bool Consume(object input)
		{
			if (input is Trigger)
				return MoveNext((Trigger)input);
			else if (input is Tuple<Trigger, TArgs>)
				return MoveNext((Tuple<Trigger, TArgs>)input);
			else if (input is KeyValuePair<Trigger, TArgs>)
				return MoveNext((KeyValuePair<Trigger, TArgs>)input);
			else
				return false;
		}

		public virtual bool MoveNext()
		{
			if (!IsFinal && (Context != null))
			{
				Input = ((Input == null) ? (Input = ((Context is IEnumerable) ? ((IEnumerable)Context).GetEnumerator() : (Context as IEnumerator))) : Input);
				if (Input != null)
				{
					if (Input.MoveNext())
						return Consume(Input.Current);
					else
					{
						Detach(true);
						return false;
					}
				}
				else
					return false;
			}
			else
			{
				Detach(true);
				return false;
			}
		}

		public bool MoveNext(Trigger trigger)
		{
			return MoveNext(new Tuple<Trigger, TArgs>(trigger, default(TArgs)));
		}

		public bool MoveNext(KeyValuePair<Trigger, TArgs> input)
		{
			return MoveNext(new Tuple<Trigger, TArgs>(input.Key, input.Value));
		}

		public bool MoveNext(Tuple<Trigger, TArgs> input)
		{
			return MoveNext(input.Item1, input.Item2);
		}

		public bool MoveNext(Trigger trigger, TArgs args)
		{
			if (!IsFinal && HasValue)
			{
				var from = Current;
				if (Edges[from].ContainsKey(trigger))
				{
					var next = Edges[from][trigger].Target;
					OnStateChange(trigger, args, next);
					if (Edges[from][trigger].Handler != null)
					{
						Exception exception = null, error = null;
						try
						{
							Edges[from][trigger].Handler(this, Current, trigger, next, args);
						}
						catch (Exception ex)
						{
							var handled = OnError(ex, trigger, args, next);
							if (handled.HasValue)
							{
								if (!handled.Value)
									exception = ex;
								else
									error = ex;
							}
						}
						if (exception != null)
							throw exception;
						if (error == null)
						{
							Current = next;
							OnStateChanged(from, trigger, args);
						}
					}
					else
					{
						Current = next;
						OnStateChanged(from, trigger, args);
					}
					Detach();
					return true;
				}
				else
					throw new InvalidOperationException("invalid transition");
			}
			else
			{
				Detach();
				return false;
			}
		}

		public TValue Current { get { return Value; } protected set { Value = value; } }

		TValue IEnumerator<TValue>.Current { get { return Current; } }

		object IEnumerator.Current { get { return Current; } }

		void IObserver<Trigger>.OnCompleted()
		{
			if (!Completed)
				Dispose();
		}

		void IObserver<Tuple<Trigger, TArgs>>.OnCompleted()
		{
			if (!Completed)
				Dispose();
		}

		void IObserver<Trigger>.OnError(Exception error)
		{
			bool? handled = OnError(error);
			if (handled.HasValue && !handled.Value)
				Dispose();
		}

		void IObserver<Tuple<Trigger, TArgs>>.OnError(Exception error)
		{
			bool? handled = OnError(error);
			if (handled.HasValue && !handled.Value)
				Dispose();
		}

		void IObserver<Trigger>.OnNext(Trigger value)
		{
			Consume(value);
		}

		void IObserver<Tuple<Trigger, TArgs>>.OnNext(Tuple<Trigger, TArgs> value)
		{
			Consume(value);
		}

		public bool IsFinal { get { return (HasValue && (Edges.Keys.Count > 0) && !Edges.ContainsKey(Current)); } }
	}

	public interface IMachine<TValue> : IEnumerable<TValue>
	{
		IState<TValue> Using();
		IState<TValue> Using(object context);
		IState<TValue> Using(object context, TValue start);
		object Context { get; }
	}

	public interface IMachine<TState, TValue> : IMachine<TValue>
		where TState : IState<TValue>
	{
	}

	public class Machine<TState, TValue> : Machine<TState, TValue, string>
		where TState : IState<TValue, string, object>
	{
	}

	public class Machine<TState, TValue, Trigger> : Machine<TState, TValue, Trigger, object>
		where TState : IState<TValue, Trigger, object>
	{
	}

	public class Machine<TState, TValue, Trigger, TArgs> : IMachine<TState, TValue>
		where TState : IState<TValue, Trigger, TArgs>
	{
		protected IState<TValue> Using(object context, params object[] args)
		{
			IState<TValue> state;
			if (context != null)
				Context = context;
			state = Activator.CreateInstance<TState>().Build(Context);
			if ((args != null) && (args.Length > 1))
			{
				OnStart(state);
				state.Start((TValue)args[0]);
				OnStarted(state);
			}
			return state;
		}

		protected virtual void OnStart(IState<TValue> start)
		{
		}

		protected virtual void OnStarted(IState<TValue> start)
		{
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return Using();
		}

		public IState<TValue> Using()
		{
			return Using(Context);
		}

		public IState<TValue> Using(TValue start)
		{
			return Using(Context, start);
		}

		public IState<TValue> Using(object context)
		{
			return Using(context, null);
		}

		public IState<TValue> Using(object context, TValue start)
		{
			return Using(context, start, null);
		}

		public object Context { get; private set; }
	}

	public interface ISignalling : IDisposable
	{
		ISignalling Emit(object signal);
	}

	public interface ISignalling<Trigger, TArgs> : ISignalling<Tuple<Trigger, TArgs>>
	{
		ISignalling<Trigger, TArgs> Emit(Trigger trigger);
		ISignalling<Trigger, TArgs> Emit(KeyValuePair<Trigger, TArgs> signal);
		ISignalling<Trigger, TArgs> Emit(Trigger trigger, TArgs args);
	}

	public interface ISignalling<TSignal> : IObservable<TSignal>, ISignalling
	{
		ISignalling<TSignal> Emit(TSignal signal);
	}

	public interface ISignalSource : ISignalling
	{
		bool IsDone();
	}

	public interface ISignalSource<Trigger, TArgs> : ISignalSource<Tuple<Trigger, TArgs>>, ISignalling<Trigger, TArgs>
	{
	}

	public interface ISignalSource<TSignal> : ISignalling<TSignal>, ISignalSource
	{
		bool IsDone(IObserver<TSignal> observer);
	}

	public class SignalSource : SignalSource<string> { }

	public class SignalSource<Trigger> : SignalSource<Trigger, object> { }

	public class SignalSource<Trigger, TArgs> : SignalSourceBase<Tuple<Trigger, TArgs>>, ISignalSource<Trigger, TArgs>
	{
		public ISignalling<Trigger, TArgs> Emit(Trigger trigger)
		{
			return (ISignalling<Trigger, TArgs>)Emit(new Tuple<Trigger, TArgs>(trigger, default(TArgs)));
		}

		public ISignalling<Trigger, TArgs> Emit(KeyValuePair<Trigger, TArgs> signal)
		{
			return (ISignalling<Trigger, TArgs>)Emit(new Tuple<Trigger, TArgs>(signal.Key, signal.Value));
		}

		public ISignalling<Trigger, TArgs> Emit(Trigger trigger, TArgs args)
		{
			return (ISignalling<Trigger, TArgs>)Emit(new Tuple<Trigger, TArgs>(trigger, args));
		}
	}

	public class SignalSourceBase<TSignal> : ISignalSource<TSignal>
	{
		protected IDictionary<IDisposable, IObserver<TSignal>> Consumers = new Dictionary<IDisposable, IObserver<TSignal>>();

		protected object Ensure<TSubject>(object subject)
		{
			if (subject != null)
			{
				if (!(subject is TSubject))
					throw new InvalidOperationException(String.Format("subject must conform to {0}", typeof(TSubject).FullName));
			}
			else
				throw new ArgumentNullException("subject", "cannot be null");
			return (TSubject)subject;
		}

		protected IDisposable Handle(IObserver<TSignal> observer, bool subscribe)
		{
			IDisposable consumer = null;
			if (observer != null)
			{
				consumer = (IDisposable)Ensure<IDisposable>(observer);
				if (subscribe)
					Consumers[consumer] = observer;
				else
				{
					if (Consumers.ContainsKey(consumer))
						Consumers.Remove(consumer);
				}
			}
			else
				Consumers.Clear();
			return ((Consumers.Count > 0) ? this : null);
		}

		public void Dispose()
		{
			IsDone(null);
		}

		public IDisposable Subscribe(IObserver<TSignal> observer)
		{
			return Handle(observer, true);
		}

		public ISignalling Emit(object signal)
		{
			return Emit((TSignal)Ensure<TSignal>(signal));
		}

		public ISignalling<TSignal> Emit(TSignal signal)
		{
			IObserver<TSignal>[] consumers = Consumers.Values.ToArray();
			foreach (IObserver<TSignal> consumer in consumers)
				consumer.OnNext(signal);
			return this;
		}

		public bool IsDone()
		{
			return IsDone(null);
		}

		public bool IsDone(IObserver<TSignal> observer)
		{
			if (observer == null)
			{
				IObserver<TSignal>[] consumers = Consumers.Values.ToArray();
				foreach (IObserver<TSignal> consumer in consumers)
					consumer.OnCompleted();
			}
			else
				observer.OnCompleted();
			return (Handle(observer, false) == null);
		}
	}
}