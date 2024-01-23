# WeakEventBroker

A library to assist event subscription by weak reference.

## Sample Code

```cs
class EventListenerClass
{
    public EventListenerClass(EventSourceClass eventSource)
    {
        // Subscribe to instance member events by weak reference
        var unsubscriber1 = WeakEventSubscriptionManager<EventHandler>.SubscribeToWeakEvent(eventSource, nameof(eventSource.InstanceEvent), EventHandler);

        // To explicitly unsubscribe, call Dispose().
        // Even when not explicitly unsubscribed, unsubscribe automatically when the instance of EventListenerClass is garbage-collected.
        unsubscriber1.Dispose();


        // Subscribe to static member events by weak reference
        var unsubscriber2 = WeakEventSubscriptionManager<EventHandler>.SubscribeToWeakEvent<EventSourceClass>(nameof(eventSource.StaticEvent), EventHandler);

        // To explicitly unsubscribe, call Dispose().
        // Even when not explicitly unsubscribed, unsubscribe automatically when the instance of EventListenerClass is garbage-collected.
        unsubscriber2.Dispose();
    }

    public void EventHandler(object? sender, EventArgs e)
    { }
}

class EventSourceClass
{
    public event EventHandler? InstanceEvent;
    public event EventHandler? StaticEvent;
}
```
