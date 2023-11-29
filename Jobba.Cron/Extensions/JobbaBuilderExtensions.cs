using System;
using Jobba.Core.Builders;
using Jobba.Cron.Builders;

namespace Jobba.Cron.Extensions;

public static class JobbaBuilderExtensions
{
    /// <summary>
    /// Adds cron capabilities to Jobba
    /// </summary>
    /// <param name="builder">
    /// The Jobba Builder
    /// </param>
    /// <param name="configure">
    /// A callback to configure the cron builder.
    /// </param>
    /// <returns>
    /// The Jobba Builder
    /// </returns>
    public static JobbaBuilder UsingCron(this JobbaBuilder builder, Action<JobbaCronBuilder> configure = null)
    {
        var cronBuilder = new JobbaCronBuilder(builder);
        configure?.Invoke(cronBuilder);

        return builder;
    }
}
