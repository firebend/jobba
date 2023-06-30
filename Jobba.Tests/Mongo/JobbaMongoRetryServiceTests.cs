using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Jobba.Store.Mongo.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Mongo;

[TestClass]
public class JobbaMongoRetryServiceTests
{
    [TestMethod]
    public async Task Jobba_Mongo_Retry_Service_Should_Only_Try_Once()
    {
        //arrange
        var fixture = new Fixture();

        var retryService = fixture.Create<JobbaMongoRetryService>();

        var count = 0;

        Task<bool> FakeFunc()
        {
            count++;
            return Task.FromResult(true);
        }

        //act
        var result = await retryService.RetryErrorAsync(FakeFunc, 10);

        //assert
        result.Should().BeTrue();
        count.Should().Be(1);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Retry_Service_Should_Retry()
    {
        //arrange
        var fixture = new Fixture();

        var retryService = fixture.Create<JobbaMongoRetryService>();

        var count = 0;

        Task<bool> FakeFunc()
        {
            count++;

            if (count < 3)
            {
                throw new Exception("Fake");
            }

            return Task.FromResult(true);
        }

        //act
        var result = await retryService.RetryErrorAsync(FakeFunc, 10);

        //assert
        result.Should().BeTrue();
        count.Should().Be(3);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Retry_Service_Should_Retry_Maximum_Number_Of_Times()
    {
        //arrange
        var fixture = new Fixture();

        var retryService = fixture.Create<JobbaMongoRetryService>();

        var count = 0;

        Task<bool> FakeFunc()
        {
            count++;

            throw new Exception("Fake");
        }

        //act
        try
        {
            await retryService.RetryErrorAsync(FakeFunc, 4);
        }
        catch (Exception ex)
        {
            ex.Message.Should().Be("Fake");
        }

        //assert
        count.Should().Be(4);
    }
}
