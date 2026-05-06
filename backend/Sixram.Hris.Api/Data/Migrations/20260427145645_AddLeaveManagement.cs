using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sixram.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LeaveRequestId",
                table: "AttendanceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAttachment = table.Column<bool>(type: "bit", nullable: false),
                    RequiresReason = table.Column<bool>(type: "bit", nullable: false),
                    AllowHalfDay = table.Column<bool>(type: "bit", nullable: false),
                    AllowNegativeBalance = table.Column<bool>(type: "bit", nullable: false),
                    DefaultAnnualCredits = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    MaxDaysPerRequest = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    MinDaysBeforeFiling = table.Column<int>(type: "int", nullable: true),
                    GenderRestriction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmploymentTypeRestrictions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CountsRestDays = table.Column<bool>(type: "bit", nullable: false),
                    CountsHolidays = table.Column<bool>(type: "bit", nullable: false),
                    AllowDuringProbationaryPeriod = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLeaveBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Accrued = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Used = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Pending = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Adjusted = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CarriedForward = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDayType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EndDayType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TotalLeaveDays = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AttachmentOriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    AttachmentMimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AttachmentFileSize = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CurrentApproverUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DecisionRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_AspNetUsers_CurrentApproverUserId",
                        column: x => x.CurrentApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalanceTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalanceTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceTransactions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveBalanceTransactions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceTransactions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceTransactions_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_LeaveRequestId",
                table: "AttendanceRecords",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId",
                table: "EmployeeLeaveBalances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_PeriodYear",
                table: "EmployeeLeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "PeriodYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_LeaveTypeId",
                table: "EmployeeLeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_PeriodYear",
                table: "EmployeeLeaveBalances",
                column: "PeriodYear");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_CreatedAtUtc",
                table: "LeaveBalanceTransactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_CreatedByUserId",
                table: "LeaveBalanceTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_EmployeeId",
                table: "LeaveBalanceTransactions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_LeaveRequestId",
                table: "LeaveBalanceTransactions",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_LeaveTypeId",
                table: "LeaveBalanceTransactions",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_PeriodYear",
                table: "LeaveBalanceTransactions",
                column: "PeriodYear");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceTransactions_TransactionType",
                table: "LeaveBalanceTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CreatedByUserId",
                table: "LeaveRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CurrentApproverUserId",
                table: "LeaveRequests",
                column: "CurrentApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StartDate_EndDate",
                table: "LeaveRequests",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_UpdatedByUserId",
                table: "LeaveRequests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Name",
                table: "LeaveTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_LeaveRequests_LeaveRequestId",
                table: "AttendanceRecords",
                column: "LeaveRequestId",
                principalTable: "LeaveRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_LeaveRequests_LeaveRequestId",
                table: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "EmployeeLeaveBalances");

            migrationBuilder.DropTable(
                name: "LeaveBalanceTransactions");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_LeaveRequestId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "LeaveRequestId",
                table: "AttendanceRecords");
        }
    }
}
