using System;
using FluentAssertions;
using Jobba.Cron.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Cron;

[TestClass]
public class CronServiceTests
{
    [TestMethod]
    public void Cron_Service_Should_Get_Next_Execution_Date()
    {
        var service = new CronService();

        var next = service.GetNextExecutionDate("30 16 1 JAN *", TimeZoneInfo.Utc);

        next.Should().NotBeNull();
        next!.Value.Hour.Should().Be(16);
        next.Value.Minute.Should().Be(30);
        next.Value.Month.Should().Be(1);
        next.Value.Day.Should().Be(1);
        next.Value.Year.Should().Be(DateTimeOffset.UtcNow.Year + 1);
    }

    [TestMethod]
    public void Cron_Service_Should_Get_Next_Execution_Date_With_Specified_Time_Zone()
    {
        var service = new CronService();

        var next = service.GetNextExecutionDate("30 16 1 JAN *", TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

        next.Should().NotBeNull();
        next!.Value.Hour.Should().Be(16);
        next.Value.Minute.Should().Be(30);
        next.Value.Month.Should().Be(1);
        next.Value.Day.Should().Be(1);
        next.Value.Year.Should().Be(DateTimeOffset.UtcNow.Year + 1);
        next.Value.Offset.Should().BeNegative();
    }

    [TestMethod]
    public void Cron_Service_Should_Get_Window()
    {
        var service = new CronService();

        var year = DateTimeOffset.UtcNow.Year + 1;
        var start = new DateTimeOffset(year, 1, 1, 1, 1, 1, TimeSpan.FromHours(-6));
        var end = new DateTimeOffset(year, 2, 1, 1, 1, 1, TimeSpan.FromHours(-6));
        var next = service.WillExecuteInWindow("30 16 1 1 *", start, end, TimeZoneInfo.Utc);
        next.Should().BeTrue();
    }

    [TestMethod]
    public void Cron_Service_Should_Get_Occurrences()
    {
        var service = new CronService();

        var year = DateTimeOffset.UtcNow.Year + 1;
        var start = new DateTimeOffset(year, 1, 1, 1, 1, 1, TimeSpan.FromHours(-6));
        var end = new DateTimeOffset(year + 5, 2, 1, 1, 1, 1, TimeSpan.FromHours(-6));
        var next = service.GetSchedule("30 16 1 1 *", start, end, TimeZoneInfo.Utc);
        next.Should().NotBeEmpty();
        next.Length.Should().Be(6);
    }
}
