using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Cron.Abstractions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Jobba.Web.Sample;

public class CronState : IJobState
{
    public string Phrase { get; set; }
}

public class CronParameters : IJobParams
{
    public DateTimeOffset StartDate { get; set; }
}

public class SampleCronJobWithParametersAndState : AbstractCronJobBaseClass<CronParameters, CronState>
{
    private readonly ILogger<SampleCronJobWithParametersAndState> _logger;

    public SampleCronJobWithParametersAndState(IJobProgressStore progressStore, ILogger<SampleCronJobWithParametersAndState> logger) : base(progressStore)
    {
        _logger = logger;
    }

    public override string JobName => "Sample Cron Job";

    protected override Task OnStartAsync(JobStartContext<CronParameters, CronState> jobStartContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("I'm a little cron job \n {Json}", jobStartContext.ToJson(new JsonWriterSettings { Indent = true }));
        return Task.CompletedTask;
    }
}
