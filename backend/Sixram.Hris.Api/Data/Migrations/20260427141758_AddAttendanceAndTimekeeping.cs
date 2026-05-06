using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sixram.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAndTimekeeping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ScheduledEndTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ActualTimeIn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ActualTimeOut = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    BreakStartTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    BreakEndTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    TotalWorkedMinutes = table.Column<int>(type: "int", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    UndertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    BreakStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    RequiredWorkingMinutes = table.Column<int>(type: "int", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    IsOvernight = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequiredWorkingMinutes = table.Column<int>(type: "int", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    BreakDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeScheduleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RestDays = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeScheduleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeScheduleAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeScheduleAssignments_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeScheduleAssignments_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceDate",
                table: "AttendanceRecords",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CreatedByUserId",
                table: "AttendanceRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_AttendanceDate",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Source",
                table: "AttendanceRecords",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Status",
                table: "AttendanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UpdatedByUserId",
                table: "AttendanceRecords",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeScheduleAssignments_EmployeeId",
                table: "EmployeeScheduleAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeScheduleAssignments_EmployeeId_EffectiveStartDate_EffectiveEndDate",
                table: "EmployeeScheduleAssignments",
                columns: new[] { "EmployeeId", "EffectiveStartDate", "EffectiveEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeScheduleAssignments_IsActive",
                table: "EmployeeScheduleAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeScheduleAssignments_ShiftId",
                table: "EmployeeScheduleAssignments",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeScheduleAssignments_WorkScheduleId",
                table: "EmployeeScheduleAssignments",
                column: "WorkScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Code",
                table: "Shifts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Name",
                table: "Shifts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_Code",
                table: "WorkSchedules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_Name",
                table: "WorkSchedules",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "EmployeeScheduleAssignments");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "WorkSchedules");
        }
    }
}
