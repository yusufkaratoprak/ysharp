using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machines
{
    #region State graph definition
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class TransitionAttribute : Attribute { }

    public sealed class Edge<TValue, TData, TArgs>
    {
        #region Internal members
        internal Edge(TValue source, TValue target, Handler<TValue, TData, TArgs> handler) { Source = source; Target = target; Handler = handler; }
        #endregion

        #region Public members
        public Handler<TValue, TData, TArgs> Handler { get; private set; }

        public TValue Source { get; private set; }

        public TValue Target { get; private set; }
        #endregion
    }

    public interface ITransition<TValue, TData, TArgs>
    {
        TValue From { get; }

        TData When { get; }

        TValue Goto { get; }

        Handler<TValue, TData, TArgs> With { get; }
    }

    public struct Transition<TValue> : ITransition<TValue, string, object>
    {
        #region Public members
        public TValue From { get; set; }

        public string When { get; set; }

        public TValue Goto { get; set; }

        public Handler<TValue, string, object> With { get; set; }
        #endregion
    }

    public struct Transition<TValue, TData> : ITransition<TValue, TData, object>
    {
        #region Public members
        public TValue From { get; set; }

        public TData When { get; set; }

        public TValue Goto { get; set; }

        public Handler<TValue, TData, object> With { get; set; }
        #endregion
    }

    public struct Transition<TValue, TData, TArgs> : ITransition<TValue, TData, TArgs>
    {
        #region Public members
        public TValue From { get; set; }

        public TData When { get; set; }

        public TValue Goto { get; set; }

        public Handler<TValue, TData, TArgs> With { get; set; }
        #endregion
    }
    #endregion

    #region State transition handling
    public enum ExecutionStep { StartState, LeaveState, EnterState, StateComplete, SourceComplete, SourceError }

    public delegate void Handler<in TValue>(IState<TValue> state, ExecutionStep step, TValue value, string info, object args);

    public delegate void Handler<in TValue, in TData>(IState<TValue> state, ExecutionStep step, TValue value, TData info, object args);

    public delegate void Handler<in TValue, in TData, in TArgs>(IState<TValue> state, ExecutionStep step, TValue value, TData info, TArgs args);
    #endregion

    #region State object interfaces
    public interface IState
    {
        bool IsConstant { get; }

        bool IsFinal { get; }
    }

    public interface INamedState : IState
    {
        string Moniker { get; }
    }

    public interface IState<out TValue> : IState
    {
        TValue Value { get; }
    }

    public interface IState<out TValue, TData> : IState<TValue>, IObserver<TData>
    {
        IState<TValue, TData> GetNext(TData input);

        IState<TValue, TData> MoveNext(TData input);
    }

    public interface IState<out TValue, TData, TArgs> : IState<TValue, TData>, IObserver<KeyValuePair<TData, TArgs>>
    {
        IState<TValue, TData, TArgs> GetNext(TData info, TArgs args);

        IState<TValue, TData, TArgs> MoveNext(TData info, TArgs args);

        IState<TValue, TData, TArgs> GetNext(KeyValuePair<TData, TArgs> input);

        IState<TValue, TData, TArgs> MoveNext(KeyValuePair<TData, TArgs> input);
    }
    #endregion

    #region State object implementations
    public class State<TValue> : State<TValue, string>
    {
        #region Public constructors
        public State() : base() { }

        public State(TValue value) : base(value) { }
        #endregion
    }

    public class State<TValue, TData> : State<TValue, TData, object>
    {
        #region Public constructors
        public State() : base() { }

        public State(TValue value) : base(value) { }
        #endregion
    }

    public class NamedState<TValue> : NamedState<TValue, string>
    {
        #region Public constructors
        public NamedState() : base() { }

        public NamedState(TValue value) : base(value) { }
        #endregion
    }

    public class NamedState<TValue, TData> : NamedState<TValue, TData, object>
    {
        #region Public constructors
        public NamedState() : base() { }

        public NamedState(TValue value) : base(value) { }
        #endregion
    }

    public class NamedState<TValue, TData, TArgs> : State<TValue, TData, TArgs>, INamedState
    {
        #region Private members
        private string moniker;
        #endregion

        #region Public constructors
        public NamedState() : base() { }

        public NamedState(TValue value) : base(value) { }
        #endregion

        #region INamedState implementation
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
        #endregion
    }

    public class State<TValue, TData, TArgs> : IState<TValue, TData, TArgs>
    {
        #region Private members
        private IDisposable keySubscription;
        
        private IDisposable keyValueSubscription;

        private void AddTransition(IDictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>> origins, TValue source, TData trigger, TValue target, Handler<TValue, TData, TArgs> handler)
        {
            if (!origins.ContainsKey(source))
                origins.Add(source, new Dictionary<TData, Edge<TValue, TData, TArgs>>());
            origins[source].Add(trigger, new Edge<TValue, TData, TArgs>(source, target, handler));
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

        private bool TryGetEdge(KeyValuePair<TData, TArgs> input, bool silent, out Edge<TValue, TData, TArgs> edge)
        {
            var value = Value;
            Edge<TValue, TData, TArgs> found;
            bool empty;
            if ((empty = IsEmptyStateGraph) || !IsTransitionOrigin(value))
            {
                if (!silent)
                {
                    if (empty)
                        throw new InvalidOperationException("state graph is empty");
                    else
                        throw new InvalidOperationException(String.Format("no transition from {0}", value));
                }
                edge = null;
                return false;
            }
            input = Prepare(input);
            edge = found = FollowableEdgeFor(value, input);
            return (found != null);
        }

        private IState<TValue, TData, TArgs> TryGetNext(KeyValuePair<TData, TArgs> input)
        {
            Edge<TValue, TData, TArgs> edge;
            return (TryGetEdge(input, true, out edge) ? this : null);
        }

        private void Next<TInput, TOutput>(Func<TInput, TOutput> moveNext, TInput input)
        {
            moveNext(input);
        }

        private State<TValue, TData, TArgs> HandleCompletion(ExecutionStep step, KeyValuePair<TData, TArgs>? input)
        {
            bool done;
            if (done = IsFinal)
            {
                var info = (input.HasValue ? input.Value.Key : default(TData));
                var args = (input.HasValue ? input.Value.Value : default(TArgs));
                var next = Value;
                try
                {
                    OnComplete(step != ExecutionStep.SourceComplete);
                }
                catch (Exception exception)
                {
                    if (HandleError(exception, step, info, args, ref next))
                        Value = next;
                }
            }
            return SelfIff(!done || !IsFinal);
        }

        private void UnsubscribeFrom(IDisposable subscription)
        {
            if (subscription != null)
                subscription.Dispose();
        }
        #endregion

        #region Protected members
        #region Read-only dictionary
        protected class ReadOnlyDictionary<K, V> : IDictionary<K, V>
        {
            #region Private members
            private readonly IDictionary<K, V> dictionary;
            #endregion

            #region Internal constructor
            internal ReadOnlyDictionary(IDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException("dictionary", "cannot be null");
                this.dictionary = dictionary;
            }
            #endregion

            #region Implementation
            void IDictionary<K, V>.Add(K key, V value)
            {
                throw ReadOnlyException();
            }

            public bool ContainsKey(K key)
            {
                return dictionary.ContainsKey(key);
            }

            public ICollection<K> Keys
            {
                get { return dictionary.Keys; }
            }

            bool IDictionary<K, V>.Remove(K key)
            {
                throw ReadOnlyException();
            }

            public bool TryGetValue(K key, out V value)
            {
                return dictionary.TryGetValue(key, out value);
            }

            public ICollection<V> Values
            {
                get { return dictionary.Values; }
            }

            public V this[K key]
            {
                get
                {
                    return dictionary[key];
                }
            }

            V IDictionary<K, V>.this[K key]
            {
                get
                {
                    return this[key];
                }
                set
                {
                    throw ReadOnlyException();
                }
            }

            void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
            {
                throw ReadOnlyException();
            }

            void ICollection<KeyValuePair<K, V>>.Clear()
            {
                throw ReadOnlyException();
            }

            public bool Contains(KeyValuePair<K, V> item)
            {
                return dictionary.Contains(item);
            }

            public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                dictionary.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return dictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
            {
                throw ReadOnlyException();
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return dictionary.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static Exception ReadOnlyException()
            {
                return new NotSupportedException("dictionary is read-only");
            }
            #endregion
        }
        #endregion

        #region State graph construction and handling
        #region "Edge star" (transition edge set for same state of origin in the state graph)
        protected class EdgeStar : ReadOnlyDictionary<TData, Edge<TValue, TData, TArgs>>
        {
            #region Internal constructor
            internal EdgeStar(IDictionary<TData, Edge<TValue, TData, TArgs>> edges) : base(edges) { }
            #endregion
        }
        #endregion

        #region "State graph" (set of edge stars for all states of origin)
        protected class StateGraph : ReadOnlyDictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>>
        {
            #region Internal constructor
            internal StateGraph(IDictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>> origins) : base(origins) { }
            #endregion
        }
        #endregion

        protected IDictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>> Edges { get; private set; }

        protected bool IsEmptyStateGraph { get { return ((Edges == null) || (Edges.Count == 0)); } }

        protected State<TValue, TData, TArgs> SelfIff(bool guard) { return (guard ? this : null); }

        protected State<TValue, TData, TArgs> Build()
        {
            return Build(true);
        }

        protected State<TValue, TData, TArgs> Build(bool includeAttributes)
        {
            return Build(null, includeAttributes);
        }

        protected State<TValue, TData, TArgs> Build(System.Collections.IEnumerable transitions)
        {
            return Build(transitions, true);
        }

        protected State<TValue, TData, TArgs> Build(System.Collections.IEnumerable transitions, bool includeAttributes)
        {
            if (IsConstant)
                throw new InvalidOperationException("cannot modify a state constant");
            if (IsEmptyStateGraph)
            {
                var origins = new Dictionary<TValue, IDictionary<TData, Edge<TValue, TData, TArgs>>>();
                if (includeAttributes)
                {
                    var attributes = GetType().GetCustomAttributes(typeof(TransitionAttribute), false);
                    foreach (TransitionAttribute attribute in attributes)
                    {
                        var transition = ParseTransition(attribute as object);
                        AddTransition(origins, transition.From, transition.When, transition.Goto, transition.With);
                    }
                }
                if (transitions != null)
                {
                    foreach (var transition in transitions)
                    {
                        if (transition != null)
                        {
                            ITransition<TValue, TData, TArgs> typed;
                            if (
                                typeof(ITransition<TValue, TData, TArgs>).IsAssignableFrom(transition.GetType()) ||
                                typeof(ITransition<TValue, TData, object>).IsAssignableFrom(transition.GetType()) ||
                                typeof(ITransition<TValue, string, object>).IsAssignableFrom(transition.GetType())
                            )
                                typed = (ITransition<TValue, TData, TArgs>)(object)transition;
                            else
                                typed = ParseTransition(transition as object);
                            AddTransition(origins, typed.From, typed.When, typed.Goto, typed.With);
                        }
                    }
                }
                foreach (var state in origins.Keys.ToArray())
                    origins[state] = new EdgeStar(origins[state]);
                Edges = new StateGraph(origins);
            }
            else
                throw new InvalidOperationException("state graph already defined");
            return this;
        }
        #endregion

        #region Subscribe to / unsubscribe from signal sources
        protected IDisposable KeySubscription
        {
            get { return keySubscription; }
            set
            {
                var subscription = keySubscription;
                keySubscription = null;
                UnsubscribeFrom(subscription);
                keySubscription = value;
            }
        }

        protected IDisposable KeyValueSubscription
        {
            get { return keyValueSubscription; }
            set
            {
                var subscription = keyValueSubscription;
                keyValueSubscription = null;
                UnsubscribeFrom(subscription);
                keyValueSubscription = value;
            }
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

        protected void UnsubscribeFromAll()
        {
            KeySubscription = null;
            KeyValueSubscription = null;
        }
        #endregion

        #region Overridable helpers
        protected virtual bool IsConstantState(TValue value)
        {
            return (this == (object)Value);
        }

        protected virtual bool IsFinalState(TValue value)
        {
            return (IsEmptyStateGraph || !Edges.ContainsKey(value));
        }

        protected virtual bool IsTransitionOrigin(TValue value)
        {
            return !IsFinal;
        }

        protected virtual Edge<TValue, TData, TArgs> FollowableEdgeFor(TValue value, KeyValuePair<TData, TArgs> input)
        {
            IDictionary<TData, Edge<TValue, TData, TArgs>> edges;
            Edge<TValue, TData, TArgs> edge;
            return ((Edges.TryGetValue(value, out edges)) && edges.TryGetValue(input.Key, out edge) ? edge : null);
        }
        #endregion

        #region Overridable state transition and error handling
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

        protected virtual void OnComplete(bool stateComplete)
        {
        }

        protected virtual bool HandleError(Exception exception, ExecutionStep step, TData info, TArgs args, ref TValue next)
        {
            return false;
        }
        #endregion
        #endregion

        #region Public constructors
        public State() { Build(); }

        public State(TValue value) { Build(); Start(value); }
        #endregion

        #region IState implementation
        public bool IsConstant { get { return IsConstantState(Value); } }

        public bool IsFinal { get { return IsFinalState(Value); } }
        #endregion

        #region IState<TValue> implementation
        public TValue Value { get; private set; }
        #endregion

        #region IState<TValue, TData> implementation
        public IState<TValue, TData> GetNext(TData input)
        {
            return GetNext(input, default(TArgs));
        }

        public IState<TValue, TData> MoveNext(TData input)
        {
            return MoveNext(input, default(TArgs));
        }
        #endregion

        #region IState<TValue, TData, TArgs> implementation
        public IState<TValue, TData, TArgs> GetNext(TData info, TArgs args)
        {
            return GetNext(new KeyValuePair<TData, TArgs>(info, args));
        }

        public IState<TValue, TData, TArgs> MoveNext(TData info, TArgs args)
        {
            return MoveNext(new KeyValuePair<TData, TArgs>(info, args));
        }

        public IState<TValue, TData, TArgs> GetNext(KeyValuePair<TData, TArgs> input)
        {
            return TryGetNext(input);
        }

        public IState<TValue, TData, TArgs> MoveNext(KeyValuePair<TData, TArgs> input)
        {
            var value = Value;
            Edge<TValue, TData, TArgs> edge;
            if (TryGetEdge(input, false, out edge))
            {
                var next = edge.Target;
                var step = ExecutionStep.LeaveState;
                try
                {
                    var handler = edge.Handler;
                    OnChange(step, next, input.Key, input.Value);
                    if (handler != null)
                        handler(this, step, next, input.Key, input.Value);
                    step = ExecutionStep.EnterState;
                    Value = next;
                    if (handler != null)
                        handler(this, step, value, input.Key, input.Value);
                    OnChange(step, value, input.Key, input.Value);
                }
                catch (Exception exception)
                {
                    if (HandleError(exception, step, input.Key, input.Value, ref next))
                        Value = next;
                }
                return HandleCompletion(ExecutionStep.StateComplete, input);
            }
            else
                throw new InvalidOperationException(String.Format("invalid transition from {0}", value));
        }
        #endregion

        #region IObserver<TData> and IObserver<KeyValuePair<TData, TArgs>> implementations
        public void OnCompleted()
        {
            HandleCompletion(ExecutionStep.SourceComplete, null);
        }

        public void OnError(Exception exception)
        {
            var next = Value;
            if (HandleError(exception, ExecutionStep.SourceError, default(TData), default(TArgs), ref next))
                Value = next;
        }

        public void OnNext(TData input)
        {
            Next(MoveNext, input);
        }

        public void OnNext(KeyValuePair<TData, TArgs> input)
        {
            Next(MoveNext, input);
        }
        #endregion

        #region Public members
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
                if (HandleError(exception, ExecutionStep.StartState, default(TData), default(TArgs), ref value))
                    Value = value;
            }
            return HandleCompletion(ExecutionStep.StartState, null);
        }

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
        #endregion
    }
    #endregion

    #region Signal source interfaces
    public interface ISignalSource<TData> : IObservable<TData>
    {
        ISignalSource<TData> Emit(TData info);
    }

    public interface ISignalSource<TData, TArgs> : IObservable<KeyValuePair<TData, TArgs>>
    {
        ISignalSource<TData, TArgs> Emit(TData info);

        ISignalSource<TData, TArgs> Emit(TData info, TArgs args);
    }
    #endregion

    #region Signal source implementations
    public class SignalSource : SignalSource<string> { }

    public class SignalSource<TData, TArgs> : SignalSource<KeyValuePair<TData, TArgs>>, ISignalSource<TData, TArgs>
    {
        #region ISignalSource<TData, TArgs> implementation
        public ISignalSource<TData, TArgs> Emit(TData info)
        {
            return Emit(info, default(TArgs));
        }

        public ISignalSource<TData, TArgs> Emit(TData info, TArgs args)
        {
            return (ISignalSource<TData, TArgs>)Emit(new KeyValuePair<TData, TArgs>(info, args));
        }
        #endregion
    }

    public class SignalSource<TInput> : ISignalSource<TInput>
    {
        #region Private members
        private readonly HashSet<Subscription> Subscriptions = new HashSet<Subscription>();

        private class Subscription : IDisposable
        {
            #region Private members
            private readonly SignalSource<TInput> Source;
            #endregion

            #region Internal constructor
            internal Subscription(SignalSource<TInput> source, IObserver<TInput> observer) { Source = source; Observer = observer; }
            #endregion

            #region Internal members
            internal IObserver<TInput> Observer { get; private set; }
            #endregion

            #region IDisposable implementation
            public void Dispose() { Source.RemoveExistingSubscription(this); }
            #endregion
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
        #endregion

        #region IObservable<TInput> implementation
        public IDisposable Subscribe(IObserver<TInput> observer)
        {
            if (observer == null) throw new ArgumentNullException("observer", "cannot be null");
            return NewOrExistingSubscription(observer);
        }
        #endregion

        #region ISignalSource<TInput> implementation
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
        #endregion
    }
    #endregion
}