using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Redis.Implementations;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Redis;

[TestClass]
public class LitRedisJobLockServiceTests
{
    [TestMethod]
    public async Task Lit_Redis_Lock_Service_Should_Lock()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());
        var lockMock = fixture.Freeze<Mock<ILitRedisDistributedLockService>>();

        lockMock.Setup(x => x.AcquireLockAsync(It.IsAny<RequestLockModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LitRedisDistributedLockModel(true, () => Task.CompletedTask, false, null));

        var service = fixture.Create<LitRedisJobLockService>();

        var jobId = Guid.NewGuid();

        //act
        var locker = await service.LockJobAsync(jobId, default);

        //assert
        locker.Should().NotBeNull();
        lockMock.Verify(x => x.AcquireLockAsync(It.IsAny<RequestLockModel>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
