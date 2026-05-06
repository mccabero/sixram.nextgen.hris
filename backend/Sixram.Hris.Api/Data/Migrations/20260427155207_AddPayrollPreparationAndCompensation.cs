using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sixram.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPreparationAndCompensation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompensationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PayFrequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompensationProfiles_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompensationProfiles_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompensationProfiles_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EmployeeShareApplicable = table.Column<bool>(type: "bit", nullable: false),
                    EmployerShareApplicable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeductionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PreTax = table.Column<bool>(type: "bit", nullable: false),
                    Recurring = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EarningTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Taxable = table.Column<bool>(type: "bit", nullable: false),
                    Recurring = table.Column<bool>(type: "bit", nullable: false),
                    AffectsThirteenthMonth = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayPeriodTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PayFrequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PeriodLengthDays = table.Column<int>(type: "int", nullable: false),
                    PayrollOffsetDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPeriodTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayFrequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernmentContributionTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentContributionTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernmentContributionTables_ContributionTypes_ContributionTypeId",
                        column: x => x.ContributionTypeId,
                        principalTable: "ContributionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRecurringDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeductionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRecurringDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRecurringDeductions_DeductionTypes_DeductionTypeId",
                        column: x => x.DeductionTypeId,
                        principalTable: "DeductionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeRecurringDeductions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRecurringEarnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarningTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRecurringEarnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRecurringEarnings_EarningTypes_EarningTypeId",
                        column: x => x.EarningTypeId,
                        principalTable: "EarningTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeRecurringEarnings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayFrequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PayrollDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CutoffStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CutoffEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PayPeriodTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayPeriods_PayPeriodTemplates_PayPeriodTemplateId",
                        column: x => x.PayPeriodTemplateId,
                        principalTable: "PayPeriodTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaxBrackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinTaxableIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxTaxableIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaseTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExcessOver = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxBrackets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxBrackets_TaxTables_TaxTableId",
                        column: x => x.TaxTableId,
                        principalTable: "TaxTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GovernmentContributionBrackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernmentContributionTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinCompensation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxCompensation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EmployeeShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EmployeeShareRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    EmployerShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EmployerShareRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentContributionBrackets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernmentContributionBrackets_GovernmentContributionTables_GovernmentContributionTableId",
                        column: x => x.GovernmentContributionTableId,
                        principalTable: "GovernmentContributionTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GeneratedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollRuns_AspNetUsers_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollRuns_PayPeriods_PayPeriodId",
                        column: x => x.PayPeriodId,
                        principalTable: "PayPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdjustmentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EarningTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeductionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    DecisionRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_DeductionTypes_DeductionTypeId",
                        column: x => x.DeductionTypeId,
                        principalTable: "DeductionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_EarningTypes_EarningTypeId",
                        column: x => x.EarningTypeId,
                        principalTable: "EarningTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_PayPeriods_PayPeriodId",
                        column: x => x.PayPeriodId,
                        principalTable: "PayPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompensationProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeCodeSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DepartmentSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PositionSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BranchSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayTypeSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrencySnapshot = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    BasicSalarySnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DailyRateSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    HourlyRateSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RegularWorkedDays = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegularWorkedHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidLeaveDays = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnpaidLeaveDays = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AbsentDays = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    UndertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    BasicPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowanceTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimePay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HolidayPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LeavePay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherEarningsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GovernmentDeductionsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AbsenceDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UndertimeDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoanDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherDeductionsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerContributionTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HasCriticalIssues = table.Column<bool>(type: "bit", nullable: false),
                    IssueSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunItems_CompensationProfiles_CompensationProfileId",
                        column: x => x.CompensationProfileId,
                        principalTable: "CompensationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollRunItems_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRunItems_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayrollRunItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAuditLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollAuditLogs_PayrollRunItems_PayrollRunItemId",
                        column: x => x.PayrollRunItemId,
                        principalTable: "PayrollRunItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollAuditLogs_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDeductionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeductionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeductionTypeCodeSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeductionTypeNameSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DeductionCategorySnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PreTax = table.Column<bool>(type: "bit", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    PayrollAdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeRecurringDeductionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDeductionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollDeductionLines_DeductionTypes_DeductionTypeId",
                        column: x => x.DeductionTypeId,
                        principalTable: "DeductionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollDeductionLines_EmployeeRecurringDeductions_EmployeeRecurringDeductionId",
                        column: x => x.EmployeeRecurringDeductionId,
                        principalTable: "EmployeeRecurringDeductions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollDeductionLines_PayrollAdjustments_PayrollAdjustmentId",
                        column: x => x.PayrollAdjustmentId,
                        principalTable: "PayrollAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollDeductionLines_PayrollRunItems_PayrollRunItemId",
                        column: x => x.PayrollRunItemId,
                        principalTable: "PayrollRunItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollEarningLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarningTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EarningTypeCodeSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EarningTypeNameSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Taxable = table.Column<bool>(type: "bit", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    PayrollAdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeRecurringEarningId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollEarningLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_EarningTypes_EarningTypeId",
                        column: x => x.EarningTypeId,
                        principalTable: "EarningTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_EmployeeRecurringEarnings_EmployeeRecurringEarningId",
                        column: x => x.EmployeeRecurringEarningId,
                        principalTable: "EmployeeRecurringEarnings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_PayrollAdjustments_PayrollAdjustmentId",
                        column: x => x.PayrollAdjustmentId,
                        principalTable: "PayrollAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_PayrollRunItems_PayrollRunItemId",
                        column: x => x.PayrollRunItemId,
                        principalTable: "PayrollRunItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompensationProfiles_CreatedByUserId",
                table: "CompensationProfiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationProfiles_EmployeeId",
                table: "CompensationProfiles",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationProfiles_EmployeeId_EffectiveStartDate_EffectiveEndDate",
                table: "CompensationProfiles",
                columns: new[] { "EmployeeId", "EffectiveStartDate", "EffectiveEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CompensationProfiles_IsActive",
                table: "CompensationProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationProfiles_UpdatedByUserId",
                table: "CompensationProfiles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionTypes_Code",
                table: "ContributionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContributionTypes_Name",
                table: "ContributionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_Category",
                table: "DeductionTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_Code",
                table: "DeductionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_Name",
                table: "DeductionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EarningTypes_Category",
                table: "EarningTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EarningTypes_Code",
                table: "EarningTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EarningTypes_Name",
                table: "EarningTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringDeductions_DeductionTypeId",
                table: "EmployeeRecurringDeductions",
                column: "DeductionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringDeductions_EmployeeId",
                table: "EmployeeRecurringDeductions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringDeductions_EmployeeId_EffectiveStartDate_EffectiveEndDate",
                table: "EmployeeRecurringDeductions",
                columns: new[] { "EmployeeId", "EffectiveStartDate", "EffectiveEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringDeductions_IsActive",
                table: "EmployeeRecurringDeductions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringEarnings_EarningTypeId",
                table: "EmployeeRecurringEarnings",
                column: "EarningTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringEarnings_EmployeeId",
                table: "EmployeeRecurringEarnings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringEarnings_EmployeeId_EffectiveStartDate_EffectiveEndDate",
                table: "EmployeeRecurringEarnings",
                columns: new[] { "EmployeeId", "EffectiveStartDate", "EffectiveEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringEarnings_IsActive",
                table: "EmployeeRecurringEarnings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentContributionBrackets_GovernmentContributionTableId",
                table: "GovernmentContributionBrackets",
                column: "GovernmentContributionTableId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentContributionTables_ContributionTypeId",
                table: "GovernmentContributionTables",
                column: "ContributionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentContributionTables_ContributionTypeId_EffectiveStartDate_EffectiveEndDate",
                table: "GovernmentContributionTables",
                columns: new[] { "ContributionTypeId", "EffectiveStartDate", "EffectiveEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriods_Code",
                table: "PayPeriods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriods_PayPeriodTemplateId",
                table: "PayPeriods",
                column: "PayPeriodTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriods_PayrollDate",
                table: "PayPeriods",
                column: "PayrollDate");

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriods_PeriodStartDate_PeriodEndDate",
                table: "PayPeriods",
                columns: new[] { "PeriodStartDate", "PeriodEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriods_Status",
                table: "PayPeriods",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriodTemplates_Code",
                table: "PayPeriodTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriodTemplates_IsActive",
                table: "PayPeriodTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriodTemplates_Name",
                table: "PayPeriodTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_ApprovedByUserId",
                table: "PayrollAdjustments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_DeductionTypeId",
                table: "PayrollAdjustments",
                column: "DeductionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_EarningTypeId",
                table: "PayrollAdjustments",
                column: "EarningTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_EmployeeId",
                table: "PayrollAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_PayPeriodId",
                table: "PayrollAdjustments",
                column: "PayPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_PayrollRunId",
                table: "PayrollAdjustments",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_RequestedByUserId",
                table: "PayrollAdjustments",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_Status",
                table: "PayrollAdjustments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditLogs_ActorUserId",
                table: "PayrollAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditLogs_CreatedAtUtc",
                table: "PayrollAuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditLogs_PayrollRunId",
                table: "PayrollAuditLogs",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditLogs_PayrollRunItemId",
                table: "PayrollAuditLogs",
                column: "PayrollRunItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductionLines_DeductionTypeId",
                table: "PayrollDeductionLines",
                column: "DeductionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductionLines_EmployeeRecurringDeductionId",
                table: "PayrollDeductionLines",
                column: "EmployeeRecurringDeductionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductionLines_PayrollAdjustmentId",
                table: "PayrollDeductionLines",
                column: "PayrollAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductionLines_PayrollRunItemId",
                table: "PayrollDeductionLines",
                column: "PayrollRunItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductionLines_Source",
                table: "PayrollDeductionLines",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_EarningTypeId",
                table: "PayrollEarningLines",
                column: "EarningTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_EmployeeRecurringEarningId",
                table: "PayrollEarningLines",
                column: "EmployeeRecurringEarningId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_PayrollAdjustmentId",
                table: "PayrollEarningLines",
                column: "PayrollAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_PayrollRunItemId",
                table: "PayrollEarningLines",
                column: "PayrollRunItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_Source",
                table: "PayrollEarningLines",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunItems_CompensationProfileId",
                table: "PayrollRunItems",
                column: "CompensationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunItems_EmployeeId",
                table: "PayrollRunItems",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunItems_HasCriticalIssues",
                table: "PayrollRunItems",
                column: "HasCriticalIssues");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunItems_PayrollRunId_EmployeeId",
                table: "PayrollRunItems",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunItems_Status",
                table: "PayrollRunItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_ApprovedByUserId",
                table: "PayrollRuns",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_GeneratedAtUtc",
                table: "PayrollRuns",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_GeneratedByUserId",
                table: "PayrollRuns",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayPeriodId",
                table: "PayrollRuns",
                column: "PayPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_ReferenceNumber",
                table: "PayrollRuns",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Status",
                table: "PayrollRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettings_Key",
                table: "PayrollSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxBrackets_TaxTableId",
                table: "TaxBrackets",
                column: "TaxTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxTables_Code",
                table: "TaxTables",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxTables_PayFrequency_EffectiveStartDate_EffectiveEndDate",
                table: "TaxTables",
                columns: new[] { "PayFrequency", "EffectiveStartDate", "EffectiveEndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GovernmentContributionBrackets");

            migrationBuilder.DropTable(
                name: "PayrollAuditLogs");

            migrationBuilder.DropTable(
                name: "PayrollDeductionLines");

            migrationBuilder.DropTable(
                name: "PayrollEarningLines");

            migrationBuilder.DropTable(
                name: "PayrollSettings");

            migrationBuilder.DropTable(
                name: "TaxBrackets");

            migrationBuilder.DropTable(
                name: "GovernmentContributionTables");

            migrationBuilder.DropTable(
                name: "EmployeeRecurringDeductions");

            migrationBuilder.DropTable(
                name: "EmployeeRecurringEarnings");

            migrationBuilder.DropTable(
                name: "PayrollAdjustments");

            migrationBuilder.DropTable(
                name: "PayrollRunItems");

            migrationBuilder.DropTable(
                name: "TaxTables");

            migrationBuilder.DropTable(
                name: "ContributionTypes");

            migrationBuilder.DropTable(
                name: "DeductionTypes");

            migrationBuilder.DropTable(
                name: "EarningTypes");

            migrationBuilder.DropTable(
                name: "CompensationProfiles");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropTable(
                name: "PayPeriods");

            migrationBuilder.DropTable(
                name: "PayPeriodTemplates");
        }
    }
}
