using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobba.Store.EF.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class JobRegistrationSystemMoniker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobRegistrations_JobName",
                schema: "jobba",
                table: "JobRegistrations");

            migrationBuilder.AddColumn<string>(
                name: "SystemMoniker",
                schema: "jobba",
                table: "JobRegistrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobRegistrations_JobName_SystemMoniker",
                schema: "jobba",
                table: "JobRegistrations",
                columns: new[] { "JobName", "SystemMoniker" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobRegistrations_JobName_SystemMoniker",
                schema: "jobba",
                table: "JobRegistrations");

            migrationBuilder.DropColumn(
                name: "SystemMoniker",
                schema: "jobba",
                table: "JobRegistrations");

            migrationBuilder.CreateIndex(
                name: "IX_JobRegistrations_JobName",
                schema: "jobba",
                table: "JobRegistrations",
                column: "JobName",
                unique: true);
        }
    }
}
