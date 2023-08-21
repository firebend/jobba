using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.MassTransit.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Abstractions;

public abstract class AbstractJobbaMassTransitConsumer<TMessage, TSubscriber> : IConsumer<TMessage>, IJobbaMassTransitConsumer, IDisposable
    where TMessage : class
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected AbstractJobbaMassTransitConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task Consume(ConsumeContext<TMessage> context)
    {
        if (_scopeFactory.TryCreateScope(out var scope))
        {
            using (scope)
            {
                var tasks = scope
                    .ServiceProvider
                    .GetServices<TSubscriber>()
                    .Select(x => HandleMessageAsync(x, context.Message, context.CancellationToken));

                await Task.WhenAll(tasks);

                await AfterSubscribersAsync(context);
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected abstract Task HandleMessageAsync(TSubscriber subscriber, TMessage message, CancellationToken cancellationToken);

    protected virtual Task AfterSubscribersAsync(ConsumeContext<TMessage> context) => Task.CompletedTask;
}
