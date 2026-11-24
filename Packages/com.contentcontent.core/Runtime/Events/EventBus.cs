using System;
using System.Collections.Generic;

namespace ContentContent.Core.Events
{
    /// <summary>
    /// Instance-based, zero-boxing typed publish/subscribe event bus with priority ordering
    /// and re-entrant safe subscription mutations.
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, IEventChannel> _channels = new Dictionary<Type, IEventChannel>();
        private readonly Action<Exception> _errorHandler;

        /// <summary>
        /// Creates a new <see cref="EventBus"/>.
        /// </summary>
        /// <param name="errorHandler">
        /// Optional exception handler invoked if a subscriber throws during <see cref="Publish{T}"/>.
        /// When null, exceptions propagate immediately (fail-fast).
        /// </param>
        /// <summary>Number of distinct event types that currently have channels.</summary>
        public int ChannelCount => _channels.Count;

        public EventBus(Action<Exception> errorHandler = null)
        {
            _errorHandler = errorHandler;
        }

        public EventSubscription Subscribe<T>(EventCallback<T> handler, int priority = 0)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channel = GetOrCreateChannel<T>();
            return channel.Subscribe(handler, null, priority);
        }

        public EventSubscription Subscribe<T>(Action<T> handler, int priority = 0)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channel = GetOrCreateChannel<T>();
            return channel.Subscribe(null, handler, priority);
        }

        public void Publish<T>(in T evt)
        {
            if (!_channels.TryGetValue(typeof(T), out var rawChannel))
                return;

            var channel = (Channel<T>)rawChannel;
            channel.Publish(in evt, _errorHandler);
        }

        public int GetSubscriberCount<T>()
        {
            if (!_channels.TryGetValue(typeof(T), out var rawChannel))
                return 0;

            return ((Channel<T>)rawChannel).ActiveCount;
        }

        public void Clear<T>()
        {
            if (_channels.TryGetValue(typeof(T), out var rawChannel))
            {
                rawChannel.Clear();
            }
        }

        public void Clear()
        {
            foreach (var kvp in _channels)
            {
                kvp.Value.Clear();
            }
        }

        /// <summary>
        /// Copies each registered event type name and active subscriber count for Editor telemetry.
        /// </summary>
        public void CopyChannelStats(List<KeyValuePair<string, int>> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.Clear();
            foreach (var kvp in _channels)
            {
                output.Add(new KeyValuePair<string, int>(kvp.Key.Name, kvp.Value.ActiveSubscriberCount));
            }
        }

        private Channel<T> GetOrCreateChannel<T>()
        {
            var type = typeof(T);
            if (!_channels.TryGetValue(type, out var rawChannel))
            {
                var channel = new Channel<T>();
                _channels[type] = channel;
                return channel;
            }

            return (Channel<T>)rawChannel;
        }

        private sealed class Channel<T> : IEventChannel
        {
            private sealed class Listener
            {
                public int Id;
                public int Priority;
                public EventCallback<T> RefHandler;
                public Action<T> ActionHandler;
                public bool IsActive;
            }

            private readonly List<Listener> _listeners = new List<Listener>();
            private readonly List<Listener> _pendingAdds = new List<Listener>();
            private int _nextId = 1;
            private int _publishDepth;
            private bool _hasPendingRemovals;
            private int _activeCount;

            public int ActiveCount => _activeCount;
            public int ActiveSubscriberCount => _activeCount;

            public EventSubscription Subscribe(EventCallback<T> refHandler, Action<T> actionHandler, int priority)
            {
                int id = _nextId++;
                var listener = new Listener
                {
                    Id = id,
                    Priority = priority,
                    RefHandler = refHandler,
                    ActionHandler = actionHandler,
                    IsActive = true
                };

                _activeCount++;

                if (_publishDepth > 0)
                {
                    _pendingAdds.Add(listener);
                }
                else
                {
                    InsertByPriority(listener);
                }

                return new EventSubscription(this, id);
            }

            public void Unsubscribe(int listenerId)
            {
                for (int i = 0; i < _listeners.Count; i++)
                {
                    var listener = _listeners[i];
                    if (listener.Id == listenerId)
                    {
                        if (!listener.IsActive)
                            return;

                        listener.IsActive = false;
                        _activeCount--;

                        if (_publishDepth > 0)
                        {
                            _hasPendingRemovals = true;
                        }
                        else
                        {
                            _listeners.RemoveAt(i);
                        }

                        return;
                    }
                }

                for (int i = 0; i < _pendingAdds.Count; i++)
                {
                    var listener = _pendingAdds[i];
                    if (listener.Id == listenerId)
                    {
                        if (listener.IsActive)
                        {
                            listener.IsActive = false;
                            _activeCount--;
                        }

                        _pendingAdds.RemoveAt(i);
                        return;
                    }
                }
            }

            public void Publish(in T evt, Action<Exception> errorHandler)
            {
                if (_listeners.Count == 0)
                    return;

                _publishDepth++;
                try
                {
                    int count = _listeners.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var listener = _listeners[i];
                        if (!listener.IsActive)
                            continue;

                        if (errorHandler != null)
                        {
                            try
                            {
                                InvokeListener(listener, in evt);
                            }
                            catch (Exception ex)
                            {
                                errorHandler(ex);
                            }
                        }
                        else
                        {
                            InvokeListener(listener, in evt);
                        }
                    }
                }
                finally
                {
                    _publishDepth--;
                    if (_publishDepth == 0)
                    {
                        FlushPendingChanges();
                    }
                }
            }

            public void Clear()
            {
                if (_publishDepth > 0)
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        _listeners[i].IsActive = false;
                    }

                    _pendingAdds.Clear();
                    _activeCount = 0;
                    _hasPendingRemovals = true;
                }
                else
                {
                    _listeners.Clear();
                    _pendingAdds.Clear();
                    _activeCount = 0;
                    _hasPendingRemovals = false;
                }
            }

            private static void InvokeListener(Listener listener, in T evt)
            {
                if (listener.RefHandler != null)
                {
                    listener.RefHandler(in evt);
                }
                else
                {
                    listener.ActionHandler(evt);
                }
            }

            private void FlushPendingChanges()
            {
                if (_hasPendingRemovals)
                {
                    for (int i = _listeners.Count - 1; i >= 0; i--)
                    {
                        if (!_listeners[i].IsActive)
                        {
                            _listeners.RemoveAt(i);
                        }
                    }

                    _hasPendingRemovals = false;
                }

                if (_pendingAdds.Count > 0)
                {
                    for (int i = 0; i < _pendingAdds.Count; i++)
                    {
                        var listener = _pendingAdds[i];
                        if (listener.IsActive)
                        {
                            InsertByPriority(listener);
                        }
                    }

                    _pendingAdds.Clear();
                }
            }

            private void InsertByPriority(Listener listener)
            {
                int index = _listeners.Count;
                for (int i = 0; i < _listeners.Count; i++)
                {
                    if (listener.Priority > _listeners[i].Priority)
                    {
                        index = i;
                        break;
                    }
                }

                _listeners.Insert(index, listener);
            }
        }
    }
}
