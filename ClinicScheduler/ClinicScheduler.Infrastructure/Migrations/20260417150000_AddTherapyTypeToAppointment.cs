using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTherapyTypeToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TherapyTypeId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TherapyTypeId",
                table: "Appointments",
                column: "TherapyTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_TherapyTypes_TherapyTypeId",
                table: "Appointments",
                column: "TherapyTypeId",
                principalTable: "TherapyTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_TherapyTypes_TherapyTypeId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TherapyTypeId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TherapyTypeId",
                table: "Appointments");
        }
    }
}
