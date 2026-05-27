using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobba.Store.EF.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddLastHeartbeatTimeToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastHeartbeatTime",
                schema: "jobba",
                table: "Jobs",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHeartbeatTime",
                schema: "jobba",
                table: "Jobs");
        }
    }
}
