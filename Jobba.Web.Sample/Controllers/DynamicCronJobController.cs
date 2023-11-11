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
public class DynamicCronJobController : ControllerBase
{
    private readonly IJobRegistrationStore _registrationStore;
    private readonly IJobScheduler _jobScheduler;

    public DynamicCronJobController(IJobRegistrationStore registrationStore, IJobScheduler jobScheduler)
    {
        _registrationStore = registrationStore;
        _jobScheduler = jobScheduler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CancellationToken cancellationToken)
    {
        var registration = JobRegistration.FromTypes<DynamicCronJob, DefaultJobParams, DefaultJobState>(
            $"{DynamicCronJob.Name}-{Guid.NewGuid()}",
            "A dynamic cron job",
            "* * * * *");

        var created = await _registrationStore.RegisterJobAsync(registration, cancellationToken);

        while(DynamicCronJobStatics.Runs.ContainsKey(created.Id) is false)
        {
            await Task.Delay(100, cancellationToken);
        }

        return Ok();
    }
}
