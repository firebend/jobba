using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Jobba.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Core.Implementations;

[TestClass]
public class DefaultJobCancellationTokenStoreTests
{
    [TestMethod]
    public void Default_Job_Cancellation_Token_Store_Should_Cancel()
    {
        //arrange
        var token = new CancellationToken();
        var store = new DefaultJobCancellationTokenStore();
        var jobId = Guid.NewGuid();

        //act
        var createdToken = store.CreateJobCancellationToken(jobId, token);
        var cancelled = store.CancelJob(jobId);

        //assert
        cancelled.Should().BeTrue();
        createdToken.IsCancellationRequested.Should().BeTrue();
    }

    [TestMethod]
    public void Default_Job_Cancellation_Token_Store_Should_Cancel_And_Remove_Token_From_Cache()
    {
        //arrange
        var token = new CancellationToken();
        var store = new DefaultJobCancellationTokenStore();
        var jobId = Guid.NewGuid();

        //act
        var createdToken = store.CreateJobCancellationToken(jobId, token);
        var cancelled = store.CancelJob(jobId);
        var cancelledTwo = store.CancelJob(jobId);

        //assert
        cancelled.Should().BeTrue();
        createdToken.IsCancellationRequested.Should().BeTrue();
        cancelledTwo.Should().BeFalse();
        createdToken.IsCancellationRequested.Should().BeTrue();
    }

    [TestMethod]
    public void Default_Job_Cancellation_Token_Store_Should_Not_Cancel_When_Job_Id_Does_Not_Exist()
    {
        //arrange
        var store = new DefaultJobCancellationTokenStore();
        var jobId = Guid.NewGuid();

        //act
        var cancelled = store.CancelJob(jobId);

        //assert
        cancelled.Should().BeFalse();
    }

    [TestMethod]
    public void Default_Job_Cancellation_Token_Store_Should_Cancel_All_Jobs()
    {
        //arrange
        var store = new DefaultJobCancellationTokenStore();

        var jobTokens = Enumerable
            .Range(1, 10)
            .Select(_ => new
            {
                CancellationToken = new CancellationToken(),
                JobId = Guid.NewGuid()
            })
            .ToList();

        //act
        var tokens = jobTokens
            .Select(x => store.CreateJobCancellationToken(x.JobId, x.CancellationToken))
            .ToList();

        store.CancelAllJobs();

        //assert
        tokens.TrueForAll(x => x.IsCancellationRequested).Should().BeTrue();
    }
}
