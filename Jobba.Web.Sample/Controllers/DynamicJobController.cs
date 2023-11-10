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
public class DynamicJobController : ControllerBase
{
    private readonly IJobRegistrationStore _registrationStore;
    private readonly IJobScheduler _jobScheduler;

    public DynamicJobController(IJobRegistrationStore registrationStore, IJobScheduler jobScheduler)
    {
        _registrationStore = registrationStore;
        _jobScheduler = jobScheduler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CancellationToken cancellationToken)
    {
        var registration = JobRegistration.FromTypes<DynamicJob, DefaultJobParams, DefaultJobState>(
            $"{DynamicJob.Name}-{Guid.NewGuid()}",
            "A dynamic job");

        var created = await _registrationStore.RegisterJobAsync(registration, cancellationToken);

        var jobInfo = await _jobScheduler.ScheduleJobAsync<DefaultJobParams, DefaultJobState>(
            created.Id,
            new(),
            new(),
            cancellationToken);

        while(DynamicJobStatics.Runs.ContainsKey(jobInfo.Id) is false)
        {
            await Task.Delay(100, cancellationToken);
        }

        return Ok();
    }
}
