using System;
using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Web.Sample
{
    public static class MassTransitSetup
    {
        private static readonly Regex ConStringParser = new(
            "^rabbitmq://([^:]+):(.+)@([^@]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //Typical mass transit configuration using rabbit mq
        public static IServiceCollection AddJobbaSampleMassTransit(this IServiceCollection serviceCollection, string connectionString) => serviceCollection
            .AddMassTransit(bus =>
            {
                bus.AddDelayedMessageScheduler();

                bus.UsingRabbitMq((context, configurator) =>
                {
                    var match = ConStringParser.Match(connectionString);

                    var domain = match.Groups[3].Value;
                    var uri = $"rabbitmq://{domain}";

                    configurator.Host(new Uri(uri), h =>
                    {
                        h.PublisherConfirmation = true;
                        h.Username(match.Groups[1].Value);
                        h.Password(match.Groups[2].Value);
                    });

                    configurator.Lazy = true;
                    configurator.AutoDelete = true;
                    configurator.PurgeOnStartup = true;
                    configurator.UseDelayedMessageScheduler();
                    configurator.ConfigureEndpoints(context);
                });
            });
    }
}
