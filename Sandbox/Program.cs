// See https://aka.ms/new-console-template for more information
using Benutomo.WeakEventBroker;

var eventSource = new EventSourceClass();
var eventHandler = new EventHandlerClass();

while (true)
{
    //run(eventSource, eventHandler);
    run(eventSource, null);
    run(null, eventHandler);
}


static void run(EventSourceClass? eventSource, EventHandlerClass? eventHandler)
{
    eventSource ??= new EventSourceClass();
    eventHandler ??= new EventHandlerClass();

    WeakEventSubscriptionManager<EventHandler<EventArgs>>.SubscribeToWeakEvent(eventSource, nameof(eventSource.StandardEventHandler), eventHandler.StandardEventHandler);
    WeakEventSubscriptionManager<EventSourceClass.RefParamEventHander>.SubscribeToWeakEvent(eventSource, nameof(eventSource.RefParamEventHandler), eventHandler.RefParamEventHandler);

    //eventSource.StandardEventHandler += eventHandler.StandardEventHandler;
    //eventSource.RefParamEventHandler += eventHandler.RefParamEventHandler;

    eventSource.CallTest();

    GC.KeepAlive(eventHandler);
}

//class EventListenerClass
//{
//    public EventListenerClass(EventSourceClass eventSource)
//    {
//        // Subscribe to instance member events by weak reference
//        var unsubscriber1 = WeakEventSubscriptionManager<EventHandler>.SubscribeToWeakEvent(eventSource, nameof(eventSource.InstanceEvent), EventHandler);

//        // To explicitly unsubscribe, call Dispose().
//        // Even when not explicitly unsubscribed, unsubscribe automatically when the instance of EventListenerClass is garbage-collected.
//        unsubscriber1.Dispose();


//        // Subscribe to static member events by weak reference
//        var unsubscriber2 = WeakEventSubscriptionManager<EventHandler>.SubscribeToWeakEvent<EventSourceClass>(nameof(eventSource.StaticEvent), EventHandler);

//        // To explicitly unsubscribe, call Dispose().
//        // Even when not explicitly unsubscribed, unsubscribe automatically when the instance of EventListenerClass is garbage-collected.
//        unsubscriber2.Dispose();
//    }

//    public void EventHandler(object? sender, EventArgs e)
//    { }
//}

//class EventSourceClass
//{
//    public event EventHandler? InstanceEvent;
//    public event EventHandler? StaticEvent;
//}


internal class EventSourceClass
{
    private byte[] _buffer = new byte[10_000];

    public event EventHandler<EventArgs>? StandardEventHandler;
    public event RefParamEventHander? RefParamEventHandler;

    public delegate void RefParamEventHander(long a, ref long b, in long c, ref readonly long d, out long e);

    private static long _count;

    public EventSourceClass()
    {
        Interlocked.Increment(ref _count);
    }

    ~EventSourceClass()
    {

        Interlocked.Decrement(ref _count);
    }

    public void CallTest()
    {
        {
            var sender = "senderObject";
            //Console.WriteLine($"StandardEventHandler(sender: {sender})");
            StandardEventHandler?.Invoke(sender, EventArgs.Empty);
            //Console.WriteLine();
        }

        if (RefParamEventHandler is { } refParamEventHandler)
        {
            long a = 1;
            long b = 2;
            long c = 3;
            long d = 4;
            long e;
            //Console.WriteLine($"RefParamEventHandler(a: {a}, b: {b}, c: {c}, d: {d}, e)");
            refParamEventHandler(a, ref b, c, ref d, out e);
            //Console.WriteLine($"{nameof(a)}: {a}");
            //Console.WriteLine($"{nameof(b)}: {b}");
            //Console.WriteLine($"{nameof(c)}: {c}");
            //Console.WriteLine($"{nameof(d)}: {d}");
            //Console.WriteLine($"{nameof(e)}: {e}");
            //Console.WriteLine();
        }
    }
}

internal class EventHandlerClass
{
    private byte[] _buffer = new byte[10_000];

    private static long _count;

    public EventHandlerClass()
    {
        Interlocked.Increment(ref _count);
    }

    ~EventHandlerClass()
    {

        Interlocked.Decrement(ref _count);
    }

    public void StandardEventHandler(object? sender, EventArgs e)
    {
        //Console.WriteLine($"EventHandlerClass.StandardEventHandler[{nameof(sender)} => {sender}");
    }

    public string FuncEventHandler(string value)
    {
        //Console.WriteLine($"EventHandlerClass.FuncEventHandler[{nameof(value)} => {value}");
        return $"{value}x{value}";
    }

    public void RefParamEventHandler(long a, ref long b, in long c, ref readonly long d, out long e)
    {
        //Console.WriteLine($"EventHandlerClass.RefParamEventHandler[{nameof(a)} => {a}");
        //Console.WriteLine($"EventHandlerClass.RefParamEventHandler[{nameof(b)} => {b}");
        //Console.WriteLine($"EventHandlerClass.RefParamEventHandler[{nameof(c)} => {c}");
        //Console.WriteLine($"EventHandlerClass.RefParamEventHandler[{nameof(d)} => {d}");

        e = a + b + c + d;
        //Console.WriteLine($"EventHandlerClass.RefParamEventHandler[{nameof(e)} <= {e}");

        b = b * b;
        //Console.WriteLine($"EventHandlerClass.RefParamEventHandler[{nameof(b)} <= {b}");
    }
}