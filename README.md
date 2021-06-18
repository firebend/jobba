# jobba
A durable job scheduling platform for dotnet

# Setup

Check out the [Sample project](https://github.com/firebend/jobba/tree/main/Jobba.Sample) for a working example.

1. Install the library
```xml
<ItemGroup>
   <PackageReference Include="Firebend.Jobba.Core" />
   <PackageReference Include="Firebend.Jobba.MassTransit" />
   <PackageReference Include="Firebend.Jobba.Redis" />
   <PackageReference Include="Firebend.Jobba.Store.Mongo" />
</ItemGroup>
```
or 
```bash
dotnet add package Firebend.Jobba.Core
dotnet add package Firebend.Jobba.MassTransit
dotnet add package Firebend.Jobba.Redis
dotnet add package Firebend.Jobba.Store.Mongo
```

2. Create a new `SampleJob` class that extends `AbstractJobBaseClass`. You can also create classes for `JobState` and `JobParameters`, or use `object` as a placeholder
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Sample
{
    public class SampleJobState
    {
        public int Tries { get; set; }
    }

    public class SampleJobParameters
    {
        public string Greeting { get; set; }
    }

    public class SampleJob : AbstractJobBaseClass<SampleJobParameters, SampleJobState> // or use `AbstractJobBaseClass<object, object>` to not use state or parameters
    {
        private readonly ILogger<SampleJob> _logger;

        public SampleJob(IJobProgressStore progressStore, ILogger<SampleJob> logger) : base(progressStore)
        {
            _logger = logger;
        }

        protected override async Task OnStartAsync(JobStartContext<SampleJobParameters, SampleJobState> jobStartContext, CancellationToken cancellationToken)
        {
            // implement your job's behavior
            var tries = jobStartContext.JobState.Tries + 1;
            _logger.LogInformation("Hey I'm trying! Tries: {Tries} {JobId} {Now}", tries, jobStartContext.JobId, DateTimeOffset.Now);
            await LogProgressAsync(new SampleJobState { Tries = tries }, 50, jobStartContext.JobParameters.Greeting, cancellationToken);
            await Task.Delay(100 * tries, cancellationToken);

            if (tries < 10)
            {
                throw new Exception($"Haven't tried enough {tries}"); // jobba will retry if it encounters an exception
            }
            _logger.LogInformation("Now I'm done!");
        }

        public override string JobName => "Sample Job";
    }
}
```

3. In `Program.cs`, add the jobba configuration to the `ConfigureServices` callback in `CreateHostBuilder`
```csharp
   services
      .AddLogging(o => o.AddSimpleConsole(c => c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "))
      .AddJobba(jobba =>
        jobba.UsingMassTransit() // use MassTransit as an event bus
          .UsingMongo("mongodb://localhost:27017/jobba-sample", false) // Mongo currently is the only supported data store
          .UsingLitRedis("localhost:6379,defaultDatabase=0") // Use LitRedis for distributed locking
          .AddJob<SampleJob, SampleJobParameters, SampleJobState>() // `AddJob<SampleJob, object, object>` if not using state or parameters
        )
      .AddJobbaSampleMassTransit("rabbitmq://guest:guest@localhost/")
      .AddHostedService<SampleHostedService>();
```
This example uses
* [MassTransit](https://github.com/MassTransit/MassTransit) as an event bus
* Mongo as a data store
* [LitRedis](https://github.com/firebend/lit-redis) for distributed locking

4. Make a service `SampleHostedService` that extends `BackgroundService` and injects an `IJobScheduler`
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Microsoft.Extensions.Hosting;

namespace Jobba.Sample
{
    public class SampleHostedService : BackgroundService
    {
        private readonly IJobScheduler _jobScheduler;

        public SampleHostedService(IJobScheduler jobScheduler)
        {
            _jobScheduler = jobScheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // create and schedule your job
            var request = new JobRequest<SampleJobParameters, SampleJobState>
            {
                Description = "A Sample Job",
                JobParameters = new SampleJobParameters { Greeting = "Hello" },
                JobType = typeof(SampleJob),
                InitialJobState = new SampleJobState { Tries = 0 },
                JobWatchInterval = TimeSpan.FromSeconds(10),
                MaxNumberOfTries = 100
            };

            await _jobScheduler.ScheduleJobAsync(request, stoppingToken);
        }
    }
}
```

# Usage

## ScheduleJobAsync

Create a new `JobRequest` and schedule it with `_jobScheduler` in `SampleHostedService`'s `ExecuteAsync`

```csharp
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      // create a job request
      var request = new JobRequest<SampleJobParameters, SampleJobState>
      {
         Description = "A Sample Job",
         JobParameters = new SampleJobParameters { Greeting = "Hello" },
         JobType = typeof(SampleJob), // the class for the job to run
         InitialJobState = new SampleJobState { Tries = 0 },
         JobWatchInterval = TimeSpan.FromSeconds(10), // time to wait between retries if the job fails
         MaxNumberOfTries = 100 // maximum number of times to retry when a job fails
      };

      // schedule it with the passed-in cancellation token
      await _jobScheduler.ScheduleJobAsync(request, stoppingToken);
   }
```

## CancelJobAsync
Use the job's ID and the provided cancellation token to cancel a scheduled or running job

```csharp
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      // create a job request
      var request = new JobRequest<SampleJobParameters, SampleJobState>
      {
         Description = "A Sample Job",
         JobParameters = new SampleJobParameters { Greeting = "Hello" },
         JobType = typeof(SampleJob), // the class for the job to run
         InitialJobState = new SampleJobState { Tries = 0 },
         JobWatchInterval = TimeSpan.FromSeconds(10), // time to wait between retries if the job fails
         MaxNumberOfTries = 100 // maximum number of times to retry when a job fails
      };

      // schedule it with the passed-in cancellation token
      await _jobScheduler.ScheduleJobAsync(request, stoppingToken);

      // wait a second and cancel the job
      await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
      await _jobScheduler.CancelJobAsync(request.Id, stoppingToken);
   }
```