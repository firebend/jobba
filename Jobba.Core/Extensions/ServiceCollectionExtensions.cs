using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterReplace<TService, TImpl>(this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TImpl : TService
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(TService), typeof(TImpl), lifetime);
        serviceCollection.Replace(serviceDescriptor);
        return serviceCollection;
    }

    public static IServiceCollection RegisterReplace<TService>(this IServiceCollection serviceCollection, TService service)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(TService), service);
        serviceCollection.Replace(serviceDescriptor);
        return serviceCollection;
    }
}
