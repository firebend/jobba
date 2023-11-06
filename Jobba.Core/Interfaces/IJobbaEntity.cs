using System;

namespace Jobba.Core.Interfaces;

/// <summary>
/// A Job entity that should be saved to a store.
/// </summary>
public interface IJobbaEntity
{
    public Guid Id { get; set; }
}
