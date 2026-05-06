using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sixram.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfServiceManagerPortalAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedTimeIn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RequestedTimeOut = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RequestedRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrentApproverUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReviewerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_AspNetUsers_CurrentApproverUserId",
                        column: x => x.CurrentApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeProfileChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FieldChangesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReviewerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfileChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeProfileChangeRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeProfileChangeRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeProfileChangeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_AttendanceDate",
                table: "AttendanceAdjustmentRequests",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_AttendanceRecordId",
                table: "AttendanceAdjustmentRequests",
                column: "AttendanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_CurrentApproverUserId",
                table: "AttendanceAdjustmentRequests",
                column: "CurrentApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_EmployeeId",
                table: "AttendanceAdjustmentRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_RequestedByUserId",
                table: "AttendanceAdjustmentRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_ReviewedByUserId",
                table: "AttendanceAdjustmentRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_Status",
                table: "AttendanceAdjustmentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfileChangeRequests_CreatedAtUtc",
                table: "EmployeeProfileChangeRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfileChangeRequests_EmployeeId",
                table: "EmployeeProfileChangeRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfileChangeRequests_RequestedByUserId",
                table: "EmployeeProfileChangeRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfileChangeRequests_ReviewedByUserId",
                table: "EmployeeProfileChangeRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfileChangeRequests_Status",
                table: "EmployeeProfileChangeRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAtUtc",
                table: "Notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReadAtUtc",
                table: "Notifications",
                column: "ReadAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_ReadAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "ReadAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAdjustmentRequests");

            migrationBuilder.DropTable(
                name: "EmployeeProfileChangeRequests");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
