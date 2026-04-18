using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTotalDaysConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_TherapyTypes_TherapyTypeId",
                table: "Appointments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Notifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Action",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans",
                sql: "\"TotalDays\" IN (20, 30, 50)");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_TherapyTypes_TherapyTypeId",
                table: "Appointments",
                column: "TherapyTypeId",
                principalTable: "TherapyTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_TherapyTypes_TherapyTypeId",
                table: "Appointments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans",
                sql: "\"TotalDays\" IN (20, 30, 40, 50)");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_TherapyTypes_TherapyTypeId",
                table: "Appointments",
                column: "TherapyTypeId",
                principalTable: "TherapyTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
