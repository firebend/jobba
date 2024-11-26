using System;
using AutoFixture;
using AutoFixture.Dsl;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Tests;

public static  class TestExtensions
{
    public static IPostprocessComposer<JobRegistration> JobRegistrationBuilder(this IFixture fixture)
    {
        return fixture.Build<JobRegistration>()
            .With(x => x.SystemMoniker, TestModels.TestSystemInfo.SystemMoniker)
            .With(x => x.JobName, "Test")
            .With(x => x.Description, "Test")
            .With(x => x.DefaultParams, new TestModels.FooParams { Baz = "baz" })
            .With(x => x.DefaultState, new TestModels.FooState { Bar = "bar" })
            .With(x => x.IsInactive, false)
            .With(x => x.TimeZoneId, "UTC");
    }

    public static IPostprocessComposer<JobRegistration> JobCronRegistrationBuilder(this IFixture fixture,
        string cron = "0 0 0 1 1 ? 2099") => fixture.JobRegistrationBuilder().With(x => x.CronExpression, cron);

    public static IPostprocessComposer<JobEntity> JobBuilder(this IFixture fixture, Guid jobRegistrationId)
    {
        return fixture.Build<JobEntity>()
            .With(x => x.JobRegistrationId, jobRegistrationId)
            .With(x => x.JobParameters, new TestModels.FooParams { Baz = "baz" })
            .With(x => x.JobState, new TestModels.FooState { Bar = "bar" })
            .With(x => x.SystemInfo, TestModels.TestSystemInfo);
    }
}
