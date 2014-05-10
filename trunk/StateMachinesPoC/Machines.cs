using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machines
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class TransitionAttribute : Attribute { }

    public enum ExecutionStep { Start, Leave, Enter, Complete, SourceComplete, SourceError }

    public delegate void Handler<TValue, TData, TArgs>(IState<TValue> state, ExecutionStep step, TValue value, TData info, TArgs args);

    public sealed class Edge<TValue, TData, TArgs>
    {
        internal Edge(TValue source, TValue target, Handler<TValue, TData, TArgs> handler) { Source = source; Target = target; Handler = handler; }
        public Handler<TValue, TData, TArgs> Handler { get; private set; }
        public TValue Source { get; private set; }
        public TValue Target { get; private set; }
    }

    public struct Transition<TValue, TData, TArgs>
    {
        public TValue From { get; set; }
        public TData When { get; set; }
        public TValue Goto { get; set; }
        public Handler<TValue, TData, TArgs> With { get; set; }
    }

    public interface IState
    {
        bool IsConstant { get; }
        bool IsFinal { get; }
    }

    public interface INamedState : IState
    {
        string Moniker { get; }
    }

    public interface IState<TValue> : IState
    {
        bool IsFinalAt(TValue value);
        TValue Value { get; }
    }

    public interface IState<TValue, TData> : IState<TValue>, IObserver<TData>
    {
        IState<TValue, TData> MoveNext(TData info);
    }

    public interface IState<TValue, TData, TArgs> : IState<TValue, TData>, IObserver<KeyValuePair<TData, TArgs>>
    {
        IState<TValue, TData, TArgs> MoveNext(TData info, TArgs args);
        IState<TValue, TData, TArgs> MoveNext(KeyValuePair<TData, TArgs> input);
    }

    public class State<TValue> : State<TValue, string>
    {
        public State() : base() { }
        public State(TValue value) : base(value) { }
    }

    public class State<TValue, TData> : State<TValue, TData, object>
    {
        public State() : base() { }
        public State(TValue value) : base(value) { }
    }

    public class NamedState<TValue> : NamedState<TValue, string> { }

    public class NamedState<TValue, TData> : NamedState<TValue, TData, object> { }

    public class NamedState<TValue, TData, TArgs> : State<TValue, TData, TArgs>, INamedState
    {
        private string moniker;
        public string Moniker
        {
            get { return moniker; }
            protected set
            {
                if (moniker != null)
                    throw new InvalidOperationException("moniker already defined");
                if (value == null)
                    throw new ArgumentNullException("value", "cannot be null");
                moniker = value;
            }
        }
    }

    public class State<TValue, TData, TArgs> : IState<TValue, TData, TArgs>
    {
        protected IDictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>> Edges { get; private set; }

        private IDisposable keySubscription;
        protected IDisposable KeySubscription
        {
            get { return keySubscription; }
            set
            {
                var subscription = keySubscription;
                keySubscription = null;
                Unsubscribe(subscription);
                keySubscription = value;
            }
        }

        private IDisposable keyValueSubscription;
        protected IDisposable KeyValueSubscription
        {
            get { return keyValueSubscription; }
            set
            {
                var subscription = keyValueSubscription;
                keyValueSubscription = null;
                Unsubscribe(subscription);
                keyValueSubscription = value;
            }
        }

        private void Unsubscribe(IDisposable subscription)
        {
            if (subscription != null)
                subscription.Dispose();
        }

        private void AddTransition(IDictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>> edges, TValue source, TData trigger, TValue target, Handler<TValue, TData, TArgs> handler)
        {
            if (!edges.ContainsKey(source))
                edges.Add(source, new Dictionary<TData, Edge<TValue, TData, TArgs>>());
            edges[source].Add(trigger, new Edge<TValue, TData, TArgs>(source, target, handler));
        }

        private Transition<TValue, TData, TArgs> ParseTransition(object untyped)
        {
            var type = untyped.GetType();
            var with = type.GetProperty("With");
            var value = ((with != null) ? with.GetValue(untyped, null) : null);
            var source = (TValue)type.GetProperty("From").GetValue(untyped, null);
            var info = (TData)type.GetProperty("When").GetValue(untyped, null);
            var target = (TValue)type.GetProperty("Goto").GetValue(untyped, null);
            var method = ((with != null) ? ((value is string) ? (((string)value).Contains('.') ? Type.GetType(((string)value).Substring(0, ((string)value).LastIndexOf('.'))) : GetType()).GetMethod((((string)value).Contains('.') ? ((string)value).Substring(((string)value).LastIndexOf('.') + 1) : (string)value), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy) : null) : null);
            var handler = ((method != null) ? (Handler<TValue, TData, TArgs>)Delegate.CreateDelegate(typeof(Handler<TValue, TData, TArgs>), (!method.IsStatic ? this : null), method) : ((value is Delegate) ? (Handler<TValue, TData, TArgs>)value : null));
            if ((value is string) && (handler == null))
                throw new InvalidOperationException(String.Format("transition handler method {0} not found", value));
            return new Transition<TValue, TData, TArgs> { From = source, When = info, Goto = target, With = handler };
        }

        private TValue Required(TValue value)
        {
            if (IsFinalAt(value))
                throw new InvalidOperationException(String.Format("no state graph edge at {0}", value));
            return value;
        }

        public State() { Build(); }

        public State(TValue value) { Build(); Start(value); }

        public State<TValue, TData, TArgs> Build()
        {
            return Build(true);
        }

        public State<TValue, TData, TArgs> Build(bool includeAttributes)
        {
            return Build(null, includeAttributes);
        }

        public State<TValue, TData, TArgs> Build(IEnumerable transitions)
        {
            return Build(transitions, true);
        }

        public State<TValue, TData, TArgs> Build(IEnumerable transitions, bool includeAttributes)
        {
            if (IsConstant)
                throw new InvalidOperationException("cannot modify a state constant");
            if ((Edges == null) || (Edges.Count == 0))
            {
                var edges = new Dictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>>();
                if (includeAttributes)
                {
                    var attributes = GetType().GetCustomAttributes(typeof(TransitionAttribute), false);
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
            else
                throw new InvalidOperationException("state graph already defined");
            return this;
        }

        protected IDisposable SubscribeTo(IObservable<TData> source)
        {
            KeySubscription = null;
            return (KeySubscription = ((source != null) ? source.Subscribe(this) : null));
        }

        protected IDisposable SubscribeTo(IObservable<KeyValuePair<TData, TArgs>> source)
        {
            KeyValueSubscription = null;
            return (KeyValueSubscription = ((source != null) ? source.Subscribe(this) : null));
        }

        protected void Unsubscribe()
        {
            KeySubscription = null;
            KeyValueSubscription = null;
        }

        protected virtual KeyValuePair<TData, TArgs> Prepare(KeyValuePair<TData, TArgs> input)
        {
            return input;
        }

        protected virtual void OnStart(TValue value)
        {
        }

        protected virtual void OnChange(ExecutionStep step, TValue value, TData info, TArgs args)
        {
        }

        protected virtual bool HandleError(Exception exception, ExecutionStep step, TData info, TArgs args, ref TValue next)
        {
            return false;
        }

        protected virtual void OnComplete(bool sourceComplete)
        {
        }

        public State<TValue, TData, TArgs> Start(TValue value)
        {
            if (IsConstant)
                throw new InvalidOperationException("cannot modify a state constant");
            try
            {
                OnStart(value);
                Value = value;
            }
            catch (Exception exception)
            {
                if (HandleError(exception, ExecutionStep.Start, default(TData), default(TArgs), ref value))
                    Value = value;
            }
            return this;
        }

        public TValue Value { get; protected set; }

        public State<TValue, TData, TArgs> Using(IObservable<TData> source)
        {
            if ((SubscribeTo(source) == null) && (source != null))
                throw new InvalidOperationException(String.Format("could not subscribe to {0}", typeof(IObservable<TData>).FullName));
            else
                return this;
        }

        public State<TValue, TData, TArgs> Using(IObservable<KeyValuePair<TData, TArgs>> source)
        {
            if ((SubscribeTo(source) == null) && (source != null))
                throw new InvalidOperationException(String.Format("could not subscribe to {0}", typeof(IObservable<KeyValuePair<TData, TArgs>>).FullName));
            else
                return this;
        }

        public IState<TValue, TData> MoveNext(TData input)
        {
            return MoveNext(input, default(TArgs));
        }

        public IState<TValue, TData, TArgs> MoveNext(TData info, TArgs args)
        {
            return MoveNext(new KeyValuePair<TData, TArgs>(info, args));
        }

        public IState<TValue, TData, TArgs> MoveNext(KeyValuePair<TData, TArgs> input)
        {
            var from = Required(Value);
            input = Prepare(input);
            if (Edges[from].ContainsKey(input.Key))
            {
                var edge = Edges[from][input.Key];
                var next = edge.Target;
                var step = ExecutionStep.Leave;
                try
                {
                    var handler = edge.Handler;
                    OnChange(step, next, input.Key, input.Value);
                    if (handler != null)
                        handler(this, step, next, input.Key, input.Value);
                    step = ExecutionStep.Enter;
                    Value = next;
                    if (handler != null)
                        handler(this, step, from, input.Key, input.Value);
                    OnChange(step, from, input.Key, input.Value);
                }
                catch (Exception exception)
                {
                    if (HandleError(exception, step, input.Key, input.Value, ref next))
                        Value = next;
                }
                if (IsFinal)
                    try
                    {
                        OnComplete(false);
                    }
                    catch (Exception exception)
                    {
                        if (HandleError(exception, ExecutionStep.Complete, input.Key, input.Value, ref next))
                            Value = next;
                    }
                return (!IsFinal ? this : null);
            }
            else
                throw new InvalidOperationException("invalid transition");
        }

        public void OnCompleted()
        {
            var next = Value;
            try
            {
                OnComplete(true);
            }
            catch (Exception exception)
            {
                if (HandleError(exception, ExecutionStep.SourceComplete, default(TData), default(TArgs), ref next))
                    Value = next;
            }
        }

        public void OnError(Exception exception)
        {
            var next = Value;
            if (HandleError(exception, ExecutionStep.SourceError, default(TData), default(TArgs), ref next))
                Value = next;
        }

        private void Next<TInput, TResult>(Func<TInput, TResult> moveNext, TInput input)
        {
            moveNext(input);
        }

        public void OnNext(TData input)
        {
            Next<TData, IState<TValue, TData>>(MoveNext, input);
        }

        public void OnNext(KeyValuePair<TData, TArgs> input)
        {
            Next<KeyValuePair<TData, TArgs>, IState<TValue, TData, TArgs>>(MoveNext, input);
        }

        public bool IsFinalAt(TValue value) { return ((Edges == null) || !Edges.ContainsKey(value)); }

        public bool IsFinal { get { return IsFinalAt(Value); } }

        public bool IsConstant { get { return (this == (object)Value); } }
    }

    public interface ISignalSource<TData> : IObservable<TData>
    {
        ISignalSource<TData> Emit(TData info);
    }

    public interface ISignalSource<TData, TArgs> : IObservable<KeyValuePair<TData, TArgs>>
    {
        ISignalSource<TData, TArgs> Emit(TData info);
        ISignalSource<TData, TArgs> Emit(TData info, TArgs args);
    }

    public class SignalSource : SignalSource<string> { }

    public class SignalSource<TData, TArgs> : SignalSource<KeyValuePair<TData, TArgs>>, ISignalSource<TData, TArgs>
    {
        public ISignalSource<TData, TArgs> Emit(TData info)
        {
            return Emit(info, default(TArgs));
        }

        public ISignalSource<TData, TArgs> Emit(TData info, TArgs args)
        {
            return (ISignalSource<TData, TArgs>)Emit(new KeyValuePair<TData, TArgs>(info, args));
        }
    }

    public class SignalSource<TInput> : ISignalSource<TInput>
    {
        private readonly HashSet<Subscription> Subscriptions = new HashSet<Subscription>();

        private class Subscription : IDisposable
        {
            private readonly SignalSource<TInput> Source;
            internal Subscription(SignalSource<TInput> source, IObserver<TInput> observer) { Source = source; Observer = observer; }
            internal IObserver<TInput> Observer { get; private set; }
            public void Dispose() { Source.RemoveExistingSubscription(this); }
        }

        private Subscription NewOrExistingSubscription(IObserver<TInput> observer)
        {
            var subscription = Subscriptions.Where(item => item.Observer == observer).FirstOrDefault();
            if (subscription == null)
            {
                subscription = new Subscription(this, observer);
                Subscriptions.Add(subscription);
            }
            return subscription;
        }

        private void RemoveExistingSubscription(Subscription subscription)
        {
            if (Subscriptions.Contains(subscription))
            {
                var observer = subscription.Observer;
                try
                {
                    try
                    {
                        observer.OnCompleted();
                    }
                    finally
                    {
                        Subscriptions.Remove(subscription);
                    }
                }
                catch (Exception exception)
                {
                    observer.OnError(exception);
                }
            }
        }

        public IDisposable Subscribe(IObserver<TInput> observer)
        {
            if (observer == null) throw new ArgumentNullException("observer", "cannot be null");
            return NewOrExistingSubscription(observer);
        }

        public ISignalSource<TInput> Emit(TInput input)
        {
            foreach (var observer in Subscriptions.Select(item => item.Observer).ToArray())
                try
                {
                    observer.OnNext(input);
                }
                catch (Exception exception)
                {
                    observer.OnError(exception);
                }
            return this;
        }
    }
}