using System.Text.Json.Serialization;
using Jobba.Core.Extensions;
using Jobba.MassTransit.Extensions;
using Jobba.Redis;
using Jobba.Store.Mongo.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Jobba.Web.Sample
{
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
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jobba.Web.Sample", Version = "v1" });
            });
            services
                .AddLogging(o => o.AddSimpleConsole(c => c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "))
                .AddJobba(jobba =>
                    jobba.UsingMassTransit()
                        .UsingMongo("mongodb://localhost:27017/jobba-web-sample", false)
                        .UsingLitRedis("localhost:6379,defaultDatabase=0")
                        .AddJob<SampleWebJob, SampleWebJobParameters, SampleWebJobState>()
                        .AddJob<SampleFaultWebJob, SampleFaultWebJobParameters, SampleFaultWebJobState>()
                )
                .AddJobbaSampleMassTransit("rabbitmq://guest:guest@localhost/");
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
