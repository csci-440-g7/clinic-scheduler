using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add40DayTreatmentPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans",
                sql: "\"TotalDays\" IN (20, 30, 40, 50)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentPlan_TotalDays",
                table: "TreatmentPlans",
                sql: "\"TotalDays\" IN (20, 30, 50)");
        }
    }
}
