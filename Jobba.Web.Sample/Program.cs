using Jobba.Store.Mongo;
using Jobba.Web.Sample;
using Microsoft.AspNetCore.Builder;
using MongoDB.Bson.Serialization;

var builder = WebApplication.CreateBuilder(args);
JobbaMongoDbConfigurator.Configure();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, app.Environment);

await app.RunAsync();
