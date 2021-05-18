using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations
{
    public class DefaultJobbaGuidGenerator : IJobbaGuidGenerator
    {
        public Task<Guid> GenerateGuidAsync(CancellationToken cancellationToken)
        {
            var guid = Guid.Empty;
            var timestamp = DateTime.UtcNow;
            var dateTime = DateTime.UnixEpoch;
            var timeSpan = timestamp - dateTime;
            var timeSpanMs = (long)timeSpan.TotalMilliseconds;
            var timestampString = timeSpanMs.ToString("x8");
            var guidString = guid.ToString("N");

            var pooledBuilder = new StringBuilder();

            pooledBuilder.Append(timestampString[..11]);
            pooledBuilder.Append(guidString[11..]);

            var newGuidString = pooledBuilder.ToString();

            if (string.IsNullOrWhiteSpace(newGuidString))
            {
                throw new Exception("Could not get guid string");
            }

            var newGuid = Guid.Parse(newGuidString);

            return Task.FromResult(newGuid);
        }
    }
}
