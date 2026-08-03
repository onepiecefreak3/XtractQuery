using System;

namespace CrossCutting.Core.EventBrokerage;

internal sealed class Subscription
{
    public Func<object, bool>? Filter { get; set; }
    public Action<object>? Handler { get; set; }
    public Type? HandlerType { get; set; }
    public Action<object, object>? HandlerWithActivation { get; set; }
}
