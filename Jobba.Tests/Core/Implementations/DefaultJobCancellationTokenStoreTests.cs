using System;
using System.Threading;
using FluentAssertions;
using Jobba.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Core.Implementations
{
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
    }
}
