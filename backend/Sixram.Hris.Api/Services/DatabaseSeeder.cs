using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.Constants;
using Sixram.Api.Entities;

namespace Sixram.Api.Services;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public class DatabaseSeeder : IDatabaseSeeder
{
    private const string AdminEmail = "admin@sixram.local";
    private const string AdminPassword = "ChangeMe123!";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        ILogger<DatabaseSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureRoleAsync(SystemRoles.Administrator, "Full administrative access to Sixram HRIS.", cancellationToken);
        await EnsureRoleAsync(SystemRoles.HumanResources, "HR operations, employee services, and approval handling.", cancellationToken);
        await EnsureRoleAsync(SystemRoles.Manager, "Manager self-service access for assigned teams.", cancellationToken);
        await EnsureRoleAsync(SystemRoles.PayrollOfficer, "Payroll preparation and compensation administration access.", cancellationToken);
        await EnsureRoleAsync(SystemRoles.User, "Standard authenticated user access.", cancellationToken);

        var adminUser = await _userManager.FindByEmailAsync(AdminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                DisplayName = "Sixram HRIS Administrator",
                EmailConfirmed = true,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(adminUser, AdminPassword);
            ThrowIfIdentityFailed(createResult, "Unable to seed the default administrator user.");

            _logger.LogInformation("Seeded default administrator user {Email}.", AdminEmail);
        }
        else
        {
            adminUser.UserName = AdminEmail;
            adminUser.Email = AdminEmail;
            adminUser.DisplayName = string.IsNullOrWhiteSpace(adminUser.DisplayName) ? "Sixram HRIS Administrator" : adminUser.DisplayName;
            adminUser.EmailConfirmed = true;
            adminUser.IsEnabled = true;
            adminUser.UpdatedAtUtc = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(adminUser);
            ThrowIfIdentityFailed(updateResult, "Unable to update the default administrator seed user.");
        }

        if (_environment.IsDevelopment() && !await _userManager.CheckPasswordAsync(adminUser, AdminPassword))
        {
            var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
            var resetResult = await _userManager.ResetPasswordAsync(adminUser, passwordResetToken, AdminPassword);
            ThrowIfIdentityFailed(resetResult, "Unable to reset the default administrator password for development.");

            _logger.LogInformation("Reset the development administrator password for {Email}.", AdminEmail);
        }

        if (!await _userManager.IsInRoleAsync(adminUser, SystemRoles.Administrator))
        {
            var adminRoleResult = await _userManager.AddToRoleAsync(adminUser, SystemRoles.Administrator);
            ThrowIfIdentityFailed(adminRoleResult, "Unable to assign the Administrator role to the default admin user.");
        }

        if (!await _userManager.IsInRoleAsync(adminUser, SystemRoles.User))
        {
            var userRoleResult = await _userManager.AddToRoleAsync(adminUser, SystemRoles.User);
            ThrowIfIdentityFailed(userRoleResult, "Unable to assign the User role to the default admin user.");
        }

