using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GroupAppSettingsScalarsIntoVOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the 11 flat-scalar columns to their new ComplexProperty names.
            // Using RenameColumn (not Drop+Add) to preserve the existing singleton row values.
            migrationBuilder.RenameColumn(
                name: "BugRatioAlertThreshold",
                table: "AppSettings",
                newName: "BugRatio_AlertThreshold");

            migrationBuilder.RenameColumn(
                name: "BugRatioConsecutiveSprintCount",
                table: "AppSettings",
                newName: "BugRatio_ConsecutiveSprintCount");

            migrationBuilder.RenameColumn(
                name: "BugRatioTarget",
                table: "AppSettings",
                newName: "BugRatio_Target");

            migrationBuilder.RenameColumn(
                name: "DefaultSpPerBug",
                table: "AppSettings",
                newName: "BugRatio_DefaultSpPerBug");

            migrationBuilder.RenameColumn(
                name: "XrayEnabled",
                table: "AppSettings",
                newName: "Xray_Enabled");

            migrationBuilder.RenameColumn(
                name: "XrayClientId",
                table: "AppSettings",
                newName: "Xray_ClientId");

            migrationBuilder.RenameColumn(
                name: "XrayClientSecret",
                table: "AppSettings",
                newName: "Xray_ClientSecret");

            migrationBuilder.RenameColumn(
                name: "CycleTimeStartStage",
                table: "AppSettings",
                newName: "CycleTime_StartStage");

            migrationBuilder.RenameColumn(
                name: "CycleTimeEndStage",
                table: "AppSettings",
                newName: "CycleTime_EndStage");

            migrationBuilder.RenameColumn(
                name: "SyncBackSprintCount",
                table: "AppSettings",
                newName: "Sync_BackSprintCount");

            migrationBuilder.RenameColumn(
                name: "PlanningWindowDays",
                table: "AppSettings",
                newName: "Sync_PlanningWindowDays");

            // Correct the store default: original column was created with defaultValue: 0
            // (see 20260509203929_AddPlanningWindowDays). AlterColumn aligns it with
            // HasDefaultValue(2) in AppSettingsConfiguration and SyncConfig initializer.
            migrationBuilder.AlterColumn<int>(
                name: "Sync_PlanningWindowDays",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BugRatio_AlertThreshold",
                table: "AppSettings",
                newName: "BugRatioAlertThreshold");

            migrationBuilder.RenameColumn(
                name: "BugRatio_ConsecutiveSprintCount",
                table: "AppSettings",
                newName: "BugRatioConsecutiveSprintCount");

            migrationBuilder.RenameColumn(
                name: "BugRatio_Target",
                table: "AppSettings",
                newName: "BugRatioTarget");

            migrationBuilder.RenameColumn(
                name: "BugRatio_DefaultSpPerBug",
                table: "AppSettings",
                newName: "DefaultSpPerBug");

            migrationBuilder.RenameColumn(
                name: "Xray_Enabled",
                table: "AppSettings",
                newName: "XrayEnabled");

            migrationBuilder.RenameColumn(
                name: "Xray_ClientId",
                table: "AppSettings",
                newName: "XrayClientId");

            migrationBuilder.RenameColumn(
                name: "Xray_ClientSecret",
                table: "AppSettings",
                newName: "XrayClientSecret");

            migrationBuilder.RenameColumn(
                name: "CycleTime_StartStage",
                table: "AppSettings",
                newName: "CycleTimeStartStage");

            migrationBuilder.RenameColumn(
                name: "CycleTime_EndStage",
                table: "AppSettings",
                newName: "CycleTimeEndStage");

            migrationBuilder.RenameColumn(
                name: "Sync_BackSprintCount",
                table: "AppSettings",
                newName: "SyncBackSprintCount");

            migrationBuilder.RenameColumn(
                name: "Sync_PlanningWindowDays",
                table: "AppSettings",
                newName: "PlanningWindowDays");

            // Restore original store default of 0 on the way down.
            migrationBuilder.AlterColumn<int>(
                name: "PlanningWindowDays",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 2);
        }
    }
}
