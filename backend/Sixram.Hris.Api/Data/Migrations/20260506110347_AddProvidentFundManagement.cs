using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sixram.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProvidentFundManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProvidentFundPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EligibilityRules = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EmployeeContributionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeContributionValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EmployerContributionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployerContributionValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ContributionFrequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AllowVoluntaryContribution = table.Column<bool>(type: "bit", nullable: false),
                    AllowWithdrawal = table.Column<bool>(type: "bit", nullable: false),
                    AllowLoan = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundPolicies_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundPolicies_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundContributionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsSupplemental = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    PostedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PostingDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundContributionBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatches_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatches_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatches_ProvidentFundPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "ProvidentFundPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VestingStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EmployeeContributionOverrideType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeContributionOverrideValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EmployerContributionOverrideType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployerContributionOverrideValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundEnrollments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundEnrollments_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundEnrollments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProvidentFundEnrollments_ProvidentFundPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "ProvidentFundPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundVestingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YearsOfService = table.Column<int>(type: "int", nullable: false),
                    VestedPercentage = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundVestingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundVestingRules_ProvidentFundPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "ProvidentFundPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustmentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AdjustmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShareAffected = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    DecisionRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundAdjustments_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundAdjustments_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProvidentFundAdjustments_ProvidentFundEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "ProvidentFundEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundContributionBatchLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployeeContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VoluntaryContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundContributionBatchLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatchLines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatchLines_ProvidentFundContributionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProvidentFundContributionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProvidentFundContributionBatchLines_ProvidentFundEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "ProvidentFundEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundWithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WithdrawalType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EligibleWithdrawableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundWithdrawalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundWithdrawalRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundWithdrawalRequests_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundWithdrawalRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProvidentFundWithdrawalRequests_ProvidentFundEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "ProvidentFundEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundAdjustmentApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActorNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundAdjustmentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundAdjustmentApprovals_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundAdjustmentApprovals_ProvidentFundAdjustments_AdjustmentId",
                        column: x => x.AdjustmentId,
                        principalTable: "ProvidentFundAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundLedgerTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContributionBatchLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceReferenceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EmployeeShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VoluntaryShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversalReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundLedgerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_ProvidentFundContributionBatchLines_ContributionBatchLineId",
                        column: x => x.ContributionBatchLineId,
                        principalTable: "ProvidentFundContributionBatchLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_ProvidentFundContributionBatches_ContributionBatchId",
                        column: x => x.ContributionBatchId,
                        principalTable: "ProvidentFundContributionBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_ProvidentFundEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "ProvidentFundEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_ProvidentFundLedgerTransactions_ReversalReferenceId",
                        column: x => x.ReversalReferenceId,
                        principalTable: "ProvidentFundLedgerTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundLedgerTransactions_ProvidentFundPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "ProvidentFundPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvidentFundWithdrawalApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActorNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvidentFundWithdrawalApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvidentFundWithdrawalApprovals_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProvidentFundWithdrawalApprovals_ProvidentFundWithdrawalRequests_WithdrawalRequestId",
                        column: x => x.WithdrawalRequestId,
                        principalTable: "ProvidentFundWithdrawalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustmentApprovals_ActorUserId",
                table: "ProvidentFundAdjustmentApprovals",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustmentApprovals_AdjustmentId",
                table: "ProvidentFundAdjustmentApprovals",
                column: "AdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_AdjustmentDate",
                table: "ProvidentFundAdjustments",
                column: "AdjustmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_ApprovedByUserId",
                table: "ProvidentFundAdjustments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_EmployeeId",
                table: "ProvidentFundAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_EnrollmentId",
                table: "ProvidentFundAdjustments",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_RequestedByUserId",
                table: "ProvidentFundAdjustments",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_ShareAffected",
                table: "ProvidentFundAdjustments",
                column: "ShareAffected");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundAdjustments_Status",
                table: "ProvidentFundAdjustments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_BatchNumber",
                table: "ProvidentFundContributionBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_CreatedByUserId",
                table: "ProvidentFundContributionBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_PolicyId",
                table: "ProvidentFundContributionBatches",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_PostedByUserId",
                table: "ProvidentFundContributionBatches",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_ReviewedByUserId",
                table: "ProvidentFundContributionBatches",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_Status",
                table: "ProvidentFundContributionBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatches_Year_Month_PolicyId",
                table: "ProvidentFundContributionBatches",
                columns: new[] { "Year", "Month", "PolicyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatchLines_BatchId",
                table: "ProvidentFundContributionBatchLines",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatchLines_EmployeeId",
                table: "ProvidentFundContributionBatchLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundContributionBatchLines_EnrollmentId",
                table: "ProvidentFundContributionBatchLines",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundEnrollments_CreatedByUserId",
                table: "ProvidentFundEnrollments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundEnrollments_EmployeeId",
                table: "ProvidentFundEnrollments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundEnrollments_EmployeeId_Status",
                table: "ProvidentFundEnrollments",
                columns: new[] { "EmployeeId", "Status" },
                unique: true,
                filter: "[Status] = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundEnrollments_PolicyId",
                table: "ProvidentFundEnrollments",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundEnrollments_Status",
                table: "ProvidentFundEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundEnrollments_UpdatedByUserId",
                table: "ProvidentFundEnrollments",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_ContributionBatchId",
                table: "ProvidentFundLedgerTransactions",
                column: "ContributionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_ContributionBatchLineId",
                table: "ProvidentFundLedgerTransactions",
                column: "ContributionBatchLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_CreatedByUserId",
                table: "ProvidentFundLedgerTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_EmployeeId",
                table: "ProvidentFundLedgerTransactions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_EnrollmentId",
                table: "ProvidentFundLedgerTransactions",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_PolicyId",
                table: "ProvidentFundLedgerTransactions",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_ReversalReferenceId",
                table: "ProvidentFundLedgerTransactions",
                column: "ReversalReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_SourceType_SourceReferenceId",
                table: "ProvidentFundLedgerTransactions",
                columns: new[] { "SourceType", "SourceReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_TransactionDate",
                table: "ProvidentFundLedgerTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_TransactionNumber",
                table: "ProvidentFundLedgerTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundLedgerTransactions_TransactionType",
                table: "ProvidentFundLedgerTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundPolicies_CreatedByUserId",
                table: "ProvidentFundPolicies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundPolicies_PolicyName",
                table: "ProvidentFundPolicies",
                column: "PolicyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundPolicies_Status",
                table: "ProvidentFundPolicies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundPolicies_UpdatedByUserId",
                table: "ProvidentFundPolicies",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundVestingRules_PolicyId_YearsOfService",
                table: "ProvidentFundVestingRules",
                columns: new[] { "PolicyId", "YearsOfService" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalApprovals_ActorUserId",
                table: "ProvidentFundWithdrawalApprovals",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalApprovals_WithdrawalRequestId",
                table: "ProvidentFundWithdrawalApprovals",
                column: "WithdrawalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_CreatedByUserId",
                table: "ProvidentFundWithdrawalRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_EmployeeId",
                table: "ProvidentFundWithdrawalRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_EnrollmentId",
                table: "ProvidentFundWithdrawalRequests",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_RequestDate",
                table: "ProvidentFundWithdrawalRequests",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_RequestNumber",
                table: "ProvidentFundWithdrawalRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_Status",
                table: "ProvidentFundWithdrawalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_UpdatedByUserId",
                table: "ProvidentFundWithdrawalRequests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvidentFundWithdrawalRequests_WithdrawalType",
                table: "ProvidentFundWithdrawalRequests",
                column: "WithdrawalType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProvidentFundAdjustmentApprovals");

            migrationBuilder.DropTable(
                name: "ProvidentFundLedgerTransactions");

            migrationBuilder.DropTable(
                name: "ProvidentFundVestingRules");

            migrationBuilder.DropTable(
                name: "ProvidentFundWithdrawalApprovals");

            migrationBuilder.DropTable(
                name: "ProvidentFundAdjustments");

            migrationBuilder.DropTable(
                name: "ProvidentFundContributionBatchLines");

            migrationBuilder.DropTable(
                name: "ProvidentFundWithdrawalRequests");

            migrationBuilder.DropTable(
                name: "ProvidentFundContributionBatches");

            migrationBuilder.DropTable(
                name: "ProvidentFundEnrollments");

            migrationBuilder.DropTable(
                name: "ProvidentFundPolicies");
        }
    }
}