        await EnsureOrganizationSeedAsync(cancellationToken);
        await EnsureDocumentTypeSeedAsync(cancellationToken);
        await EnsureAttendanceSeedAsync(cancellationToken);
        await EnsureLeaveSeedAsync(cancellationToken);
        await EnsurePayrollSeedAsync(cancellationToken);
        await EnsureProvidentFundSeedAsync(cancellationToken);
    }

    private async Task EnsureRoleAsync(string roleName, string description, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            var createResult = await _roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow
            });

            ThrowIfIdentityFailed(createResult, $"Unable to seed role '{roleName}'.");
            _logger.LogInformation("Seeded role {RoleName}.", roleName);
            return;
        }

        role.Description = description;
        role.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _roleManager.UpdateAsync(role);
        ThrowIfIdentityFailed(updateResult, $"Unable to update role seed '{roleName}'.");
    }

    private async Task EnsureOrganizationSeedAsync(CancellationToken cancellationToken)
    {
        var departments = new[]
        {
            new Department { Code = "HR", Name = "Human Resources", Description = "People operations, recruitment, and employee relations." },
            new Department { Code = "IT", Name = "Information Technology", Description = "Business systems, engineering, and infrastructure support." },
            new Department { Code = "FIN", Name = "Finance", Description = "Accounting, treasury, and financial controls." },
            new Department { Code = "OPS", Name = "Operations", Description = "Operational delivery, service coordination, and execution." }
        };

        foreach (var department in departments)
        {
            await UpsertDepartmentAsync(department, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var departmentIds = await _dbContext.Departments
            .AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record.Id, cancellationToken);

        var positions = new[]
        {
            new Position { Code = "HR-MGR", Name = "HR Manager", Description = "Leads people operations and employee services.", DepartmentId = departmentIds["HR"] },
            new Position { Code = "SYS-ADMIN", Name = "Systems Administrator", Description = "Maintains application and infrastructure operations.", DepartmentId = departmentIds["IT"] },
            new Position { Code = "ACCOUNTANT", Name = "Accountant", Description = "Handles accounting operations and reconciliations.", DepartmentId = departmentIds["FIN"] },
            new Position { Code = "OPS-ANL", Name = "Operations Analyst", Description = "Supports service coordination and operating metrics.", DepartmentId = departmentIds["OPS"] }
        };

        foreach (var position in positions)
        {
            await UpsertPositionAsync(position, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var branches = new[]
        {
            new Branch { Code = "HQ", Name = "Head Office", Description = "Primary corporate office.", Address = "Sixram HRIS Headquarters" },
            new Branch { Code = "NORTH", Name = "North Branch", Description = "Northern area operations.", Address = "North Regional Office" },
            new Branch { Code = "SOUTH", Name = "South Branch", Description = "Southern area operations.", Address = "South Regional Office" }
        };

        foreach (var branch in branches)
        {
            await UpsertBranchAsync(branch, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var employmentTypes = new[]
        {
            new EmploymentType { Code = "REG", Name = "Regular", Description = "Standard ongoing employment." },
            new EmploymentType { Code = "PROB", Name = "Probationary", Description = "Initial probationary employment arrangement." },
            new EmploymentType { Code = "CONT", Name = "Contractual", Description = "Fixed-term or contractual engagement." }
        };

        foreach (var employmentType in employmentTypes)
        {
            await UpsertEmploymentTypeAsync(employmentType, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var employmentStatuses = new[]
        {
            new EmploymentStatus { Code = "ACTIVE", Name = "Active", Description = "Currently active in service." },
            new EmploymentStatus { Code = "PROB", Name = "Probationary", Description = "Still within the probation period." },
            new EmploymentStatus { Code = "REGULAR", Name = "Regularized", Description = "Successfully regularized after probation." },
            new EmploymentStatus { Code = "RESIGNED", Name = "Resigned", Description = "Left the organization voluntarily." },
            new EmploymentStatus { Code = "TERMINATED", Name = "Terminated", Description = "Separated from service involuntarily." }
        };

        foreach (var employmentStatus in employmentStatuses)
        {
            await UpsertEmploymentStatusAsync(employmentStatus, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDocumentTypeSeedAsync(CancellationToken cancellationToken)
    {
        var documentTypes = new[]
        {
            new DocumentType { Code = "RESUME", Name = "Resume", Description = "Candidate or employee curriculum vitae and profile summary.", IsRequired = true },
            new DocumentType { Code = "CONTRACT", Name = "Employment Contract", Description = "Employment agreement and related onboarding contract.", IsRequired = true },
            new DocumentType { Code = "GOV-ID", Name = "Government ID", Description = "Government-issued identification documents for employee records.", RequiresExpiryDate = true, IsRequired = true },
            new DocumentType { Code = "CERT", Name = "Certificate", Description = "Professional, academic, and qualification certificates." },
            new DocumentType { Code = "MEDICAL", Name = "Medical Record", Description = "Medical documents and health clearance records." },
            new DocumentType { Code = "CLEARANCE", Name = "Clearance", Description = "Company, exit, or department clearance documents." },
            new DocumentType { Code = "TRAINING", Name = "Training Document", Description = "Training attendance records and completion documents." },
            new DocumentType { Code = "OTHER", Name = "Other", Description = "Other employee documents not covered by the default types." }
        };

        foreach (var documentType in documentTypes)
        {
            await UpsertDocumentTypeAsync(documentType, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAttendanceSeedAsync(CancellationToken cancellationToken)
    {
        var workSchedules = new[]
        {
            new WorkSchedule
            {
                Code = "FIXED-8H",
                Name = "Fixed 8-Hour Schedule",
                Description = "Standard fixed daily work schedule for office-based employees.",
                ScheduleType = AttendanceScheduleTypes.Fixed,
                RequiredWorkingMinutes = 480,
                GracePeriodMinutes = 10,
                BreakDurationMinutes = 60
            },
            new WorkSchedule
            {
                Code = "FLEX-8H",
                Name = "Flexible 8-Hour Schedule",
                Description = "Flexible daily work schedule with required total working hours.",
                ScheduleType = AttendanceScheduleTypes.Flexible,
                RequiredWorkingMinutes = 480,
                GracePeriodMinutes = 0,
                BreakDurationMinutes = 60
            },
            new WorkSchedule
            {
                Code = "SHIFT-8H",
                Name = "Shifting 8-Hour Schedule",
                Description = "Shift-based work schedule for rotating or assigned shifts.",
                ScheduleType = AttendanceScheduleTypes.Shifting,
                RequiredWorkingMinutes = 480,
                GracePeriodMinutes = 10,
                BreakDurationMinutes = 60
            }
        };

        foreach (var workSchedule in workSchedules)
        {
            await UpsertWorkScheduleAsync(workSchedule, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var shifts = new[]
        {
            new Shift
            {
                Code = "DAY-0900",
                Name = "Day Shift 9:00 AM - 6:00 PM",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
                BreakStartTime = new TimeOnly(12, 0),
                BreakEndTime = new TimeOnly(13, 0),
                RequiredWorkingMinutes = 480,
                GracePeriodMinutes = 10,
                IsOvernight = false
            },
            new Shift
            {
                Code = "DAY-0800",
                Name = "Day Shift 8:00 AM - 5:00 PM",
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                BreakStartTime = new TimeOnly(12, 0),
                BreakEndTime = new TimeOnly(13, 0),
                RequiredWorkingMinutes = 480,
                GracePeriodMinutes = 10,
                IsOvernight = false
            },
            new Shift
            {
                Code = "NIGHT-2200",
                Name = "Night Shift 10:00 PM - 7:00 AM",
                StartTime = new TimeOnly(22, 0),
                EndTime = new TimeOnly(7, 0),
                BreakStartTime = new TimeOnly(2, 0),
                BreakEndTime = new TimeOnly(3, 0),
                RequiredWorkingMinutes = 480,
                GracePeriodMinutes = 10,
                IsOvernight = true
            }
        };

        foreach (var shift in shifts)
        {
            await UpsertShiftAsync(shift, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLeaveSeedAsync(CancellationToken cancellationToken)
    {
        var leaveTypes = new[]
        {
            new LeaveType
            {
                Code = "VL",
                Name = "Vacation Leave",
                Description = "Paid personal leave for scheduled time away from work.",
                IsPaid = true,
                RequiresReason = true,
                AllowHalfDay = true,
                DefaultAnnualCredits = 10m
            },
            new LeaveType
            {
                Code = "SL",
                Name = "Sick Leave",
                Description = "Paid leave for illness, recovery, or medical appointments.",
                IsPaid = true,
                RequiresReason = true,
                RequiresAttachment = false,
                AllowHalfDay = true,
                DefaultAnnualCredits = 10m
            },
            new LeaveType
            {
                Code = "EL",
                Name = "Emergency Leave",
                Description = "Short-notice leave for urgent personal or family matters.",
                IsPaid = true,
                RequiresReason = true,
                AllowHalfDay = false,
                DefaultAnnualCredits = 3m
            },
            new LeaveType
            {
                Code = "MAT",
                Name = "Maternity Leave",
                Description = "Leave for eligible employees during maternity periods.",
                IsPaid = true,
                RequiresAttachment = true,
                RequiresReason = true,
                AllowHalfDay = false,
                DefaultAnnualCredits = null,
                GenderRestriction = "Female"
            },
            new LeaveType
            {
                Code = "PAT",
                Name = "Paternity Leave",
                Description = "Leave for eligible employees during paternity periods.",
                IsPaid = true,
                RequiresAttachment = true,
                RequiresReason = true,
                AllowHalfDay = false,
                DefaultAnnualCredits = null,
                GenderRestriction = "Male"
            },
            new LeaveType
            {
                Code = "BVL",
                Name = "Bereavement Leave",
                Description = "Leave for bereavement and family loss situations.",
                IsPaid = true,
                RequiresReason = true,
                AllowHalfDay = false,
                DefaultAnnualCredits = 5m
            },
            new LeaveType
            {
                Code = "SPL",
                Name = "Solo Parent Leave",
                Description = "Configurable leave type for eligible solo parent arrangements.",
                IsPaid = true,
                RequiresReason = true,
                AllowHalfDay = true,
                DefaultAnnualCredits = 7m
            },
            new LeaveType
            {
                Code = "SIL",
                Name = "Service Incentive Leave",
                Description = "Configurable leave credits granted as service incentive leave.",
                IsPaid = true,
                RequiresReason = true,
                AllowHalfDay = true,
                DefaultAnnualCredits = 5m
            },
            new LeaveType
            {
                Code = "UL",
                Name = "Unpaid Leave",
                Description = "Unpaid leave that can proceed even without available credits.",
                IsPaid = false,
                RequiresReason = true,
                AllowHalfDay = true,
                AllowNegativeBalance = true,
                DefaultAnnualCredits = 0m
            },
            new LeaveType
            {
                Code = "OTHER",
                Name = "Other",
                Description = "General-purpose leave category for company-specific policies.",
                IsPaid = true,
                RequiresReason = true,
                AllowHalfDay = true,
                DefaultAnnualCredits = null
            }
        };

        foreach (var leaveType in leaveTypes)
        {
            await UpsertLeaveTypeAsync(leaveType, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePayrollSeedAsync(CancellationToken cancellationToken)
    {
        var payrollSettings = new Dictionary<string, (string Value, string Description)>
        {
            [PayrollSettingKeys.DefaultPayFrequency] = (PayrollPayFrequencies.SemiMonthly, "Default payroll frequency."),
            [PayrollSettingKeys.DefaultWorkingDaysPerMonth] = ("22", "Default working days per month."),
            [PayrollSettingKeys.DefaultWorkingHoursPerDay] = ("8", "Default working hours per day."),
            [PayrollSettingKeys.LateUndertimeDeductionPolicy] = ("minute_based", "Late and undertime deduction policy."),
            [PayrollSettingKeys.AbsenceDeductionPolicy] = ("day_based", "Absence deduction policy."),
            [PayrollSettingKeys.OvertimeCalculationPolicy] = ("preliminary_hourly_rate", "Preliminary overtime calculation policy."),
            [PayrollSettingKeys.RoundingRule] = ("round_2", "Rounding rule for payroll values."),
            [PayrollSettingKeys.PayrollTimeZoneId] = ("Singapore Standard Time", "Payroll business timezone."),
            [PayrollSettingKeys.PayslipVisibilityRule] = ("approved_or_paid", "Payslip visibility rule."),
            [PayrollSettingKeys.AllowNegativeNetPay] = ("false", "Whether negative net pay is allowed."),
            [PayrollSettingKeys.DefaultCurrency] = ("PHP", "Default payroll currency.")
        };

        foreach (var setting in payrollSettings)
        {
            await UpsertPayrollSettingAsync(setting.Key, setting.Value.Value, setting.Value.Description, cancellationToken);
        }

        var payPeriodTemplates = new[]
        {
            new PayPeriodTemplate
            {
                Code = "SEMI-MONTHLY",
                Name = "Semi-monthly Payroll",
                Description = "Baseline semi-monthly payroll period template.",
                PayFrequency = PayrollPayFrequencies.SemiMonthly,
                PeriodLengthDays = 15,
                PayrollOffsetDays = 5
            },
            new PayPeriodTemplate
            {
                Code = "MONTHLY",
                Name = "Monthly Payroll",
                Description = "Baseline monthly payroll period template.",
                PayFrequency = PayrollPayFrequencies.Monthly,
                PeriodLengthDays = 30,
                PayrollOffsetDays = 5
            },
            new PayPeriodTemplate
            {
                Code = "WEEKLY",
                Name = "Weekly Payroll",
                Description = "Baseline weekly payroll period template.",
                PayFrequency = PayrollPayFrequencies.Weekly,
                PeriodLengthDays = 7,
                PayrollOffsetDays = 3
            }
        };

        foreach (var template in payPeriodTemplates)
        {
            await UpsertPayPeriodTemplateAsync(template, cancellationToken);
        }

        var earningTypes = new[]
        {
            new EarningType { Code = "BASIC", Name = "Basic Pay", Description = "Baseline regular pay derived from the compensation profile.", Category = EarningTypeCategories.Basic, Taxable = true, Recurring = false, AffectsThirteenthMonth = true },
            new EarningType { Code = "ALLOW-RICE", Name = "Rice Allowance", Description = "Recurring allowance for rice or meal support.", Category = EarningTypeCategories.Allowance, Taxable = false, Recurring = true, AffectsThirteenthMonth = false },
            new EarningType { Code = "ALLOW-TRANS", Name = "Transport Allowance", Description = "Recurring transport or travel support allowance.", Category = EarningTypeCategories.Allowance, Taxable = false, Recurring = true, AffectsThirteenthMonth = false },
            new EarningType { Code = "OT", Name = "Overtime Pay", Description = "Preliminary overtime pay pulled from attendance data.", Category = EarningTypeCategories.Overtime, Taxable = true, Recurring = false, AffectsThirteenthMonth = false },
            new EarningType { Code = "HOLPAY", Name = "Holiday Pay", Description = "Holiday pay placeholder for future holiday and premium rules.", Category = EarningTypeCategories.HolidayPay, Taxable = true, Recurring = false, AffectsThirteenthMonth = false },
            new EarningType { Code = "BONUS", Name = "Bonus", Description = "One-time or periodic employee bonus.", Category = EarningTypeCategories.Bonus, Taxable = true, Recurring = false, AffectsThirteenthMonth = false },
            new EarningType { Code = "COMM", Name = "Commission", Description = "Commission-based payroll earning.", Category = EarningTypeCategories.Commission, Taxable = true, Recurring = false, AffectsThirteenthMonth = false },
            new EarningType { Code = "REIMB", Name = "Reimbursement", Description = "Non-taxable employee reimbursement.", Category = EarningTypeCategories.Reimbursement, Taxable = false, Recurring = false, AffectsThirteenthMonth = false },
            new EarningType { Code = "OTHER-EARN", Name = "Other Earning", Description = "Fallback manual payroll earning type.", Category = EarningTypeCategories.Other, Taxable = true, Recurring = false, AffectsThirteenthMonth = false }
        };

        foreach (var earningType in earningTypes)
        {
            await UpsertEarningTypeAsync(earningType, cancellationToken);
        }

        var deductionTypes = new[]
        {
            new DeductionType { Code = "SSS", Name = "SSS", Description = "Configurable government deduction line for SSS.", Category = DeductionTypeCategories.Government, PreTax = false, Recurring = false },
            new DeductionType { Code = "PHILHEALTH", Name = "PhilHealth", Description = "Configurable government deduction line for PhilHealth.", Category = DeductionTypeCategories.Government, PreTax = false, Recurring = false },
            new DeductionType { Code = "PAGIBIG", Name = "Pag-IBIG", Description = "Configurable government deduction line for Pag-IBIG.", Category = DeductionTypeCategories.Government, PreTax = false, Recurring = false },
            new DeductionType { Code = "WTAX", Name = "Withholding Tax", Description = "Configurable tax deduction line.", Category = DeductionTypeCategories.Tax, PreTax = false, Recurring = false },
            new DeductionType { Code = "ABSENCE", Name = "Absence Deduction", Description = "Payroll deduction for unpaid absences.", Category = DeductionTypeCategories.Absence, PreTax = false, Recurring = false },
            new DeductionType { Code = "LATE", Name = "Late Deduction", Description = "Payroll deduction for late minutes.", Category = DeductionTypeCategories.Late, PreTax = false, Recurring = false },
            new DeductionType { Code = "UNDERTIME", Name = "Undertime Deduction", Description = "Payroll deduction for undertime minutes.", Category = DeductionTypeCategories.Undertime, PreTax = false, Recurring = false },
            new DeductionType { Code = "LOAN", Name = "Loan Deduction", Description = "Recurring loan repayment deduction.", Category = DeductionTypeCategories.Loan, PreTax = false, Recurring = true },
            new DeductionType { Code = "CASH-ADV", Name = "Cash Advance", Description = "Recurring cash advance deduction.", Category = DeductionTypeCategories.CashAdvance, PreTax = false, Recurring = true },
            new DeductionType { Code = "OTHER-DED", Name = "Other Deduction", Description = "Fallback manual payroll deduction type.", Category = DeductionTypeCategories.Other, PreTax = false, Recurring = false }
        };

        foreach (var deductionType in deductionTypes)
        {
            await UpsertDeductionTypeAsync(deductionType, cancellationToken);
        }

        var contributionTypes = new[]
        {
            new ContributionType { Code = "SSS", Name = "SSS", Description = "Configurable Social Security System contribution type.", EmployeeShareApplicable = true, EmployerShareApplicable = true },
            new ContributionType { Code = "PHILHEALTH", Name = "PhilHealth", Description = "Configurable PhilHealth contribution type.", EmployeeShareApplicable = true, EmployerShareApplicable = true },
            new ContributionType { Code = "PAGIBIG", Name = "Pag-IBIG", Description = "Configurable Pag-IBIG contribution type.", EmployeeShareApplicable = true, EmployerShareApplicable = true }
        };

        foreach (var contributionType in contributionTypes)
        {
            await UpsertContributionTypeAsync(contributionType, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var contributionTypeIds = await _dbContext.ContributionTypes
            .AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record.Id, cancellationToken);

        var contributionTables = new[]
        {
            new GovernmentContributionTable
            {
                ContributionTypeId = contributionTypeIds["SSS"],
                Name = "SSS Baseline Configuration",
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            },
            new GovernmentContributionTable
            {
                ContributionTypeId = contributionTypeIds["PHILHEALTH"],
                Name = "PhilHealth Baseline Configuration",
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            },
            new GovernmentContributionTable
            {
                ContributionTypeId = contributionTypeIds["PAGIBIG"],
                Name = "Pag-IBIG Baseline Configuration",
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            }
        };

        foreach (var contributionTable in contributionTables)
        {
            await UpsertGovernmentContributionTableAsync(contributionTable, cancellationToken);
        }

        var taxTables = new[]
        {
            new TaxTable
            {
                Code = "WTAX-WEEKLY",
                Name = "Weekly Withholding Tax Table",
                PayFrequency = PayrollPayFrequencies.Weekly,
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            },
            new TaxTable
            {
                Code = "WTAX-SEMI",
                Name = "Semi-monthly Withholding Tax Table",
                PayFrequency = PayrollPayFrequencies.SemiMonthly,
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            },
            new TaxTable
            {
                Code = "WTAX-MONTHLY",
                Name = "Monthly Withholding Tax Table",
                PayFrequency = PayrollPayFrequencies.Monthly,
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            },
            new TaxTable
            {
                Code = "WTAX-CUSTOM",
                Name = "Custom Withholding Tax Table",
                PayFrequency = PayrollPayFrequencies.Custom,
                EffectiveStartDate = new DateOnly(2020, 1, 1)
            }
        };

        foreach (var taxTable in taxTables)
        {
            await UpsertTaxTableAsync(taxTable, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertDepartmentAsync(Department seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Departments.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.Departments.Add(new Department
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertPositionAsync(Position seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Positions.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.Positions.Add(new Position
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                DepartmentId = seed.DepartmentId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.DepartmentId = seed.DepartmentId;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertBranchAsync(Branch seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Branches.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.Branches.Add(new Branch
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                Address = seed.Address,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.Address = seed.Address;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertEmploymentTypeAsync(EmploymentType seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EmploymentTypes.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.EmploymentTypes.Add(new EmploymentType
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertEmploymentStatusAsync(EmploymentStatus seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EmploymentStatuses.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.EmploymentStatuses.Add(new EmploymentStatus
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertDocumentTypeAsync(DocumentType seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DocumentTypes.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.DocumentTypes.Add(new DocumentType
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                RequiresExpiryDate = seed.RequiresExpiryDate,
                IsRequired = seed.IsRequired,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.RequiresExpiryDate = seed.RequiresExpiryDate;
        existing.IsRequired = seed.IsRequired;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertWorkScheduleAsync(WorkSchedule seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.WorkSchedules.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.WorkSchedules.Add(new WorkSchedule
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                ScheduleType = seed.ScheduleType,
                RequiredWorkingMinutes = seed.RequiredWorkingMinutes,
                GracePeriodMinutes = seed.GracePeriodMinutes,
                BreakDurationMinutes = seed.BreakDurationMinutes,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.ScheduleType = seed.ScheduleType;
        existing.RequiredWorkingMinutes = seed.RequiredWorkingMinutes;
        existing.GracePeriodMinutes = seed.GracePeriodMinutes;
        existing.BreakDurationMinutes = seed.BreakDurationMinutes;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertShiftAsync(Shift seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Shifts.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.Shifts.Add(new Shift
            {
                Code = seed.Code,
                Name = seed.Name,
                StartTime = seed.StartTime,
                EndTime = seed.EndTime,
                BreakStartTime = seed.BreakStartTime,
                BreakEndTime = seed.BreakEndTime,
                RequiredWorkingMinutes = seed.RequiredWorkingMinutes,
                GracePeriodMinutes = seed.GracePeriodMinutes,
                IsOvernight = seed.IsOvernight,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.StartTime = seed.StartTime;
        existing.EndTime = seed.EndTime;
        existing.BreakStartTime = seed.BreakStartTime;
        existing.BreakEndTime = seed.BreakEndTime;
        existing.RequiredWorkingMinutes = seed.RequiredWorkingMinutes;
        existing.GracePeriodMinutes = seed.GracePeriodMinutes;
        existing.IsOvernight = seed.IsOvernight;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertLeaveTypeAsync(LeaveType seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.LeaveTypes.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.LeaveTypes.Add(new LeaveType
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                IsPaid = seed.IsPaid,
                RequiresAttachment = seed.RequiresAttachment,
                RequiresReason = seed.RequiresReason,
                AllowHalfDay = seed.AllowHalfDay,
                AllowNegativeBalance = seed.AllowNegativeBalance,
                DefaultAnnualCredits = seed.DefaultAnnualCredits,
                MaxDaysPerRequest = seed.MaxDaysPerRequest,
                MinDaysBeforeFiling = seed.MinDaysBeforeFiling,
                GenderRestriction = seed.GenderRestriction,
                EmploymentTypeRestrictions = seed.EmploymentTypeRestrictions,
                CountsRestDays = seed.CountsRestDays,
                CountsHolidays = seed.CountsHolidays,
                AllowDuringProbationaryPeriod = seed.AllowDuringProbationaryPeriod,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.IsPaid = seed.IsPaid;
        existing.RequiresAttachment = seed.RequiresAttachment;
        existing.RequiresReason = seed.RequiresReason;
        existing.AllowHalfDay = seed.AllowHalfDay;
        existing.AllowNegativeBalance = seed.AllowNegativeBalance;
        existing.DefaultAnnualCredits = seed.DefaultAnnualCredits;
        existing.MaxDaysPerRequest = seed.MaxDaysPerRequest;
        existing.MinDaysBeforeFiling = seed.MinDaysBeforeFiling;
        existing.GenderRestriction = seed.GenderRestriction;
        existing.EmploymentTypeRestrictions = seed.EmploymentTypeRestrictions;
        existing.CountsRestDays = seed.CountsRestDays;
        existing.CountsHolidays = seed.CountsHolidays;
        existing.AllowDuringProbationaryPeriod = seed.AllowDuringProbationaryPeriod;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertPayPeriodTemplateAsync(PayPeriodTemplate seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PayPeriodTemplates.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.PayPeriodTemplates.Add(new PayPeriodTemplate
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                PayFrequency = seed.PayFrequency,
                PeriodLengthDays = seed.PeriodLengthDays,
                PayrollOffsetDays = seed.PayrollOffsetDays,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.PayFrequency = seed.PayFrequency;
        existing.PeriodLengthDays = seed.PeriodLengthDays;
        existing.PayrollOffsetDays = seed.PayrollOffsetDays;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertEarningTypeAsync(EarningType seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EarningTypes.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.EarningTypes.Add(new EarningType
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                Category = seed.Category,
                Taxable = seed.Taxable,
                Recurring = seed.Recurring,
                AffectsThirteenthMonth = seed.AffectsThirteenthMonth,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.Category = seed.Category;
        existing.Taxable = seed.Taxable;
        existing.Recurring = seed.Recurring;
        existing.AffectsThirteenthMonth = seed.AffectsThirteenthMonth;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertDeductionTypeAsync(DeductionType seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DeductionTypes.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.DeductionTypes.Add(new DeductionType
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                Category = seed.Category,
                PreTax = seed.PreTax,
                Recurring = seed.Recurring,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.Category = seed.Category;
        existing.PreTax = seed.PreTax;
        existing.Recurring = seed.Recurring;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertContributionTypeAsync(ContributionType seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ContributionTypes.SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);
        if (existing is null)
        {
            _dbContext.ContributionTypes.Add(new ContributionType
            {
                Code = seed.Code,
                Name = seed.Name,
                Description = seed.Description,
                EmployeeShareApplicable = seed.EmployeeShareApplicable,
                EmployerShareApplicable = seed.EmployerShareApplicable,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.EmployeeShareApplicable = seed.EmployeeShareApplicable;
        existing.EmployerShareApplicable = seed.EmployerShareApplicable;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertPayrollSettingAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PayrollSettings.SingleOrDefaultAsync(record => record.Key == key, cancellationToken);
        if (existing is null)
        {
            _dbContext.PayrollSettings.Add(new PayrollSetting
            {
                Key = key,
                Value = value,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Value = value;
        existing.Description = description;
        existing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task UpsertGovernmentContributionTableAsync(GovernmentContributionTable seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.GovernmentContributionTables
            .Include(record => record.Brackets)
            .SingleOrDefaultAsync(
                record => record.ContributionTypeId == seed.ContributionTypeId && record.Name == seed.Name,
                cancellationToken);

        if (existing is null)
        {
            existing = new GovernmentContributionTable
            {
                ContributionTypeId = seed.ContributionTypeId,
                Name = seed.Name,
                EffectiveStartDate = seed.EffectiveStartDate,
                EffectiveEndDate = seed.EffectiveEndDate,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            existing.Brackets.Add(new GovernmentContributionBracket
            {
                MinCompensation = 0m,
                MaxCompensation = null,
                EmployeeShareAmount = 0m,
                EmployeeShareRate = 0m,
                EmployerShareAmount = 0m,
                EmployerShareRate = 0m,
                Remarks = "Baseline placeholder bracket. Update with actual rates before live payroll processing.",
                CreatedAtUtc = DateTime.UtcNow
            });

            _dbContext.GovernmentContributionTables.Add(existing);
            return;
        }

        existing.EffectiveStartDate = seed.EffectiveStartDate;
        existing.EffectiveEndDate = seed.EffectiveEndDate;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        if (existing.Brackets.Count == 0)
        {
            existing.Brackets.Add(new GovernmentContributionBracket
            {
                MinCompensation = 0m,
                MaxCompensation = null,
                EmployeeShareAmount = 0m,
                EmployeeShareRate = 0m,
                EmployerShareAmount = 0m,
                EmployerShareRate = 0m,
                Remarks = "Baseline placeholder bracket. Update with actual rates before live payroll processing.",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task UpsertTaxTableAsync(TaxTable seed, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.TaxTables
            .Include(record => record.Brackets)
            .SingleOrDefaultAsync(record => record.Code == seed.Code, cancellationToken);

        if (existing is null)
        {
            existing = new TaxTable
            {
                Code = seed.Code,
                Name = seed.Name,
                PayFrequency = seed.PayFrequency,
                EffectiveStartDate = seed.EffectiveStartDate,
                EffectiveEndDate = seed.EffectiveEndDate,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            existing.Brackets.Add(new TaxBracket
            {
                MinTaxableIncome = 0m,
                MaxTaxableIncome = null,
                BaseTax = 0m,
                TaxRate = 0m,
                ExcessOver = 0m,
                CreatedAtUtc = DateTime.UtcNow
            });

            _dbContext.TaxTables.Add(existing);
            return;
        }

        existing.Name = seed.Name;
        existing.PayFrequency = seed.PayFrequency;
        existing.EffectiveStartDate = seed.EffectiveStartDate;
        existing.EffectiveEndDate = seed.EffectiveEndDate;
        existing.IsActive = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        if (existing.Brackets.Count == 0)
        {
            existing.Brackets.Add(new TaxBracket
            {
                MinTaxableIncome = 0m,
                MaxTaxableIncome = null,
                BaseTax = 0m,
                TaxRate = 0m,
                ExcessOver = 0m,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task EnsureProvidentFundSeedAsync(CancellationToken cancellationToken)
    {
        var policy = await _dbContext.ProvidentFundPolicies
            .SingleOrDefaultAsync(record => record.PolicyName == "Regular Employee Provident Fund", cancellationToken);

        if (policy is null)
        {
            policy = new ProvidentFundPolicy
            {
                PolicyName = "Regular Employee Provident Fund",
                Description = "Default active provident fund policy for regular employees.",
                EligibilityRules = "Regular employees subject to HR, Finance, Legal, and local labor compliance validation before production use.",
                EmployeeContributionType = ProvidentFundContributionTypes.Percentage,
                EmployeeContributionValue = 5m,
                EmployerContributionType = ProvidentFundContributionTypes.Percentage,
                EmployerContributionValue = 5m,
                ContributionFrequency = "monthly",
                EffectiveDate = new DateOnly(DateTime.UtcNow.Year, 1, 1),
                Status = ProvidentFundPolicyStatuses.Active,
                AllowVoluntaryContribution = true,
                AllowWithdrawal = true,
                AllowLoan = false,
                Remarks = "Seed policy for implementation testing. Confirm actual legal/tax/compliance rules before production use.",
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.ProvidentFundPolicies.Add(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            policy.Description = "Default active provident fund policy for regular employees.";
            policy.EligibilityRules = "Regular employees subject to HR, Finance, Legal, and local labor compliance validation before production use.";
            policy.EmployeeContributionType = ProvidentFundContributionTypes.Percentage;
            policy.EmployeeContributionValue = 5m;
            policy.EmployerContributionType = ProvidentFundContributionTypes.Percentage;
            policy.EmployerContributionValue = 5m;
            policy.ContributionFrequency = "monthly";
            policy.Status = ProvidentFundPolicyStatuses.Active;
            policy.AllowVoluntaryContribution = true;
            policy.AllowWithdrawal = true;
            policy.AllowLoan = false;
            policy.Remarks = "Seed policy for implementation testing. Confirm actual legal/tax/compliance rules before production use.";
            policy.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var rules = new[]
        {
            new { Years = 0, Percentage = 0m },
            new { Years = 1, Percentage = 20m },
            new { Years = 2, Percentage = 40m },
            new { Years = 3, Percentage = 60m },
            new { Years = 4, Percentage = 80m },
            new { Years = 5, Percentage = 100m }
        };

        var existingRules = await _dbContext.ProvidentFundVestingRules
            .Where(record => record.PolicyId == policy.Id)
            .ToDictionaryAsync(record => record.YearsOfService, cancellationToken);

        foreach (var seed in rules)
        {
            if (!existingRules.TryGetValue(seed.Years, out var existingRule))
            {
                _dbContext.ProvidentFundVestingRules.Add(new ProvidentFundVestingRule
                {
                    PolicyId = policy.Id,
                    YearsOfService = seed.Years,
                    VestedPercentage = seed.Percentage,
                    Remarks = seed.Years == 5 ? "Five years and above." : $"{seed.Years} year threshold.",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existingRule.VestedPercentage = seed.Percentage;
                existingRule.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ThrowIfIdentityFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        throw new Exceptions.BadRequestException(message, errors);
    }
}
