using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Observables;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Jobba.Web.Sample;

public static class MassTransitSetup
{
    //Typical mass transit configuration using rabbit mq
    public static IServiceCollection AddJobbaSampleMassTransit(this IServiceCollection serviceCollection, string connectionString) => serviceCollection
        .AddMassTransit(bus =>
        {
            bus.AddConsumer<SimpleConsumer>();

            bus.AddDelayedMessageScheduler();

            bus.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(connectionString, h =>
                {
                    h.PublisherConfirmation = true;
                });

                configurator.Lazy = true;
                configurator.AutoDelete = true;
                configurator.PurgeOnStartup = true;
                configurator.UseDelayedMessageScheduler();
                configurator.ConfigureEndpoints(context);
                configurator.ConcurrentMessageLimit = 100;
                configurator.PrefetchCount = 110;

                const string path = "jobba";

                configurator.UseContextFilter(ctx =>
                    Task.FromResult(
                        ctx.Headers.TryGetHeader("Partition", out var partition) &&
                        partition.ToString() == path));

                configurator.ConfigureSend(sendCfg =>
                    sendCfg.UseSendExecute(sendCtx =>
                        sendCtx.Headers.Set("Partition", path, true)));

                configurator.ConfigurePublish(pubCfg =>
                    pubCfg.UseExecute(pubCtx =>
                        pubCtx.Headers.Set("Partition", path, true)));

                configurator.UseNewtonsoftJsonSerializer();

                configurator.ConfigureNewtonsoftJsonSerializer(x =>
                {
                    x.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    x.TypeNameHandling = TypeNameHandling.Objects;
                    return x;
                });

                configurator.ConfigureNewtonsoftJsonDeserializer(x =>
                {
                    x.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    x.TypeNameHandling = TypeNameHandling.Objects;
                    return x;
                });

                configurator.ReceiveEndpoint("simple-test", re =>
                {
                    re.ConfigureConsumer<SimpleConsumer>(context);

                    re.ConcurrentMessageLimit = 100;
                    re.PrefetchCount = 110;

                    re.UseMessageRetry(r => r.Incremental(
                        3,
                        TimeSpan.FromMilliseconds(500),
                        TimeSpan.FromSeconds(1)));

                    re.UseContextFilter(ctx =>
                        Task.FromResult(
                            ctx.Headers.TryGetHeader("Partition", out var partition) &&
                            partition.ToString() == path));

                    re.ConfigureSend(sendCfg =>
                        sendCfg.UseSendExecute(sendCtx => sendCtx.Headers.Set("Partition", path, true)));

                    re.ConfigurePublish(pubCfg =>
                        pubCfg.UseExecute(pubCtx => pubCtx.Headers.Set("Partition", path, true)));
                });

                configurator.ConnectActivityObserver(new ActivityObservable());
            });
        });
}
