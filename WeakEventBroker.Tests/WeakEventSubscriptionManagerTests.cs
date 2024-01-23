using Benutomo.WeakEventBroker;

namespace WeakEventBroker.Tests;

public class WeakEventSubscriptionManagerTests
{
    [Fact]
    public void SameDelegateTypeInstanceEventTest()
    {
        var eventSource = new EventSourceClass();

        Run(eventSource);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        eventSource.InvokeInstanceEvent(eventSource, EventArgs.Empty);

        Assert.Empty(eventSource.InstanceEventHandlers);

        static void Run(EventSourceClass eventSource)
        {
            object? callbackSource = null;

            Assert.Empty(eventSource.InstanceEventHandlers);

            var handler = new SameDelegateTypeInstanceEventHandler(eventSource, (s, _) => callbackSource = s);

            Assert.Single(eventSource.InstanceEventHandlers);

            eventSource.InvokeInstanceEvent(eventSource, EventArgs.Empty);

            Assert.NotNull(callbackSource);
            Assert.Equal(eventSource, callbackSource);

            GC.KeepAlive(handler);
        }
    }

    [Fact]
    public void SameDelegateTypeStaticEventTest()
    {
        var dummyEventSource = new object();
        Run(dummyEventSource);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        EventSourceClass.InvokeStaticEvent(dummyEventSource, EventArgs.Empty);

        Assert.Empty(EventSourceClass.StaticEventHandlers);

        static void Run(object dummyEventSource)
        {
            object? callbackSource = null;

            Assert.Empty(EventSourceClass.StaticEventHandlers);

            var handler = new SameDelegateTypeStaticEventHandler((s, _) => callbackSource = s);

            Assert.Single(EventSourceClass.StaticEventHandlers);

            EventSourceClass.InvokeStaticEvent(dummyEventSource, EventArgs.Empty);

            Assert.NotNull(callbackSource);
            Assert.Equal(dummyEventSource, callbackSource);

            GC.KeepAlive(handler);
        }
    }

    [Fact]
    public void CompatibleSignatureInstanceEventTest()
    {
        var eventSource = new EventSourceClass();

        Run(eventSource);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        eventSource.InvokeInstanceEvent(eventSource, EventArgs.Empty);

        Assert.Empty(eventSource.InstanceEventHandlers);

        static void Run(EventSourceClass eventSource)
        {
            object? callbackSource = null;

            Assert.Empty(eventSource.InstanceEventHandlers);

            var handler = new CompatibleSignatureInstanceEventHandler(eventSource, (s, _) => callbackSource = s);

            Assert.Single(eventSource.InstanceEventHandlers);

            eventSource.InvokeInstanceEvent(eventSource, EventArgs.Empty);

            Assert.NotNull(callbackSource);
            Assert.Equal(eventSource, callbackSource);

            GC.KeepAlive(handler);
        }
    }

    [Fact]
    public void CompatibleSignatureStaticEventTest()
    {
        var dummyEventSource = new object();
        Run(dummyEventSource);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        EventSourceClass.InvokeStaticEvent(dummyEventSource, EventArgs.Empty);

        Assert.Empty(EventSourceClass.StaticEventHandlers);

        static void Run(object dummyEventSource)
        {
            object? callbackSource = null;

            Assert.Empty(EventSourceClass.StaticEventHandlers);

            var handler = new CompatibleSignatureStaticEventHandler((s, _) => callbackSource = s);

            Assert.Single(EventSourceClass.StaticEventHandlers);

            EventSourceClass.InvokeStaticEvent(dummyEventSource, EventArgs.Empty);

            Assert.NotNull(callbackSource);
            Assert.Equal(dummyEventSource, callbackSource);

            GC.KeepAlive(handler);
        }
    }

    [Fact]
    public void InstanceEventImplicitUnsubscribeTest()
    {
        var eventSource = new EventSourceClass();

        Assert.Empty(eventSource.InstanceEventHandlers);

        using (var handler = new SameDelegateTypeInstanceEventHandler(eventSource, (s, _) => { }))
        {
            Assert.Single(eventSource.InstanceEventHandlers);
        }

        Assert.Empty(eventSource.InstanceEventHandlers);
    }

    [Fact]
    public void StaticEventImplicitUnsubscribeTest()
    {
        var eventSource = new EventSourceClass();

        Assert.Empty(EventSourceClass.StaticEventHandlers);

        using (var handler = new SameDelegateTypeStaticEventHandler((s, _) => { }))
        {
            Assert.Single(EventSourceClass.StaticEventHandlers);
        }

        Assert.Empty(EventSourceClass.StaticEventHandlers);
    }

