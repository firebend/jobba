using System;
using Jobba.Store.EF.DbContexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Tests.EF;

public class EfTestContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public EfTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
    }

    public DbContextOptionsBuilder<JobbaDbContext> ConfigureDbContextOptions(
        DbContextOptionsBuilder<JobbaDbContext> optionsBuilder, bool enableLogging = false)
    {
        optionsBuilder.UseSqlite(_connection);

        if (enableLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.LogTo(Console.WriteLine);
        }

        return optionsBuilder;
    }

    public JobbaDbContext CreateContext(bool enableLogging = false)
    {
        _connection.Open();
        var optionsBuilder = ConfigureDbContextOptions(new DbContextOptionsBuilder<JobbaDbContext>(), enableLogging);
        var context = new JobbaDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection?.Close();
    }
}
