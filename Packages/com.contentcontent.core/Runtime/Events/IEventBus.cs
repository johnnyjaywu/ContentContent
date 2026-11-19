using System;

namespace ContentContent.Core.Events
{
    /// <summary>
    /// Delegate for receiving events by read-only reference to avoid copying large structs.
    /// </summary>
    public delegate void EventCallback<T>(in T evt);

    /// <summary>
    /// Contract for a typed publish/subscribe event bus.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes a by-ref handler to events of type <typeparamref name="T"/>.
        /// Higher <paramref name="priority"/> handlers are invoked first.
        /// </summary>
        EventSubscription Subscribe<T>(EventCallback<T> handler, int priority = 0);

        /// <summary>
        /// Subscribes an <see cref="Action{T}"/> handler to events of type <typeparamref name="T"/>.
        /// Higher <paramref name="priority"/> handlers are invoked first.
        /// </summary>
        EventSubscription Subscribe<T>(Action<T> handler, int priority = 0);

        /// <summary>
        /// Immediately publishes <paramref name="evt"/> to all active subscribers of type <typeparamref name="T"/>.
        /// </summary>
        void Publish<T>(in T evt);

        /// <summary>
        /// Returns the number of active subscribers for event type <typeparamref name="T"/>.
        /// </summary>
        int GetSubscriberCount<T>();

        /// <summary>
        /// Removes all subscribers for event type <typeparamref name="T"/>.
        /// </summary>
        void Clear<T>();

        /// <summary>
        /// Removes all subscribers across all event types.
        /// </summary>
        void Clear();
    }
}
