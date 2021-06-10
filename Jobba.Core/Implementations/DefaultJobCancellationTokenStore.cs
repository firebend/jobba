using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations
{
    internal static class DefaultJobCancellationTokenStoreStatics
    {
        internal static readonly ConcurrentDictionary<Guid, CancellationTokenSource> TokenDictionary = new();
    }

    public class DefaultJobCancellationTokenStore : IJobCancellationTokenStore
    {
        public CancellationToken CreateJobCancellationToken(Guid jobId, CancellationToken cancellationToken)
        {
            if (DefaultJobCancellationTokenStoreStatics.TokenDictionary.TryGetValue(jobId, out var ct))
            {
                return ct.Token;
            }

            var tokenSource = new CancellationTokenSource();

            var linked = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, cancellationToken);

            if (DefaultJobCancellationTokenStoreStatics.TokenDictionary.TryAdd(jobId, tokenSource))
            {
                return linked.Token;
            }

            return DefaultJobCancellationTokenStoreStatics.TokenDictionary[jobId].Token;
        }

        //todo: test
        private void RemoveCancelledCompletedTokens()
        {
            var jobIds = DefaultJobCancellationTokenStoreStatics.TokenDictionary.Keys.ToList();

            foreach (var jobId in jobIds)
            {
                if (!DefaultJobCancellationTokenStoreStatics.TokenDictionary.TryGetValue(jobId, out  var ct))
                {
                    continue;
                }

                if (ct.IsCancellationRequested || ct.Token.IsCancellationRequested)
                {
                    DefaultJobCancellationTokenStoreStatics.TokenDictionary.TryRemove(jobId, out _);
                }
            }
        }

        public bool CancelJob(Guid id)
        {
            if (!DefaultJobCancellationTokenStoreStatics.TokenDictionary.TryGetValue(id, out var tokenSource))
            {
                return false;
            }

            tokenSource.Cancel();

            RemoveCancelledCompletedTokens();

            return true;

        }

        //todo: test
        public void CancelAllJobs()
        {
            var jobIds = DefaultJobCancellationTokenStoreStatics.TokenDictionary.Keys.ToList();

            foreach (var jobId in jobIds)
            {
                Console.WriteLine($"Cancelling job {jobId}");
                CancelJob(jobId);
            }

            RemoveCancelledCompletedTokens();
        }
    }
}
