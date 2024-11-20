using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jobba.Store.EF.DbContexts;

public class EfTypeConverter() : ValueConverter<Type, string>(v => v.AssemblyQualifiedName!,
    v => Type.GetType(v)!);
