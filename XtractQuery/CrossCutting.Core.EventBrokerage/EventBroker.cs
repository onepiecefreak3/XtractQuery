using System;
using System.Collections.Generic;
using System.Linq;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.EventBrokerage.Exceptions;

namespace CrossCutting.Core.EventBrokerage;

public class EventBroker : IEventBroker
{
    private readonly Dictionary<Type, List<Subscription>> _messageSubscriptions;
    private Func<Type, object>? _resolverCallback;

    public EventBroker()
    {
        _messageSubscriptions = new Dictionary<Type, List<Subscription>>();
    }

    public void Subscribe<THandler, TMessage>(Action<THandler, TMessage> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var subscription = new Subscription
        {
            HandlerType = typeof(THandler),
            HandlerWithActivation = (resolvedHandler, message) =>
                handler((THandler)resolvedHandler, (TMessage)message)
        };

        AddSubscription<TMessage>(subscription);
    }

    public void Subscribe<THandler, TMessage>(Func<TMessage, bool> filter, Action<THandler, TMessage> handler)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var subscription = new Subscription
        {
            Filter = message => filter((TMessage)message),
            HandlerType = typeof(THandler),
            HandlerWithActivation = (resolvedHandler, message) =>
                handler((THandler)resolvedHandler, (TMessage)message)
        };

        AddSubscription<TMessage>(subscription);
    }

    public void Subscribe<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var subscription = new Subscription
        {
            Filter = message => filter((TMessage)message),
            Handler = message => handler((TMessage)message)
        };

        AddSubscription<TMessage>(subscription);
    }

    public void Subscribe<TMessage>(Action<TMessage> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var subscription = new Subscription
        {
            Handler = message => handler((TMessage)message)
        };

        AddSubscription<TMessage>(subscription);
    }

    private void AddSubscription<TMessage>(Subscription subscription)
    {
        Type messageType = typeof(TMessage);

        if (!_messageSubscriptions.ContainsKey(messageType))
            _messageSubscriptions[messageType] = [];

        bool isHandlerAlreadyRegistered = _messageSubscriptions[messageType].Any(s =>
            ReferenceEquals(s.Handler, subscription.Handler)
            && ReferenceEquals(s.HandlerWithActivation, subscription.HandlerWithActivation));
        if (isHandlerAlreadyRegistered)
            throw new DuplicatedHandlerException("Handler was already registered");

        _messageSubscriptions[messageType].Add(subscription);
    }

    public void Raise(object message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        Type messageType = message.GetType();
        bool isSomeoneInterested = _messageSubscriptions.ContainsKey(messageType)
                                   && _messageSubscriptions[messageType].Count > 0;
        if (!isSomeoneInterested)
            return;

        List<Subscription> subscriptions = _messageSubscriptions[messageType];

        EnsureResolveCallbackIsSetIfNeeded(subscriptions);

        foreach (Subscription subscription in subscriptions)
            RaiseForSubscription(message, subscription);
    }

    private void EnsureResolveCallbackIsSetIfNeeded(List<Subscription> subscriptions)
    {
        bool hasAnyActivationSubscription = subscriptions.Any(s => s.HandlerType != null);
        bool hasResolveCallbackSet = _resolverCallback != null;
        if (hasAnyActivationSubscription && !hasResolveCallbackSet)
            throw new NoResolveCallbackException("Can't activate handler, no resolve callback set.");
    }

    private void RaiseForSubscription(object message, Subscription subscription)
    {
        try
        {
            if (subscription.Filter is not null && !subscription.Filter(message))
                return;

            if (subscription.HandlerType is not null)
            {
                object handler = _resolverCallback!(subscription.HandlerType);
                subscription.HandlerWithActivation!(handler, message);
            }
            else
            {
                subscription.Handler!(message);
            }
        }
        catch (Exception e)
        {
            throw new EventBrokerageException("Error raising for subscription", e);
        }
    }

    public void SetResolverCallback(Func<Type, object> resolverCallback)
    {
        if (resolverCallback == null)
            throw new ArgumentNullException(nameof(resolverCallback));

        _resolverCallback = resolverCallback;
    }
}
