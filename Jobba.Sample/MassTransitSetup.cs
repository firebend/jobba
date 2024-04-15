using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Jobba.Sample;

public static class MassTransitSetup
{
    public static void ConfigureSerialization(IBusFactoryConfigurator bus)
    {
        bus.UseNewtonsoftJsonSerializer();

        bus.ConfigureNewtonsoftJsonSerializer(x =>
        {
            x.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            x.TypeNameHandling = TypeNameHandling.Objects;
            return x;
        });

        bus.ConfigureNewtonsoftJsonDeserializer(x =>
        {
            x.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            x.TypeNameHandling = TypeNameHandling.Objects;
            return x;
        });
    }

    private static void ConfigurePartition(IBusFactoryConfigurator cfg)
    {
        string path = "fake";

        cfg.UseContextFilter(ctx =>
            Task.FromResult(
                ctx.Headers.TryGetHeader("Partition", out var partition) &&
                partition.ToString() == path));

        cfg.ConfigureSend(sendCfg =>
            sendCfg.UseSendExecute(sendCtx =>
                sendCtx.Headers.Set("Partition", path, true)));

        cfg.ConfigurePublish(pubCfg =>
            pubCfg.UseExecute(pubCtx =>
                pubCtx.Headers.Set("Partition", path, true)));
    }

    //Typical mass transit configuration using rabbit mq
    public static IServiceCollection AddJobbaSampleMassTransit(this IServiceCollection serviceCollection, string connectionString) => serviceCollection
        .AddMassTransit(bus =>
        {
            bus.AddDelayedMessageScheduler();

            bus.UsingRabbitMq((context, configurator) =>
            {
                ConfigureSerialization(configurator);
                ConfigurePartition(configurator);

                configurator.Host(connectionString, h =>
                {
                    h.PublisherConfirmation = true;
                });

                configurator.Lazy = true;
                configurator.AutoDelete = true;
                configurator.PurgeOnStartup = true;
                configurator.UseDelayedMessageScheduler();
                configurator.ConfigureEndpoints(context);
            });
        });
}
