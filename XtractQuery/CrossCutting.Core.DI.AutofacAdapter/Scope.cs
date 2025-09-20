using System;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Castle.Core.Logging;
using CrossCutting.Core.Contract.DependencyInjection;

namespace CrossCutting.Core.DI.AutofacAdapter;

public class Scope : IScope
{
    private readonly ILifetimeScope _scope;
    private readonly ILogger _logger;
    public event EventHandler<ResolveRequestEventArgs> ResolveRequest;

    public Scope(ILifetimeScope scope) : this(scope, null)
    {
    }

    public Scope(ILifetimeScope scope, ILogger logger)
    {
        _scope = scope;
        _logger = logger;

        if (_logger?.IsDebugEnabled ?? false)
        {
            _scope.ResolveOperationBeginning += Scope_ResolveOperationBeginning;
            _scope.CurrentScopeEnding += Scope_CurrentScopeEnding;
        }

        _scope.ResolveOperationBeginning += (s, e) =>
        {
            e.ResolveOperation.ResolveRequestBeginning += (s, e) =>
            {
                ResolveRequest?.Invoke(this, new ResolveRequestEventArgs(
                    (e.RequestContext.Service as TypedService)?.ServiceType,
                    e.RequestContext.Registration.Activator.LimitType,
                    new RequestContext(e.RequestContext))
                );
            };
        };
    }

    public TContract Get<TContract>()
        where TContract : class
    {
        return Get(typeof(TContract)) as TContract;
    }

    public object Get(Type contractType)
    {
        return _scope.Resolve(contractType);
    }

    public string GetHash()
    {
        return RuntimeHelpers.GetHashCode(_scope).ToString();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    #region Logging

    private void Scope_ResolveOperationBeginning(object sender, Autofac.Core.Resolving.ResolveOperationBeginningEventArgs e)
    {
        e.ResolveOperation.ResolveRequestBeginning += ResolveOperation_ResolveRequestBeginning;
        e.ResolveOperation.CurrentOperationEnding += ResolveOperation_CurrentOperationEnding;

        ISharingLifetimeScope scope = e.ResolveOperation.CurrentScope;
        string scopeHash = scope.Equals(scope.RootLifetimeScope) ? "RootLifetimeScope" : GetHash();
        _logger.Debug($"Scope ({scopeHash}) resolve operation beginning.");
    }

    private void ResolveOperation_CurrentOperationEnding(object sender, Autofac.Core.Resolving.ResolveOperationEndingEventArgs e)
    {
        ISharingLifetimeScope scope = e.ResolveOperation.CurrentScope;
        string scopeHash = scope.Equals(scope.RootLifetimeScope) ? "RootLifetimeScope" : GetHash();
        _logger.Debug($"Scope ({scopeHash}) resolve operation completed.");
    }

    private void ResolveOperation_ResolveRequestBeginning(object sender, Autofac.Core.Resolving.ResolveRequestBeginningEventArgs e)
    {
        e.RequestContext.RequestCompleting += RequestContext_RequestCompleting;

        ISharingLifetimeScope scope = e.RequestContext.ActivationScope;
        string scopeHash = scope.Equals(scope.RootLifetimeScope) ? "RootLifetimeScope" : GetHash();
        string serviceName = (e.RequestContext.Service as TypedService)?.ServiceType.Name ?? e.RequestContext.Service.Description;
        _logger.Debug($"Scope ({scopeHash}) resolve request beginning. (Service: {serviceName})");
    }

    private void RequestContext_RequestCompleting(object sender, Autofac.Core.Resolving.ResolveRequestCompletingEventArgs e)
    {
        ISharingLifetimeScope scope = e.RequestContext.ActivationScope;
        string scopeHash = scope.Equals(scope.RootLifetimeScope) ? "RootLifetimeScope" : GetHash();
        string serviceName = (e.RequestContext.Service as TypedService)?.ServiceType.Name ?? e.RequestContext.Service.Description;
        _logger.Debug($"Scope ({scopeHash}) resolve request completed. Use existing instance: {!e.RequestContext.NewInstanceActivated}  (Service: {serviceName})");
    }

    private void Scope_CurrentScopeEnding(object sender, LifetimeScopeEndingEventArgs e)
    {
        _logger.Debug($"Scope ({GetHash()}) ended.");
    }

    #endregion
}