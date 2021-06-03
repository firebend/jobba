using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.MassTransit.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Abstractions
{
    //todo: test
    public abstract class AbstractJobbaMassTransitConsumer<TMessage, TSubscriber> : IConsumer<TMessage>, IJobbaMassTransitConsumer
        where TMessage : class
    {
        private readonly IServiceProvider _serviceProvider;

        protected AbstractJobbaMassTransitConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public abstract Task HandleMessageAsync(TSubscriber subscriber, TMessage message, CancellationToken cancellationToken);

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            using var scope = _serviceProvider.CreateScope();

            var tasks = scope
                .ServiceProvider
                .GetServices<TSubscriber>()
                .Select(x => HandleMessageAsync(x, context.Message, context.CancellationToken));

            await Task.WhenAll(tasks);
        }
    }
}
