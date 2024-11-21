<!-- TOC -->
* [jobba](#jobba)
  * [Stores](#stores)
    * [InMemory](#inmemory)
    * [Mongo](#mongo)
    * [EFCore](#efcore)
    * [Custom](#custom)
  * [Locks](#locks)
    * [InMemory](#inmemory-1)
    * [Redis](#redis)
    * [Custom](#custom-1)
  * [Event Publishers](#event-publishers)
    * [InMemory](#inmemory-2)
    * [MassTransit](#masstransit)
    * [Custom](#custom-2)
  * [Setup](#setup)
* [Usage](#usage)
  * [ScheduleJobAsync](#schedulejobasync)
  * [CancelJobAsync](#canceljobasync)
<!-- TOC -->

# jobba

A durable job scheduling platform for dotnet

## Stores

### InMemory

The in-memory store is useful for testing and development. It is not recommended for production use.

```csharp
var jobba = new JobbaBuilder(serviceCollection, "sample")
    .UsingInMemory();
```

### Mongo

To use the Mongo store, install the `Jobba.Store.Mongo` package

```bash
dotnet add package Jobba.Store.Mongo
```

Then add the following to your `JobbaBuilder` configuration, providing the connection string and a boolean to enable or 
disable command logging. There is also an optional third parameter to allow for additional configuration of the Mongo 
store using the [JobbaMongoBuilder.cs](Jobba.Store.Mongo%2FBuilders%2FJobbaMongoBuilder.cs).

```csharp
var jobba = new JobbaBuilder(serviceCollection, "sample")
    .UsingMongo(config.GetConnectionString("MongoDb"), false);
```

### EFCore

To use the EFCore store, install the `Jobba.Store.EFCore` package

```bash
dotnet add package Jobba.Store.EFCore
```

There are currently 2 supported EF providers maintained by this library.

#### SqlServer

To use the SqlServer provider, install the `Jobba.Store.EFCore.Sql` package

```bash
dotnet add package Jobba.Store.EFCore.Sql
```

Then add the following to your `JobbaBuilder` configuration, providing the connection string, and optional actions to provide
additional configuration of the `DbContextOptionsBuilder`, `SqlServerDbContextOptionsBuilder`. and the [JobbaEfBuilder.cs](Jobba.Store.EF%2FBuilders%2FJobbaEfBuilder.cs).

```csharp
var jobba = new JobbaBuilder(serviceCollection, "sample")
    .UsingSqlServer(config.GetConnectionString("SqlServer"),
                    options =>
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }, configureBuilder: jb => jb.WithDbInitializer());
```

#### Sqlite

To use the Sqlite provider, install the `Jobba.Store.EFCore.Sqlite` package

```bash
dotnet add package Jobba.Store.EFCore.Sqlite
```

Then add the following to your `JobbaBuilder` configuration, providing the connection string, and optional actions to provide
additional configuration of the `DbContextOptionsBuilder`, `SqliteDbContextOptionsBuilder`, and the [JobbaEfBuilder.cs](Jobba.Store.EF%2FBuilders%2FJobbaEfBuilder.cs).

```csharp
var jobba = new JobbaBuilder(serviceCollection, "sample")
    .UsingSqlite(config.GetConnectionString("Sqlite"),
                 options =>
                 {
                     options.EnableSensitiveDataLogging();
                     options.EnableDetailedErrors();
                 }, configureBuilder: jb => jb.WithDbInitializer());
```

#### Other providers

To use a different EF provider, you will need to manage the configuration and migrations yourself. You can refer to the
SqlServer and Sqlite implementations for guidance. [JobbaEfBuilderExtensions.cs](Jobba.Store.EF.Sql%2Fextensions%2FJobbaEfBuilderExtensions.cs)

### Custom

The following interfaces must be registered in the DI container for your custom store:
- [IJobListStore.cs](Jobba.Core%2FInterfaces%2FRepositories%2FIJobListStore.cs)
- [IJobProgressStore.cs](Jobba.Core%2FInterfaces%2FRepositories%2FIJobProgressStore.cs)
- [IJobProgressStore.cs](Jobba.Core%2FInterfaces%2FRepositories%2FIJobProgressStore.cs)
- [IJobProgressStore.cs](Jobba.Core%2FInterfaces%2FRepositories%2FIJobProgressStore.cs)
- [IJobProgressStore.cs](Jobba.Core%2FInterfaces%2FRepositories%2FIJobProgressStore.cs)

You can refer to the [InMemory implementation](Jobba.Core%2FBuilders%2FJobbaInMemoryBuilder.cs) for an example of how to implement these interfaces.

## Locks

Locking ensures that only one instance of a job is running at a time.

### InMemory

The in-memory lock is useful for testing and development. It is registered by default when using the `JobbaBuilder`.
Only use in production if you have a single instance of your application.

### Redis

This will use the [lit-redis](https://github.com/firebend/lit-redis) library which will allow for distributed locking 
across multiple instances of your application.

To use the Redis lock, install the `Jobba.Redis` package

```bash
dotnet add package Jobba.Redis
```

Then add the following to your `JobbaBuilder` configuration, providing your connection string.

```csharp
var jobba = new JobbaBuilder(serviceCollection, "sample")
    .UsingLitRedis(config.GetConnectionString("Redis"));
```

### Custom

The following interface must be registered in the DI container for your custom lock:

- [IJobLockService.cs](Jobba.Core%2FInterfaces%2FIJobLockService.cs)

You can refer to the [InMemory implementation](Jobba.Core%2FImplementations%2FDefaultJobLockService.cs) for an example of how to implement this interface.

## Event Publishers

Jobba publishes various events to allow for monitoring and logging of job progress.

The events published are:
- [CancelJobEvent.cs](Jobba.Core%2FEvents%2FCancelJobEvent.cs)
- [JobWatchEvent.cs](Jobba.Core%2FEvents%2FJobWatchEvent.cs)
- [JobCancelledEvent.cs](Jobba.Core%2FEvents%2FJobCancelledEvent.cs)
- [JobCompletedEvent.cs](Jobba.Core%2FEvents%2FJobCompletedEvent.cs)
- [JobFaultedEvent.cs](Jobba.Core%2FEvents%2FJobFaultedEvent.cs)
- [JobProgressEvent.cs](Jobba.Core%2FEvents%2FJobProgressEvent.cs)
- [JobRestartEvent.cs](Jobba.Core%2FEvents%2FJobRestartEvent.cs)
- [JobStartedEvent.cs](Jobba.Core%2FEvents%2FJobStartedEvent.cs)

### InMemory

The in-memory event publisher is useful for testing and development. It is registered by default when using the `JobbaBuilder`.
Only use in production if you have a single instance of your application.

### MassTransit

This will use the [MassTransit](https://github.com/MassTransit/MassTransit) library to facilitate event publishing.
By using MassTransit, you can publish events to a message broker such as RabbitMQ or Azure Service Bus allowing for distributed
event handling.

### Custom

The following interface must be registered in the DI container for your custom event publisher:

- [IJobEventPublisher.cs](Jobba.Core%2FInterfaces%2FIJobEventPublisher.cs)

You must also register consumers for each event published by Jobba. You can refer to the 
[JobbaMassTransitBuilderExtensions.cs](Jobba.MassTransit%2Fextensions%2FJobbaMassTransitBuilderExtensions.cs) for an example of how to implement this interface.

## Setup

Check out the [Sample project](https://github.com/firebend/jobba/tree/main/Jobba.Sample) for a working example.

1. Install the library

```xml
<ItemGroup>
   <PackageReference Include="Jobba.Core" />
   <PackageReference Include="Jobba.MassTransit" />
   <PackageReference Include="Jobba.Redis" />
   <PackageReference Include="Jobba.Store.Mongo" />
</ItemGroup>
```

or

```bash
dotnet add package Jobba.Core
dotnet add package Jobba.MassTransit
dotnet add package Jobba.Redis
dotnet add package Jobba.Store.Mongo
```

2. Create a new `SampleJob` class that extends `AbstractJobBaseClass`. You can also create classes for `JobState`
   and `JobParameters`, or use `object` as a placeholder

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

    public class SampleJob : AbstractJobBaseClass<SampleJobParameters, SampleJobState> // or use `AbstractJobBaseClass<DefaultJobParams, DefaultJobState>` to not use state or parameters
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
          .UsingMongo("mongodb://localhost:27017/jobba-sample", false)
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

