using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Benutomo.WeakEventBroker;

/// <summary>
/// 弱い参照によるイベント購読の補助
/// </summary>
/// <typeparam name="DelegateT">イベントハンドラのデリゲート型</typeparam>
public class WeakEventSubscriptionManager<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] DelegateT> where DelegateT : Delegate
{
    // DelegeteTはNativeAOTなどが有効になっている環境でも、必要な型情報がトリムされないようにするために必要

    /// <summary>
    /// 弱い参照でstaticメンバのイベントを購読する。
    /// </summary>
    /// <typeparam name="T">購読するイベントを含む型</typeparam>
    /// <param name="eventName">購読するイベント名</param>
    /// <param name="action">イベントハンドラ(イベントハンドラが元々保持してるオブジェクトへの参照は弱い参照に置き換えられる)</param>
    /// <returns>購読解除を明示的に行う<see cref="IDisposable"/></returns>
    public static IDisposable SubscribeToWeakEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]  T>(string eventName, DelegateT action) where T : class
    {
        return SubscribeToWeakEventCore<T>(eventSourceObject: null, eventName, action);
    }

    /// <summary>
    /// 弱い参照でstaticメンバのイベントを購読する。
    /// </summary>
    /// <typeparam name="T">購読するイベントを含む型</typeparam>
    /// <param name="eventSourceObject">購読するイベントを含むオブジェクト</param>
    /// <param name="eventName">購読するイベント名</param>
    /// <param name="action">イベントハンドラ(イベントハンドラが元々保持してるオブジェクトへの参照は弱い参照に置き換えられる)</param>
    /// <returns>購読解除を明示的に行う<see cref="IDisposable"/></returns>
    public static IDisposable SubscribeToWeakEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]  T>(T eventSourceObject, string eventName, DelegateT action) where T : class
    {
        return SubscribeToWeakEventCore<T>(eventSourceObject, eventName, action);
    }


    /// <summary>
    /// 弱い参照でstaticメンバのイベントを購読する。
    /// </summary>
    /// <typeparam name="T">購読するイベントを含む型</typeparam>
    /// <param name="eventName">購読するイベント名</param>
    /// <param name="action">イベントハンドラ(イベントハンドラが元々保持してるオブジェクトへの参照は弱い参照に置き換えられる)。メソッドシグネチャが同じであれば<paramref name="eventName"/>の型と異なるデリゲート型でも指定可能。</param>
    /// <returns>購読解除を明示的に行う<see cref="IDisposable"/></returns>
    public static IDisposable SubscribeToWeakEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] T>(string eventName, Delegate action) where T : class
    {
        return SubscribeToWeakEventCore<T>(eventSourceObject: null, eventName, action);
    }

    /// <summary>
    /// 弱い参照でstaticメンバのイベントを購読する。
    /// </summary>
    /// <typeparam name="T">購読するイベントを含む型</typeparam>
    /// <param name="eventSourceObject">購読するイベントを含むオブジェクト</param>
    /// <param name="eventName">購読するイベント名</param>
    /// <param name="action">イベントハンドラ(イベントハンドラが元々保持してるオブジェクトへの参照は弱い参照に置き換えられる)。メソッドシグネチャが同じであれば<paramref name="eventName"/>の型と異なるデリゲート型でも指定可能。</param>
    /// <returns>購読解除を明示的に行う<see cref="IDisposable"/></returns>
    public static IDisposable SubscribeToWeakEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] T>(T eventSourceObject, string eventName, Delegate action) where T : class
    {
        return SubscribeToWeakEventCore<T>(eventSourceObject, eventName, action);
    }

    private static IDisposable SubscribeToWeakEventCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] T>(object? eventSourceObject, string eventName, Delegate action)
    {
        var eventInfo = eventSourceObject is null
            ? typeof(T).GetEvent(eventName, BindingFlags.Static | BindingFlags.Public)
            : typeof(T).GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);

        if (eventInfo is null)
        {
            throw new ArgumentException($"EventNotFound: `{eventName}` is not exists in {(eventSourceObject is null ? "static": "instance")} event members of {typeof(T)}.", nameof(eventName));
        }

        if (typeof(DelegateT) != eventInfo.EventHandlerType)
        {
            throw new ArgumentException($"EventHandlerTypeMismatch {nameof(DelegateT)}:{{{typeof(DelegateT).FullName}}} {{{eventName}:{eventInfo.EventHandlerType?.FullName}}}", nameof(eventName));
        }

        var eventMethodInfo = typeof(DelegateT).GetMethod("Invoke");

        if (eventMethodInfo is null)
        {
            throw new ArgumentException("InvalidDelegateType", nameof(DelegateT));
        }

        if (eventMethodInfo.ReturnType != typeof(void))
        {
            throw new ArgumentException("UnsupportedDelegateType: Weak reference events cannot have a return value.", nameof(DelegateT));
        }

        if (eventMethodInfo.GetParameters().Any(v => v.IsOut))
        {
            throw new ArgumentException("UnsupportedDelegateType: Weak reference events cannot have a out parameter.", nameof(DelegateT));
        }

        if (!IsCompatibleDelegate(action))
        {
            throw new ArgumentException($"IncompatibleDelegate: Event and event handler method signatures are incompatible.", nameof(action));
        }

        var invocationList = action.GetInvocationList();

        if (invocationList.Length > 1)
        {
            for (int i = 0; i < invocationList.Length; i++)
            {
                Delegate? singleDelegate = invocationList[i];
                if (!IsCompatibleDelegate(singleDelegate))
                {
                    throw new ArgumentException($"IncompatibleDelegate: The event handler (MulticastDelegate) contains a delegate that is incompatible with the event and method signatures.", nameof(action));
                }
            }
        }

        var disposables = new IDisposable[invocationList.Length];

        var disposable = new CompositDisposable(disposables);

        try
        {
            for (int i = 0; i < invocationList.Length; i++)
            {
                Delegate? singleDelegate = invocationList[i];
                disposables[i] = EventListeningProxy.Listen(eventSourceObject, eventInfo, singleDelegate);
            }
        }
        catch (Exception)
        {
            disposable.Dispose();
            throw;
        }

        return disposable;

        static bool IsCompatibleDelegate(Delegate eventHandler)
        {
            if (typeof(DelegateT) == eventHandler.GetType())
                return true;

            var invokeMethodInfo = typeof(DelegateT).GetMethod("Invoke");

            if (invokeMethodInfo is null)
                return false;

            if (!IsCompatibleParameter(invokeMethodInfo.ReturnParameter, eventHandler.Method.ReturnParameter))
                return false;

            var parameters1 = invokeMethodInfo.GetParameters();
            var parameters2 = eventHandler.Method.GetParameters();

            if (parameters1.Length != parameters2.Length)
                return false;

            for (int i = 0; i < parameters1.Length; i++)
            {
                if (!IsCompatibleParameter(parameters1[i], parameters2[i]))
                    return false;
            }

            return true;
        }

        static bool IsCompatibleParameter(ParameterInfo callerParameterInfo, ParameterInfo calleeParameterInfo)
        {
            if (callerParameterInfo.ParameterType != calleeParameterInfo.ParameterType)
                return false;

            if (callerParameterInfo.IsOut != calleeParameterInfo.IsOut)
                return false;

            if (callerParameterInfo.IsIn && !calleeParameterInfo.IsIn)
            {
                // 呼び元がinパラメータのとき呼出し先で
                // 書き換えられないことを保証出来るように
                // 呼び先もinパラメータが必要。
                // 常に防御的コピーが発生するならば必要ないがランタイムで保証される動作であるか不明。
                return false;
            }

            return true;
        }
    }

    private class CompositDisposable : IDisposable
    {
        private IDisposable?[]? _disposables;

        public CompositDisposable(IDisposable?[] disposables)
        {
            _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposables, null) is { } disposables)
            {
                foreach (var disposable in disposables)
                {
                    disposable?.Dispose();
                }
            }
        }
    }

    private class EventListeningProxy : IDisposable
    {
        object? _eventSource;

        EventInfo _eventInfo;

        WeakReference _eventListener;

        DelegateT? _registeredEventHandler;

        private EventListeningProxy(object? eventSource, EventInfo eventInfo, Delegate eventHandler)
        {
            _eventSource = eventSource;
            _eventInfo = eventInfo;

            if (eventHandler.Target is null)
            {
                throw new ArgumentException($"インスタンスメソッドに対するデリゲートではありません。", nameof(eventHandler));
            }

            if (eventSource is not null)
            {
                if (eventInfo.DeclaringType?.IsAssignableFrom(eventSource.GetType()) != true)
                {
                    throw new ArgumentException($"インスタンスに対するイベントではありません。", nameof(eventInfo));
                }
            }

            _eventListener = new WeakReference(eventHandler.Target);

            var weakReferenceTargetPropInfo = typeof(WeakReference).GetProperty(nameof(WeakReference.Target));

            if (weakReferenceTargetPropInfo is null)
            {
                throw new InvalidOperationException($"{nameof(WeakReference)}の{nameof(WeakReference.Target)}プロパティが見つかりません。");
            }

            var unregisterMethodInfo = typeof(EventListeningProxy).GetMethod(nameof(Dispose));

            if (unregisterMethodInfo is null)
            {
                throw new InvalidOperationException($"{nameof(EventListeningProxy.Dispose)}メソッドが見つかりません。");
            }

            var parameters = eventHandler.GetMethodInfo().GetParameters()
                .Select(v => Expression.Parameter(v.ParameterType, v.Name))
                .ToArray();

            var eventListnerWeakReferenceConstant = Expression.Constant(_eventListener, typeof(WeakReference));

            var unregisterObjectConstant = Expression.Constant(this, typeof(EventListeningProxy));

            var eventListnerVariable = Expression.Variable(eventHandler.Target.GetType(), "eventListner");

            // {
            //    var eventListener = (TypeOfEventListnerTarget)_eventListener.Target;
            //    if (eventListener is null)
            //    {
            //       this.Dispose(); // _eventSource.XxxEvent -= _delegate;
            //    }
            //    else
            //    {
            //       eventHandler.Method.Invoke(eventListener, ...args);
            //    }
            // }
            var eventHandlerMethodBlock = Expression.Block(
                [eventListnerVariable],
                Expression.Assign(eventListnerVariable, Expression.Convert(Expression.Property(eventListnerWeakReferenceConstant, weakReferenceTargetPropInfo), eventHandler.Target.GetType())),
                Expression.IfThenElse(Expression.ReferenceEqual(eventListnerVariable, Expression.Default(eventHandler.Target.GetType())),
                    ifTrue: Expression.Call(unregisterObjectConstant, unregisterMethodInfo),
                    ifFalse: Expression.Call(eventListnerVariable, eventHandler.Method, parameters)
                    )
                );

            _registeredEventHandler = (DelegateT)Expression.Lambda(typeof(DelegateT), eventHandlerMethodBlock, parameters).Compile();

            eventInfo.AddMethod?.Invoke(eventSource, [_registeredEventHandler]);
        }

        public static EventListeningProxy Listen(object? eventSource, EventInfo eventInfo, Delegate eventHandler)
        {
            return new EventListeningProxy(eventSource, eventInfo, eventHandler);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _registeredEventHandler, null) is { } registeredEventHandler)
            {
                _eventInfo.RemoveMethod?.Invoke(_eventSource, [registeredEventHandler]);
            }
        }
    }
}