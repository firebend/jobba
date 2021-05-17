using System;
using System.Collections.Concurrent;
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

        public bool CancelJob(Guid id)
        {
            if (!DefaultJobCancellationTokenStoreStatics.TokenDictionary.TryGetValue(id, out var tokenSource))
            {
                return false;
            }

            tokenSource.Cancel();

            return true;

        }
    }
}
