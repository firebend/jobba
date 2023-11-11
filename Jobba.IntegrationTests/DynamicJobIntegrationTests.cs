using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Flurl.Http;
using Jobba.Core.Models;
using Jobba.IntegrationTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.IntegrationTests;

[TestClass]
public class DynamicJobIntegrationTests
{
    private const string Url = "http://localhost:5000/dynamicjob";

    [TestMethod]
    public async Task Should_Schedule_Dynamic_Job()
    {
        var job = await Url.PostAsync();
        job.Should().NotBeNull();
        job.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
