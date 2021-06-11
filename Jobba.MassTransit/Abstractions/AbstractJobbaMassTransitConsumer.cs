using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.MassTransit.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Abstractions
{
    public abstract class AbstractJobbaMassTransitConsumer<TMessage, TSubscriber> : IConsumer<TMessage>, IJobbaMassTransitConsumer
        where TMessage : class
    {
        private readonly IServiceProvider _serviceProvider;

        protected AbstractJobbaMassTransitConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public abstract Task HandleMessageAsync(TSubscriber subscriber, TMessage message, CancellationToken cancellationToken);

        protected virtual Task AfterSubscribersAsync(ConsumeContext<TMessage> context) => Task.CompletedTask;

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            using var scope = _serviceProvider.CreateScope();

            var tasks = scope
                .ServiceProvider
                .GetServices<TSubscriber>()
                .Select(x => HandleMessageAsync(x, context.Message, context.CancellationToken));

            await Task.WhenAll(tasks);

            await AfterSubscribersAsync(context);
        }
    }
}
