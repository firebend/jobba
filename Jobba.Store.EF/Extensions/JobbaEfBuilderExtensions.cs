using System;
using Jobba.Core.Builders;
using Jobba.Store.EF.Builders;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Extensions;

public static class JobbaEfBuilderExtensions
{
    public static JobbaBuilder UsingEf(this JobbaBuilder jobbaBuilder,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsBuilder,
        bool usePooled = false,
        Action<JobbaEfBuilder>? configure = null)
    {

        var builder = new JobbaEfBuilder(jobbaBuilder, dbContextOptionsBuilder, usePooled);
        configure?.Invoke(builder);
        return jobbaBuilder;
    }
}
