using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Jobba.Web.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class SampleFaultJobController : ControllerBase
{
    private readonly IJobScheduler _jobScheduler;
    private readonly IJobStore _jobStore;

    public SampleFaultJobController(IJobScheduler jobScheduler, IJobStore jobStore)
    {
        _jobScheduler = jobScheduler;
        _jobStore = jobStore;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJobAsync(CancellationToken cancellationToken)
    {
        var request = new JobRequest<SampleFaultWebJobParameters, SampleFaultWebJobState>
        {
            Description = "A Sample Job that should fault",
            JobParameters = new SampleFaultWebJobParameters { Greeting = "Hello" },
            JobType = typeof(SampleFaultWebJob),
            InitialJobState = new SampleFaultWebJobState { Tries = 1 },
            JobWatchInterval = TimeSpan.FromSeconds(10),
            MaxNumberOfTries = 5,
            JobName = "sample-faulted-job"
        };

        var job = await _jobScheduler.ScheduleJobAsync(request, cancellationToken);

        return Ok(job);
    }

    [HttpPost("{id:guid}/fault")]
    public IActionResult FaultAsync()
    {
        SampleFaultWebJobFaultContext.ShouldFault(true);
        return Ok();
    }

    [HttpPost("{id:guid}/run")]
    public IActionResult RunAsync()
    {
        SampleFaultWebJobFaultContext.ShouldFault(false);
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobStore.GetJobByIdAsync<SampleFaultWebJobParameters, SampleFaultWebJobState>(id, cancellationToken);
        return Ok(job);
    }
}