    [Fact]
    public void FuncEventTest()
    {
        try
        {
            WeakEventSubscriptionManager<Func<int>>.SubscribeToWeakEvent<EventSourceClass>(nameof(EventSourceClass.FuncEvent), () => 0);
            Assert.Fail("Exception not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.StartsWith("UnsupportedDelegateType: Weak reference events cannot have a return value.", ex.Message);
            Assert.Equal("DelegateT", ex.ParamName);
        }
    }

    [Fact]
    public void OutParamEventTest()
    {
        try
        {
            WeakEventSubscriptionManager<EventSourceClass.OutParamDelegate>.SubscribeToWeakEvent<EventSourceClass>(nameof(EventSourceClass.OutParamEvent), (int n, out int outValue) => outValue = n);
            Assert.Fail("Exception not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.StartsWith("UnsupportedDelegateType: Weak reference events cannot have a out parameter.", ex.Message);
            Assert.Equal("DelegateT", ex.ParamName);
        }
    }

    [Fact]
    public void IncompatibleEventHandlerTest()
    {
        try
        {
            WeakEventSubscriptionManager<Action<int>>.SubscribeToWeakEvent<EventSourceClass>(nameof(EventSourceClass.IntActionEvent), (in int outValue) => { });
            Assert.Fail("Exception not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.StartsWith("IncompatibleDelegate: Event and event handler method signatures are incompatible.", ex.Message);
            Assert.Equal("action", ex.ParamName);
        }
    }

    [Fact]
    public void MissingInstanceEventTest()
    {
        try
        {
            var eventSource = new EventSourceClass();
            WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent(eventSource, nameof(EventSourceClass.StaticEvent), (in int outValue) => { });
            Assert.Fail("Exception not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.StartsWith($"EventNotFound: `StaticEvent` is not exists in instance event members of WeakEventBroker.Tests.{nameof(WeakEventSubscriptionManagerTests)}+{nameof(EventSourceClass)}.", ex.Message);
            Assert.Equal("eventName", ex.ParamName);
        }
    }

    [Fact]
    public void MissingStaticEventTest()
    {
        try
        {
            WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent<EventSourceClass>(nameof(EventSourceClass.InstanceEvent), (in int outValue) => { });
            Assert.Fail("Exception not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.StartsWith($"EventNotFound: `InstanceEvent` is not exists in static event members of WeakEventBroker.Tests.{nameof(WeakEventSubscriptionManagerTests)}+{nameof(EventSourceClass)}.", ex.Message);
            Assert.Equal("eventName", ex.ParamName);
        }
    }

    class EventSourceClass
    {
        public event EventHandler<EventArgs> InstanceEvent
        {
            add => InstanceEventHandlers.Add(value);
            remove => InstanceEventHandlers.Remove(value);
        }

        public static event EventHandler<EventArgs> StaticEvent
        {
            add => StaticEventHandlers.Add(value);
            remove => StaticEventHandlers.Remove(value);
        }

        public static event Func<int>? FuncEvent;

        public delegate void OutParamDelegate(int n, out int outValue);
        public static event OutParamDelegate? OutParamEvent;

        public static event Action<int>? IntActionEvent;


        public List<EventHandler<EventArgs>> InstanceEventHandlers
        {
            get;
            set;
        } = new List<EventHandler<EventArgs>>();

        public static List<EventHandler<EventArgs>> StaticEventHandlers
        {
            get;
            set;
        } = new List<EventHandler<EventArgs>>();

        public void InvokeInstanceEvent(object? sender, EventArgs e)
        {
            foreach (var handler in InstanceEventHandlers.ToArray())
            {
                handler(sender, e);
            }
        }

        public static void InvokeStaticEvent(object? sender, EventArgs e)
        {
            foreach (var handler in StaticEventHandlers.ToArray())
            {
                handler(sender, e);
            }
        }
    }

    class SameDelegateTypeInstanceEventHandler : IDisposable
    {
        EventHandler<EventArgs> _callback;

        IDisposable _unsubscriber;

        public SameDelegateTypeInstanceEventHandler(EventSourceClass source, EventHandler<EventArgs> callback)
        {
            _unsubscriber = WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent(source, nameof(source.InstanceEvent), (EventHandler<EventArgs>)Source_InstanceEvent);
            _callback = callback;
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
        }

        private void Source_InstanceEvent(object? sender, EventArgs e)
        {
            _callback(sender, e);
        }
    }

    class SameDelegateTypeStaticEventHandler : IDisposable
    {
        EventHandler<EventArgs> _callback;

        IDisposable _unsubscriber;

        public SameDelegateTypeStaticEventHandler(EventHandler<EventArgs> callback)
        {
            _unsubscriber = WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent<EventSourceClass>(nameof(EventSourceClass.StaticEvent), (EventHandler<EventArgs>)Source_StaticEvent);
            _callback = callback;
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
        }

        private void Source_StaticEvent(object? sender, EventArgs e)
        {
            _callback(sender, e);
        }
    }

    class CompatibleSignatureInstanceEventHandler
    {
        EventHandler<EventArgs> _callback;

        public CompatibleSignatureInstanceEventHandler(EventSourceClass source, EventHandler<EventArgs> callback)
        {
            WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent(source, nameof(source.InstanceEvent), (Action<object?, EventArgs>)Source_InstanceEvent);
            _callback = callback;
        }

        private void Source_InstanceEvent(object? sender, EventArgs e)
        {
            _callback(sender, e);
        }
    }

    class CompatibleSignatureStaticEventHandler
    {
        EventHandler<EventArgs> _callback;

        public CompatibleSignatureStaticEventHandler(EventHandler<EventArgs> callback)
        {
            WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent<EventSourceClass>(nameof(EventSourceClass.StaticEvent), (Action<object?, EventArgs>)Source_StaticEvent);
            _callback = callback;
        }

        private void Source_StaticEvent(object? sender, EventArgs e)
        {
            _callback(sender, e);
        }
    }
}