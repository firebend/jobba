using System;
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
}
