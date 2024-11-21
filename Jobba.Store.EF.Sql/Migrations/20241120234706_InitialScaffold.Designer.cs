﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Jobba.Store.EF.SqlMigrations.Migrations
{
    [DbContext(typeof(JobbaDbContext))]
    [Migration("20241120234706_InitialScaffold")]
    partial class InitialScaffold
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Jobba.Core.Models.Entities.JobEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CurrentNumberOfTries")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("EnqueuedTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("FaultedReason")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsOutOfRetry")
                        .HasColumnType("bit");

                    b.Property<string>("JobName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobParameters")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobParamsTypeName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("JobRegistrationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("JobState")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobStateTypeName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<TimeSpan>("JobWatchInterval")
                        .HasColumnType("time");

                    b.Property<DateTimeOffset>("LastProgressDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("LastProgressPercentage")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.Property<int>("MaxNumberOfTries")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.ComplexProperty<Dictionary<string, object>>("SystemInfo", "Jobba.Core.Models.Entities.JobEntity.SystemInfo#JobSystemInfo", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("ComputerName")
                                .HasColumnType("nvarchar(max)");

                            b1.Property<string>("OperatingSystem")
                                .HasColumnType("nvarchar(max)");

                            b1.Property<string>("SystemMoniker")
                                .HasColumnType("nvarchar(max)");

                            b1.Property<string>("User")
                                .HasColumnType("nvarchar(max)");
                        });

                    b.HasKey("Id");

                    b.HasIndex("JobRegistrationId");

                    b.HasIndex("Status");

                    b.ToTable("Jobs", "jobba");
                });

            modelBuilder.Entity("Jobba.Core.Models.Entities.JobProgressEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("JobId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("JobRegistrationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("JobState")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Progress")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.HasKey("Id");

                    b.HasIndex("JobId");

                    b.HasIndex("JobRegistrationId");

                    b.ToTable("JobProgress", "jobba");
                });

            modelBuilder.Entity("Jobba.Core.Models.JobRegistration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CronExpression")
                        .HasColumnType("nvarchar(max)");

                    b.Property<TimeSpan>("DefaultJobWatchInterval")
                        .HasColumnType("time");

                    b.Property<int>("DefaultMaxNumberOfTries")
                        .HasColumnType("int");

                    b.Property<string>("DefaultParams")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DefaultState")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsInactive")
                        .HasColumnType("bit");

                    b.Property<string>("JobName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("JobParamsType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobStateType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JobType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("NextExecutionDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("PreviousExecutionDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("TimeZoneId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("JobName")
                        .IsUnique()
                        .HasFilter("[JobName] IS NOT NULL");

                    b.ToTable("JobRegistrations", "jobba");
                });

            modelBuilder.Entity("Jobba.Core.Models.Entities.JobEntity", b =>
                {
                    b.HasOne("Jobba.Core.Models.JobRegistration", null)
                        .WithMany()
                        .HasForeignKey("JobRegistrationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Jobba.Core.Models.Entities.JobProgressEntity", b =>
                {
                    b.HasOne("Jobba.Core.Models.Entities.JobEntity", null)
                        .WithMany()
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Jobba.Core.Models.JobRegistration", null)
                        .WithMany()
                        .HasForeignKey("JobRegistrationId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
