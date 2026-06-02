using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Core.Implementations;

[TestClass]
public class DefaultJobEventPublisherTests
{
    [TestMethod]
    public async Task Publisher_Scope_Should_Be_Alive_When_Subscriber_Executes()
    {
        var executed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var services = new ServiceCollection();
        services.AddScoped<ScopedDependency>();
        services.AddScoped<IOnJobCompletedSubscriber<TestModels.FooJob>>(_ => new ScopedAwareSubscriber(
            _.GetRequiredService<ScopedDependency>(), executed));

        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var publisher = new DefaultJobEventPublisher(NullLogger<DefaultJobEventPublisher>.Instance, scopeFactory);

        var evt = new JobCompletedEvent<TestModels.FooJob>(Guid.NewGuid(), Guid.NewGuid());
        _ = publisher.PublishJobCompletedEventAsync(evt, CancellationToken.None);

        var wasScopedDependencyDisposed = await executed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        wasScopedDependencyDisposed.Should().BeFalse(
            "the scope must remain alive for the entire duration of subscriber execution");
    }

    private sealed class ScopedDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() { IsDisposed = true; }
    }

    private sealed class ScopedAwareSubscriber : IOnJobCompletedSubscriber<TestModels.FooJob>
    {
        private readonly ScopedDependency _dep;
        private readonly TaskCompletionSource<bool> _executed;

        public ScopedAwareSubscriber(ScopedDependency dep, TaskCompletionSource<bool> executed)
        {
            _dep = dep;
            _executed = executed;
        }

        public Task OnJobCompletedAsync(JobCompletedEvent<TestModels.FooJob> jobCompletedEvent, CancellationToken cancellationToken)
        {
            _executed.TrySetResult(_dep.IsDisposed);
            return Task.CompletedTask;
        }
    }
}
