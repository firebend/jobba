using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobba.Store.EF.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialScaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobba");

            migrationBuilder.CreateTable(
                name: "JobRegistrations",
                schema: "jobba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobParamsType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobStateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultMaxNumberOfTries = table.Column<int>(type: "int", nullable: false),
                    DefaultJobWatchInterval = table.Column<TimeSpan>(type: "time", nullable: false),
                    PreviousExecutionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextExecutionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultParams = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInactive = table.Column<bool>(type: "bit", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_JobRegistrations", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Jobs",
                schema: "jobba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastProgressPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LastProgressDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EnqueuedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FaultedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxNumberOfTries = table.Column<int>(type: "int", nullable: false),
                    CurrentNumberOfTries = table.Column<int>(type: "int", nullable: false),
                    JobWatchInterval = table.Column<TimeSpan>(type: "time", nullable: false),
                    JobParameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobStateTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobParamsTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOutOfRetry = table.Column<bool>(type: "bit", nullable: false),
                    JobRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemInfo_ComputerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemInfo_OperatingSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemInfo_SystemMoniker = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemInfo_User = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_JobRegistrations_JobRegistrationId",
                        column: x => x.JobRegistrationId,
                        principalSchema: "jobba",
                        principalTable: "JobRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobProgress",
                schema: "jobba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    JobState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Progress = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    JobRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobProgress_JobRegistrations_JobRegistrationId",
                        column: x => x.JobRegistrationId,
                        principalSchema: "jobba",
                        principalTable: "JobRegistrations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobProgress_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobba",
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobProgress_JobId",
                schema: "jobba",
                table: "JobProgress",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobProgress_JobRegistrationId",
                schema: "jobba",
                table: "JobProgress",
                column: "JobRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRegistrations_JobName",
                schema: "jobba",
                table: "JobRegistrations",
                column: "JobName",
                unique: true,
                filter: "[JobName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobRegistrationId",
                schema: "jobba",
                table: "Jobs",
                column: "JobRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status",
                schema: "jobba",
                table: "Jobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobProgress",
                schema: "jobba");

            migrationBuilder.DropTable(
                name: "Jobs",
                schema: "jobba");

            migrationBuilder.DropTable(
                name: "JobRegistrations",
                schema: "jobba");
        }
    }
}
