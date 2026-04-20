using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDenialReasonToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DenialReason",
                table: "AppointmentRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DenialReason",
                table: "AppointmentRequests");
        }
    }
}
