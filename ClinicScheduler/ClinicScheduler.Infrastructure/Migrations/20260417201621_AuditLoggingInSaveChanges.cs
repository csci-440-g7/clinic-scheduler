using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditLoggingInSaveChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentPlan_Frequency",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentPlan_Frequency",
                table: "TreatmentPlans",
                sql: "\"FrequencyPerWeek\" IN (2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentPlan_Frequency",
                table: "TreatmentPlans");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentPlan_Frequency",
                table: "TreatmentPlans",
                sql: "\"FrequencyPerWeek\" IN  (2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }
    }
}
