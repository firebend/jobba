using System;
using System.Collections.Generic;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Jobba.Tests.AutoMoqCustomizations;

public class ServiceProviderCustomization : ICustomization
{
    private readonly IDictionary<Type, object> _resolves;

    public ServiceProviderCustomization(IDictionary<Type, object> resolves)
    {
        _resolves = resolves;
    }

    public void Customize(IFixture fixture)
    {
        var serviceProvider = fixture.Freeze<Mock<IServiceProvider>>();
        var serviceScope = fixture.Freeze<Mock<IServiceScope>>();
        var serviceScopeFactory = fixture.Freeze<Mock<IServiceScopeFactory>>();

        serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        serviceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);

        serviceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);

        foreach (var (key, value) in _resolves)
        {
            serviceProvider.Setup(x => x.GetService(key)).Returns(value);
        }
    }
}
