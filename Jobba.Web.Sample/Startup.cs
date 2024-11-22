using System;
using System.Text.Json.Serialization;
using Jobba.Core.Builders;
using Jobba.Core.Extensions;
using Jobba.Cron.Extensions;
using Jobba.MassTransit.Extensions;
using Jobba.Redis;
using Jobba.Sample;
using Jobba.Store.EF.Sql.Extensions;
using Jobba.Store.EF.Sqlite.Extensions;
using Jobba.Store.Mongo;
using Jobba.Store.Mongo.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Jobba.Web.Sample;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                o.JsonSerializerOptions.Converters.Add(new TimeSpanStringConverter());
            });
        services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Jobba.Web.Sample",
            Version = "v1"
        }));
        services
            .AddLogging(o => o.AddSimpleConsole(c => c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "))
            .AddJobba("jobba-web-sample", jobba =>
            {
                jobba.UsingMassTransit();

                ResolveStore(Configuration, jobba);
                    jobba.UsingLitRedis("localhost:6379,defaultDatabase=0")
                    .UsingCron(cron =>
                    {
                        ///schedule a sample job running every 1 minute with default job parameters and state
                        cron.AddCronJob<SampleCronJob, CronParameters, CronState>("* * * * *",
                            "Sample Cron Job",
                            "A Cron Job",
                            TimeZoneInfo.Local,
                            p =>
                            {
                                p.DefaultParams = new CronParameters { StartDate = DateTimeOffset.UtcNow };
                                p.DefaultState = new CronState { Phrase = $"Hi {Guid.NewGuid()}" };
                            });
                    })
                    .AddJob<SampleWebJob, SampleWebJobParameters, SampleWebJobState>("sample-job")
                    .AddJob<SampleFaultWebJob, SampleFaultWebJobParameters, SampleFaultWebJobState>(
                        "sample-faulted-job");
            })
            .AddJobbaSampleMassTransit("rabbitmq://guest:guest@localhost/");
    }

    private static void ResolveStore(IConfiguration config, JobbaBuilder jobba)
    {
        var provider = config.GetValue("provider", StoreProviders.SqlServer);
        Console.WriteLine("Using provider: " + provider);
        switch (provider)
        {
            case StoreProviders.InMemory:
                jobba.UsingInMemory();
                break;
            case StoreProviders.Sqlite:
                jobba.UsingSqlite(config.GetConnectionString("Sqlite")!, options =>
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }, configureBuilder: jb => jb.WithDbInitializer());
                break;
            case StoreProviders.SqlServer:
                jobba.UsingSqlServer(config.GetConnectionString("SqlServer")!,
                    options =>
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }, configureBuilder: jb => jb.WithDbInitializer());
                break;
            case StoreProviders.MongoDb:
                JobbaMongoDbConfigurator.Configure();
                jobba.UsingMongo(config.GetConnectionString("MongoDb"), false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jobba.Web.Sample v1"));
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
