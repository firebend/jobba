using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Jobba.Web.Sample;

public class CronState
{
    public string Phrase { get; set; }
}

public class CronParameters
{
    public DateTimeOffset StartDate { get; set; }
}

public class SampleCronJobWithParametersAndState : ICronJob<CronParameters, CronState>
{
    private readonly ILogger<SampleCronJobWithParametersAndState> _logger;

    public SampleCronJobWithParametersAndState(ILogger<SampleCronJobWithParametersAndState> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(JobStartContext<CronParameters, CronState> jobStartContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("I'm a little cron job \n {Json}", jobStartContext.ToJson(new JsonWriterSettings { Indent = true }));
        return Task.CompletedTask;
    }

    public string JobName => "Sample Cron Job";
}
