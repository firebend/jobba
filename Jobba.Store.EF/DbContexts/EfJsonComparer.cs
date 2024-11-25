using System;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace Jobba.Store.EF.DbContexts;

public class EfJsonComparer<T> : ValueComparer<T>
{
    public EfJsonComparer(JsonSerializerSettings? serializerSettings = null)
        : base((t1, t2) => DoEquals(t1, t2, serializerSettings),
            t => DoGetHashCode(t, serializerSettings),
            t => DoGetSnapshot(t, serializerSettings))
    {
    }

    private static T DoGetSnapshot(T instance, JsonSerializerSettings? settings)
    {
        if (instance is ICloneable cloneable)
        {
            return (T)cloneable.Clone();
        }

        return EfJsonConverter<T>.FromJson(EfJsonConverter<T>.ToJson(instance, settings));
    }

    private static int DoGetHashCode(T instance, JsonSerializerSettings? settings)
    {
        if (instance is IEquatable<T>)
        {
            return instance.GetHashCode();
        }

        return EfJsonConverter<T>.ToJson(instance, settings).GetHashCode();
    }

    private static bool DoEquals(T? left, T? right, JsonSerializerSettings? settings)
    {
        if (left is IEquatable<T> equatable)
        {
            return equatable.Equals(right);
        }

        var result = EfJsonConverter<T>.ToJson(left, settings).Equals(EfJsonConverter<T>.ToJson(right, settings));

        return result;
    }
}
