using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jobba.Store.Mongo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;

namespace Jobba.Web.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            JobbaMongoDbConfigurator.Configure();
            BsonClassMap.RegisterClassMap<SampleWebJobParameters>();
            BsonClassMap.RegisterClassMap<SampleWebJobState>();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
