using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredTherapistToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredTherapistId",
                table: "AppointmentRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_PreferredTherapistId",
                table: "AppointmentRequests",
                column: "PreferredTherapistId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_Therapists_PreferredTherapistId",
                table: "AppointmentRequests",
                column: "PreferredTherapistId",
                principalTable: "Therapists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_Therapists_PreferredTherapistId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_PreferredTherapistId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "PreferredTherapistId",
                table: "AppointmentRequests");
        }
    }
}
