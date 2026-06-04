using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Jobba.Store.EF.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfReadyGateTests
{
    [TestMethod]
    public async Task Jobba_Ef_Ready_Gate_Should_Observe_Cancellation_And_Retry()
    {
        //arrange
        var dbContextProvider = new RecordingDbContextProvider();
        var services = new ServiceCollection();
        services.AddScoped<IDbContextProvider>(_ => dbContextProvider);

        await using var serviceProvider = services.BuildServiceProvider();
        var readyGate = new JobbaEfReadyGate(serviceProvider.GetRequiredService<IServiceScopeFactory>());

        using var cancellationTokenSource = new CancellationTokenSource();

        //act
        var waitTask = readyGate.WaitAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        //assert
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => waitTask);
        dbContextProvider.CapturedTokens.Should().ContainSingle()
            .Which.Should().Be(cancellationTokenSource.Token);

        dbContextProvider.CompleteImmediately = true;

        await readyGate.WaitAsync(default);

        dbContextProvider.CallCount.Should().Be(2);
    }

    private sealed class RecordingDbContextProvider : IDbContextProvider
    {
        public List<CancellationToken> CapturedTokens { get; } = [];

        public int CallCount { get; private set; }

        public bool CompleteImmediately { get; set; }

        public async Task<JobbaDbContext> GetDbContextAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            CapturedTokens.Add(cancellationToken);

            if (!CompleteImmediately)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new JobbaDbContext();
        }
    }
}
