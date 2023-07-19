using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.MassTransit.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Abstractions;

public abstract class AbstractJobbaMassTransitConsumer<TMessage, TSubscriber> : IConsumer<TMessage>, IJobbaMassTransitConsumer, IDisposable
    where TMessage : class
{
    private readonly IServiceScope _serviceScope;

    protected AbstractJobbaMassTransitConsumer(IServiceProvider serviceProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
    }

    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var tasks = _serviceScope
            .ServiceProvider
            .GetServices<TSubscriber>()
            .Select(x => HandleMessageAsync(x, context.Message, context.CancellationToken));

        await Task.WhenAll(tasks);

        await AfterSubscribersAsync(context);
    }

    public void Dispose() => _serviceScope?.Dispose();

    protected abstract Task HandleMessageAsync(TSubscriber subscriber, TMessage message, CancellationToken cancellationToken);

    protected virtual Task AfterSubscribersAsync(ConsumeContext<TMessage> context) => Task.CompletedTask;
}
