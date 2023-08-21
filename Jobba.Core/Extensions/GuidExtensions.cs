using System;

namespace Jobba.Core.Extensions;

public static class GuidExtensions
{
    public static Guid Coalesce(this Guid source) => source == Guid.Empty ? Guid.NewGuid() : source;
}
