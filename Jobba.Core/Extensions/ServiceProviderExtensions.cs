using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Extensions;

public static class ServiceProviderExtensions
{
    public static bool TryGetService<T>(this IServiceProvider provider, out T service)
        where T : class
    {
        try
        {
            service = provider.GetService<T>();
            return true;
        }
        catch
        {
            service = null;
            return false;
        }
    }

    public static bool TryCreateScope(this IServiceScopeFactory factory, out IServiceScope scope)
    {
        try
        {
            scope = factory.CreateScope();
            return true;
        }
        catch
        {
            scope = null;
            return false;
        }
    }

    //todo: write tests for this

    /// <summary>
    /// Given a type that may not be registered with the service provider, tr to resolve all constructor parameters and
    /// return an instance of the type using reflection.
    /// </summary>
    /// <param name="provider">
    /// The service provider.
    /// </param>
    /// <returns>
    /// The materialized type.
    /// </returns>
    public static object Materialize(this IServiceProvider provider, Type type)
    {
        var fromProvider = provider.GetService(type);

        if (fromProvider is not null)
        {
            return fromProvider;
        }

        var constructors = type.GetConstructors().FirstOrDefault();

        if (constructors is null)
        {
            return null;
        }

        var parameters = constructors.GetParameters();
        var parameterInstances = parameters.Select(p => provider.Materialize(p.ParameterType)).ToArray();
        var instance = Activator.CreateInstance(type, parameterInstances);
        return instance;
    }
}
