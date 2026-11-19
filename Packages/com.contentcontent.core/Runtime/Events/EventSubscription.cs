using System;

namespace ContentContent.Core.Events
{
    internal interface IEventChannel
    {
        void Unsubscribe(int listenerId);
        void Clear();
    }

    /// <summary>
    /// Disposable handle representing a single subscription on an <see cref="IEventBus"/>.
    /// Calling <see cref="Dispose"/> is idempotent and safe during event publication.
    /// </summary>
    public readonly struct EventSubscription : IDisposable, IEquatable<EventSubscription>
    {
        private readonly IEventChannel _channel;
        private readonly int _id;

        /// <summary>True if this handle references a registered subscription.</summary>
        public bool IsValid => _channel != null && _id > 0;

        internal EventSubscription(IEventChannel channel, int id)
        {
            _channel = channel;
            _id = id;
        }

        /// <summary>Unsubscribes the associated handler from the event bus.</summary>
        public void Dispose()
        {
            if (_channel != null && _id > 0)
            {
                _channel.Unsubscribe(_id);
            }
        }

        public bool Equals(EventSubscription other)
        {
            return ReferenceEquals(_channel, other._channel) && _id == other._id;
        }

        public override bool Equals(object obj)
        {
            return obj is EventSubscription other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _channel != null ? _channel.GetHashCode() : 0;
                return (hash * 397) ^ _id;
            }
        }

        public static bool operator ==(EventSubscription left, EventSubscription right) => left.Equals(right);
        public static bool operator !=(EventSubscription left, EventSubscription right) => !left.Equals(right);
    }
}
