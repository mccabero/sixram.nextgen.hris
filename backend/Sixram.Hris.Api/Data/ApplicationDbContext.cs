using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Entities;

namespace Sixram.Api.Data;

public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    string,
    IdentityUserClaim<string>,
    ApplicationUserRole,
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private bool _isWritingAuditLogs;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<EmploymentType> EmploymentTypes => Set<EmploymentType>();

    public DbSet<EmploymentStatus> EmploymentStatuses => Set<EmploymentStatus>();

    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<EmployeeScheduleAssignment> EmployeeScheduleAssignments => Set<EmployeeScheduleAssignment>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<AttendanceAdjustmentRequest> AttendanceAdjustmentRequests => Set<AttendanceAdjustmentRequest>();

    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();

    public DbSet<LeaveBalanceTransaction> LeaveBalanceTransactions => Set<LeaveBalanceTransaction>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<CompensationProfile> CompensationProfiles => Set<CompensationProfile>();

    public DbSet<PayPeriodTemplate> PayPeriodTemplates => Set<PayPeriodTemplate>();

    public DbSet<EarningType> EarningTypes => Set<EarningType>();

    public DbSet<DeductionType> DeductionTypes => Set<DeductionType>();

    public DbSet<ContributionType> ContributionTypes => Set<ContributionType>();

    public DbSet<PayrollSetting> PayrollSettings => Set<PayrollSetting>();

    public DbSet<PayPeriod> PayPeriods => Set<PayPeriod>();

    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();

    public DbSet<PayrollRunItem> PayrollRunItems => Set<PayrollRunItem>();

    public DbSet<PayrollEarningLine> PayrollEarningLines => Set<PayrollEarningLine>();

    public DbSet<PayrollDeductionLine> PayrollDeductionLines => Set<PayrollDeductionLine>();

    public DbSet<GovernmentContributionTable> GovernmentContributionTables => Set<GovernmentContributionTable>();

    public DbSet<GovernmentContributionBracket> GovernmentContributionBrackets => Set<GovernmentContributionBracket>();

    public DbSet<TaxTable> TaxTables => Set<TaxTable>();

    public DbSet<TaxBracket> TaxBrackets => Set<TaxBracket>();

    public DbSet<EmployeeRecurringEarning> EmployeeRecurringEarnings => Set<EmployeeRecurringEarning>();

    public DbSet<EmployeeRecurringDeduction> EmployeeRecurringDeductions => Set<EmployeeRecurringDeduction>();

    public DbSet<PayrollAdjustment> PayrollAdjustments => Set<PayrollAdjustment>();

    public DbSet<PayrollAuditLog> PayrollAuditLogs => Set<PayrollAuditLog>();

    public DbSet<EmployeeProfileChangeRequest> EmployeeProfileChangeRequests => Set<EmployeeProfileChangeRequest>();

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SavedReport> SavedReports => Set<SavedReport>();

    public DbSet<ProvidentFundPolicy> ProvidentFundPolicies => Set<ProvidentFundPolicy>();

    public DbSet<ProvidentFundVestingRule> ProvidentFundVestingRules => Set<ProvidentFundVestingRule>();

    public DbSet<ProvidentFundEnrollment> ProvidentFundEnrollments => Set<ProvidentFundEnrollment>();

    public DbSet<ProvidentFundContributionBatch> ProvidentFundContributionBatches => Set<ProvidentFundContributionBatch>();

    public DbSet<ProvidentFundContributionBatchLine> ProvidentFundContributionBatchLines => Set<ProvidentFundContributionBatchLine>();

    public DbSet<ProvidentFundLedgerTransaction> ProvidentFundLedgerTransactions => Set<ProvidentFundLedgerTransaction>();

    public DbSet<ProvidentFundWithdrawalRequest> ProvidentFundWithdrawalRequests => Set<ProvidentFundWithdrawalRequest>();

    public DbSet<ProvidentFundWithdrawalApproval> ProvidentFundWithdrawalApprovals => Set<ProvidentFundWithdrawalApproval>();

    public DbSet<ProvidentFundAdjustment> ProvidentFundAdjustments => Set<ProvidentFundAdjustment>();

    public DbSet<ProvidentFundAdjustmentApproval> ProvidentFundAdjustmentApprovals => Set<ProvidentFundAdjustmentApproval>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (_isWritingAuditLogs)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var pendingAuditLogs = BuildAuditLogs();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (pendingAuditLogs.Count == 0)
        {
            return result;
        }

        foreach (var auditLog in pendingAuditLogs)
        {
            if (string.IsNullOrWhiteSpace(auditLog.EntityId))
            {
                auditLog.EntityId = ResolveDeferredEntityId(auditLog.EntityType, auditLog.NewValuesJson, auditLog.OldValuesJson);
            }

            if (auditLog.EmployeeId is null &&
                !(auditLog.Action == "delete" && auditLog.EntityType == AuditEntityTypes.Employee))
            {
                auditLog.EmployeeId = ResolveDeferredEmployeeId(auditLog.NewValuesJson, auditLog.OldValuesJson);
            }
        }

        _isWritingAuditLogs = true;

        try
        {
            AuditLogs.AddRange(pendingAuditLogs);
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        finally
        {
            _isWritingAuditLogs = false;
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(256);
            entity.Property(user => user.CreatedAtUtc).HasPrecision(0);
            entity.Property(user => user.UpdatedAtUtc).HasPrecision(0);
            entity.Property(user => user.IsEnabled).HasDefaultValue(true);
            entity.HasIndex(user => user.Email).IsUnique();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(role => role.Description).HasMaxLength(256);
            entity.Property(role => role.CreatedAtUtc).HasPrecision(0);
            entity.Property(role => role.UpdatedAtUtc).HasPrecision(0);
        });

        builder.Entity<ApplicationUserRole>(entity =>
        {
            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .IsRequired();

            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .IsRequired();
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(token => token.CreatedAtUtc).HasPrecision(0);
            entity.Property(token => token.ExpiresAtUtc).HasPrecision(0);
            entity.Property(token => token.RevokedAtUtc).HasPrecision(0);
            entity.Property(token => token.CreatedByIp).HasMaxLength(64);
            entity.Property(token => token.RevokedByIp).HasMaxLength(64);
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureDepartment(builder.Entity<Department>());
        ConfigurePosition(builder.Entity<Position>());
        ConfigureBranch(builder.Entity<Branch>());
        ConfigureEmploymentType(builder.Entity<EmploymentType>());
        ConfigureEmploymentStatus(builder.Entity<EmploymentStatus>());
        ConfigureDocumentType(builder.Entity<DocumentType>());
        ConfigureWorkSchedule(builder.Entity<WorkSchedule>());
        ConfigureShift(builder.Entity<Shift>());
        ConfigureEmployee(builder.Entity<Employee>());
        ConfigureEmployeeScheduleAssignment(builder.Entity<EmployeeScheduleAssignment>());
        ConfigureEmployeeDocument(builder.Entity<EmployeeDocument>());
        ConfigureAttendanceRecord(builder.Entity<AttendanceRecord>());
        ConfigureAttendanceAdjustmentRequest(builder.Entity<AttendanceAdjustmentRequest>());
        ConfigureLeaveType(builder.Entity<LeaveType>());
        ConfigureEmployeeLeaveBalance(builder.Entity<EmployeeLeaveBalance>());
        ConfigureLeaveBalanceTransaction(builder.Entity<LeaveBalanceTransaction>());
        ConfigureLeaveRequest(builder.Entity<LeaveRequest>());
        ConfigureEmployeeProfileChangeRequest(builder.Entity<EmployeeProfileChangeRequest>());
        ConfigureCompensationProfile(builder.Entity<CompensationProfile>());
        ConfigurePayPeriodTemplate(builder.Entity<PayPeriodTemplate>());
        ConfigureEarningType(builder.Entity<EarningType>());
        ConfigureDeductionType(builder.Entity<DeductionType>());
        ConfigureContributionType(builder.Entity<ContributionType>());
        ConfigurePayrollSetting(builder.Entity<PayrollSetting>());
        ConfigurePayPeriod(builder.Entity<PayPeriod>());
        ConfigurePayrollRun(builder.Entity<PayrollRun>());
        ConfigurePayrollRunItem(builder.Entity<PayrollRunItem>());
        ConfigurePayrollEarningLine(builder.Entity<PayrollEarningLine>());
        ConfigurePayrollDeductionLine(builder.Entity<PayrollDeductionLine>());
        ConfigureGovernmentContributionTable(builder.Entity<GovernmentContributionTable>());
        ConfigureGovernmentContributionBracket(builder.Entity<GovernmentContributionBracket>());
        ConfigureTaxTable(builder.Entity<TaxTable>());
        ConfigureTaxBracket(builder.Entity<TaxBracket>());
        ConfigureEmployeeRecurringEarning(builder.Entity<EmployeeRecurringEarning>());
        ConfigureEmployeeRecurringDeduction(builder.Entity<EmployeeRecurringDeduction>());
        ConfigurePayrollAdjustment(builder.Entity<PayrollAdjustment>());
        ConfigurePayrollAuditLog(builder.Entity<PayrollAuditLog>());
        ConfigureNotificationRecord(builder.Entity<NotificationRecord>());
        ConfigureAuditLog(builder.Entity<AuditLog>());
        ConfigureSavedReport(builder.Entity<SavedReport>());
        ConfigureProvidentFundPolicy(builder.Entity<ProvidentFundPolicy>());
        ConfigureProvidentFundVestingRule(builder.Entity<ProvidentFundVestingRule>());
        ConfigureProvidentFundEnrollment(builder.Entity<ProvidentFundEnrollment>());
        ConfigureProvidentFundContributionBatch(builder.Entity<ProvidentFundContributionBatch>());
        ConfigureProvidentFundContributionBatchLine(builder.Entity<ProvidentFundContributionBatchLine>());
        ConfigureProvidentFundLedgerTransaction(builder.Entity<ProvidentFundLedgerTransaction>());
        ConfigureProvidentFundWithdrawalRequest(builder.Entity<ProvidentFundWithdrawalRequest>());
        ConfigureProvidentFundWithdrawalApproval(builder.Entity<ProvidentFundWithdrawalApproval>());
        ConfigureProvidentFundAdjustment(builder.Entity<ProvidentFundAdjustment>());
        ConfigureProvidentFundAdjustmentApproval(builder.Entity<ProvidentFundAdjustmentApproval>());
    }

    private static void ConfigureDepartment(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Department> entity)
    {
        entity.ToTable("Departments");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigurePosition(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Position> entity)
    {
        entity.ToTable("Positions");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
        entity.HasIndex(record => record.DepartmentId);
        entity.HasOne(record => record.Department)
            .WithMany(department => department.Positions)
            .HasForeignKey(record => record.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBranch(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Branch> entity)
    {
        entity.ToTable("Branches");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.Address).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureEmploymentType(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmploymentType> entity)
    {
        entity.ToTable("EmploymentTypes");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureEmploymentStatus(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmploymentStatus> entity)
    {
        entity.ToTable("EmploymentStatuses");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureDocumentType(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<DocumentType> entity)
    {
        entity.ToTable("DocumentTypes");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureWorkSchedule(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<WorkSchedule> entity)
    {
        entity.ToTable("WorkSchedules");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.ScheduleType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureShift(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Shift> entity)
    {
        entity.ToTable("Shifts");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureEmployee(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Employee> entity)
    {
        entity.ToTable("Employees");
        entity.HasKey(record => record.Id);

        entity.Property(record => record.EmployeeCode).HasMaxLength(32).IsRequired();
        entity.Property(record => record.FirstName).HasMaxLength(128).IsRequired();
        entity.Property(record => record.MiddleName).HasMaxLength(128);
        entity.Property(record => record.LastName).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Suffix).HasMaxLength(32);
        entity.Property(record => record.Gender).HasMaxLength(32).IsRequired();
        entity.Property(record => record.BirthDate).HasColumnType("date");
        entity.Property(record => record.CivilStatus).HasMaxLength(32);
        entity.Property(record => record.Nationality).HasMaxLength(64);
        entity.Property(record => record.MobileNumber).HasMaxLength(32);
        entity.Property(record => record.Email).HasMaxLength(256);
        entity.Property(record => record.Address).HasMaxLength(512);
        entity.Property(record => record.CityProvince).HasMaxLength(128);
        entity.Property(record => record.PostalCode).HasMaxLength(32);
        entity.Property(record => record.EmergencyContactName).HasMaxLength(128);
        entity.Property(record => record.EmergencyContactRelationship).HasMaxLength(64);
        entity.Property(record => record.EmergencyContactPhone).HasMaxLength(32);
        entity.Property(record => record.WorkSchedule).HasMaxLength(128);
        entity.Property(record => record.DateHired).HasColumnType("date");
        entity.Property(record => record.DateRegularized).HasColumnType("date");
        entity.Property(record => record.DateSeparated).HasColumnType("date");
        entity.Property(record => record.SssNumber).HasMaxLength(32);
        entity.Property(record => record.PhilHealthNumber).HasMaxLength(32);
        entity.Property(record => record.PagIbigNumber).HasMaxLength(32);
        entity.Property(record => record.TinNumber).HasMaxLength(32);
        entity.Property(record => record.OtherGovernmentId).HasMaxLength(64);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);

        entity.HasIndex(record => record.EmployeeCode).IsUnique();
        entity.HasIndex(record => new { record.LastName, record.FirstName });
        entity.HasIndex(record => record.DepartmentId);
        entity.HasIndex(record => record.PositionId);
        entity.HasIndex(record => record.BranchId);
        entity.HasIndex(record => record.EmploymentTypeId);
        entity.HasIndex(record => record.EmploymentStatusId);
        entity.HasIndex(record => record.ManagerId);
        entity.HasIndex(record => record.IsActive);
        entity.HasIndex(record => record.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");

        entity.HasOne(record => record.Department)
            .WithMany(department => department.Employees)
            .HasForeignKey(record => record.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Position)
            .WithMany(position => position.Employees)
            .HasForeignKey(record => record.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Branch)
            .WithMany(branch => branch.Employees)
            .HasForeignKey(record => record.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.EmploymentType)
            .WithMany(type => type.Employees)
            .HasForeignKey(record => record.EmploymentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.EmploymentStatus)
            .WithMany(status => status.Employees)
            .HasForeignKey(record => record.EmploymentStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Manager)
            .WithMany(manager => manager.DirectReports)
            .HasForeignKey(record => record.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.User)
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureEmployeeScheduleAssignment(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmployeeScheduleAssignment> entity)
    {
        entity.ToTable("EmployeeScheduleAssignments");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.RestDays).HasMaxLength(64);
        entity.Property(record => record.EffectiveStartDate).HasColumnType("date");
        entity.Property(record => record.EffectiveEndDate).HasColumnType("date");
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.WorkScheduleId);
        entity.HasIndex(record => record.ShiftId);
        entity.HasIndex(record => new { record.EmployeeId, record.EffectiveStartDate, record.EffectiveEndDate });
        entity.HasIndex(record => record.IsActive);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.ScheduleAssignments)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.WorkSchedule)
            .WithMany(schedule => schedule.EmployeeScheduleAssignments)
            .HasForeignKey(record => record.WorkScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Shift)
            .WithMany(shift => shift.EmployeeScheduleAssignments)
            .HasForeignKey(record => record.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEmployeeDocument(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmployeeDocument> entity)
    {
        entity.ToTable("EmployeeDocuments");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Title).HasMaxLength(160).IsRequired();
        entity.Property(record => record.OriginalFileName).HasMaxLength(260).IsRequired();
        entity.Property(record => record.FilePath).HasMaxLength(512).IsRequired();
        entity.Property(record => record.FileSize).IsRequired();
        entity.Property(record => record.MimeType).HasMaxLength(128).IsRequired();
        entity.Property(record => record.IssueDate).HasColumnType("date");
        entity.Property(record => record.ExpiryDate).HasColumnType("date");
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.IsArchived).HasDefaultValue(false);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => new { record.EmployeeId, record.DocumentTypeId, record.Title }).IsUnique();
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.DocumentTypeId);
        entity.HasIndex(record => record.ExpiryDate);
        entity.HasIndex(record => record.IsArchived);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.Documents)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.DocumentType)
            .WithMany(type => type.EmployeeDocuments)
            .HasForeignKey(record => record.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.UploadedByUser)
            .WithMany(user => user.UploadedEmployeeDocuments)
            .HasForeignKey(record => record.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureAttendanceRecord(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AttendanceRecord> entity)
    {
        entity.ToTable("AttendanceRecords");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.AttendanceDate).HasColumnType("date");
        entity.Property(record => record.ScheduledStartTime).HasPrecision(0);
        entity.Property(record => record.ScheduledEndTime).HasPrecision(0);
        entity.Property(record => record.ActualTimeIn).HasPrecision(0);
        entity.Property(record => record.ActualTimeOut).HasPrecision(0);
        entity.Property(record => record.BreakStartTime).HasPrecision(0);
        entity.Property(record => record.BreakEndTime).HasPrecision(0);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Source).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => new { record.EmployeeId, record.AttendanceDate }).IsUnique();
        entity.HasIndex(record => record.AttendanceDate);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.Source);
        entity.HasIndex(record => record.LeaveRequestId);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.AttendanceRecords)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany(user => user.CreatedAttendanceRecords)
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.UpdatedByUser)
            .WithMany(user => user.UpdatedAttendanceRecords)
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.LeaveRequest)
            .WithMany(request => request.AttendanceRecords)
            .HasForeignKey(record => record.LeaveRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureAttendanceAdjustmentRequest(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AttendanceAdjustmentRequest> entity)
    {
        entity.ToTable("AttendanceAdjustmentRequests");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.RequestType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.AttendanceDate).HasColumnType("date");
        entity.Property(record => record.RequestedTimeIn).HasPrecision(0);
        entity.Property(record => record.RequestedTimeOut).HasPrecision(0);
        entity.Property(record => record.RequestedRemarks).HasMaxLength(1000);
        entity.Property(record => record.Reason).HasMaxLength(1000).IsRequired();
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.ReviewerRemarks).HasMaxLength(1000);
        entity.Property(record => record.ReviewedAtUtc).HasPrecision(0);
        entity.Property(record => record.AppliedAtUtc).HasPrecision(0);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.AttendanceRecordId);
        entity.HasIndex(record => record.RequestedByUserId);
        entity.HasIndex(record => record.CurrentApproverUserId);
        entity.HasIndex(record => record.ReviewedByUserId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.AttendanceDate);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.AttendanceAdjustmentRequests)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.AttendanceRecord)
            .WithMany(attendanceRecord => attendanceRecord.AdjustmentRequests)
            .HasForeignKey(record => record.AttendanceRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.RequestedByUser)
            .WithMany(user => user.RequestedAttendanceAdjustmentRequests)
            .HasForeignKey(record => record.RequestedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.CurrentApproverUser)
            .WithMany(user => user.CurrentApproverAttendanceAdjustmentRequests)
            .HasForeignKey(record => record.CurrentApproverUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ReviewedByUser)
            .WithMany(user => user.ReviewedAttendanceAdjustmentRequests)
            .HasForeignKey(record => record.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureLeaveType(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LeaveType> entity)
    {
        entity.ToTable("LeaveTypes");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.DefaultAnnualCredits).HasPrecision(9, 2);
        entity.Property(record => record.MaxDaysPerRequest).HasPrecision(9, 2);
        entity.Property(record => record.GenderRestriction).HasMaxLength(32);
        entity.Property(record => record.EmploymentTypeRestrictions).HasMaxLength(1000);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureEmployeeLeaveBalance(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmployeeLeaveBalance> entity)
    {
        entity.ToTable("EmployeeLeaveBalances");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.OpeningBalance).HasPrecision(9, 2);
        entity.Property(record => record.Accrued).HasPrecision(9, 2);
        entity.Property(record => record.Used).HasPrecision(9, 2);
        entity.Property(record => record.Pending).HasPrecision(9, 2);
        entity.Property(record => record.Adjusted).HasPrecision(9, 2);
        entity.Property(record => record.CarriedForward).HasPrecision(9, 2);
        entity.Property(record => record.AvailableBalance).HasPrecision(9, 2);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => new { record.EmployeeId, record.LeaveTypeId, record.PeriodYear }).IsUnique();
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.LeaveTypeId);
        entity.HasIndex(record => record.PeriodYear);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.LeaveBalances)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.LeaveType)
            .WithMany(type => type.EmployeeLeaveBalances)
            .HasForeignKey(record => record.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLeaveBalanceTransaction(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LeaveBalanceTransaction> entity)
    {
        entity.ToTable("LeaveBalanceTransactions");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.TransactionType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Amount).HasPrecision(9, 2);
        entity.Property(record => record.BalanceBefore).HasPrecision(9, 2);
        entity.Property(record => record.BalanceAfter).HasPrecision(9, 2);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.LeaveTypeId);
        entity.HasIndex(record => record.PeriodYear);
        entity.HasIndex(record => record.LeaveRequestId);
        entity.HasIndex(record => record.TransactionType);
        entity.HasIndex(record => record.CreatedAtUtc);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.LeaveBalanceTransactions)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.LeaveType)
            .WithMany(type => type.LeaveBalanceTransactions)
            .HasForeignKey(record => record.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.LeaveRequest)
            .WithMany(request => request.LeaveBalanceTransactions)
            .HasForeignKey(record => record.LeaveRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany(user => user.CreatedLeaveBalanceTransactions)
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureLeaveRequest(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LeaveRequest> entity)
    {
        entity.ToTable("LeaveRequests");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.StartDate).HasColumnType("date");
        entity.Property(record => record.EndDate).HasColumnType("date");
        entity.Property(record => record.StartDayType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EndDayType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.TotalLeaveDays).HasPrecision(9, 2);
        entity.Property(record => record.Reason).HasMaxLength(1000);
        entity.Property(record => record.AttachmentOriginalFileName).HasMaxLength(260);
        entity.Property(record => record.AttachmentPath).HasMaxLength(512);
        entity.Property(record => record.AttachmentMimeType).HasMaxLength(128);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.DecisionRemarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.Property(record => record.SubmittedAtUtc).HasPrecision(0);
        entity.Property(record => record.ApprovedAtUtc).HasPrecision(0);
        entity.Property(record => record.RejectedAtUtc).HasPrecision(0);
        entity.Property(record => record.CancelledAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.LeaveTypeId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => new { record.StartDate, record.EndDate });
        entity.HasIndex(record => record.CurrentApproverUserId);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.LeaveRequests)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.LeaveType)
            .WithMany(type => type.LeaveRequests)
            .HasForeignKey(record => record.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CurrentApproverUser)
            .WithMany(user => user.CurrentApproverLeaveRequests)
            .HasForeignKey(record => record.CurrentApproverUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany(user => user.CreatedLeaveRequests)
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.UpdatedByUser)
            .WithMany(user => user.UpdatedLeaveRequests)
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureEmployeeProfileChangeRequest(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmployeeProfileChangeRequest> entity)
    {
        entity.ToTable("EmployeeProfileChangeRequests");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.RequestType).HasMaxLength(64).IsRequired();
        entity.Property(record => record.FieldChangesJson).HasMaxLength(8000).IsRequired();
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Reason).HasMaxLength(1000);
        entity.Property(record => record.ReviewerRemarks).HasMaxLength(1000);
        entity.Property(record => record.ReviewedAtUtc).HasPrecision(0);
        entity.Property(record => record.AppliedAtUtc).HasPrecision(0);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.RequestedByUserId);
        entity.HasIndex(record => record.ReviewedByUserId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.CreatedAtUtc);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.ProfileChangeRequests)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.RequestedByUser)
            .WithMany(user => user.RequestedProfileChangeRequests)
            .HasForeignKey(record => record.RequestedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ReviewedByUser)
            .WithMany(user => user.ReviewedProfileChangeRequests)
            .HasForeignKey(record => record.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureCompensationProfile(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CompensationProfile> entity)
    {
        entity.ToTable("CompensationProfiles");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.PayType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.PayFrequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.BasicSalary).HasPrecision(18, 2);
        entity.Property(record => record.DailyRate).HasPrecision(18, 4);
        entity.Property(record => record.HourlyRate).HasPrecision(18, 4);
        entity.Property(record => record.Currency).HasMaxLength(8).IsRequired();
        entity.Property(record => record.EffectiveStartDate).HasColumnType("date");
        entity.Property(record => record.EffectiveEndDate).HasColumnType("date");
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.IsActive);
        entity.HasIndex(record => new { record.EmployeeId, record.EffectiveStartDate, record.EffectiveEndDate });

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.CompensationProfiles)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany(user => user.CreatedCompensationProfiles)
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.UpdatedByUser)
            .WithMany(user => user.UpdatedCompensationProfiles)
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigurePayPeriodTemplate(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayPeriodTemplate> entity)
    {
        entity.ToTable("PayPeriodTemplates");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.PayFrequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
        entity.HasIndex(record => record.IsActive);
    }

    private static void ConfigureEarningType(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EarningType> entity)
    {
        entity.ToTable("EarningTypes");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.Category).HasMaxLength(32).IsRequired();
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
        entity.HasIndex(record => record.Category);
    }

    private static void ConfigureDeductionType(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<DeductionType> entity)
    {
        entity.ToTable("DeductionTypes");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.Category).HasMaxLength(32).IsRequired();
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
        entity.HasIndex(record => record.Category);
    }

    private static void ConfigureContributionType(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ContributionType> entity)
    {
        entity.ToTable("ContributionTypes");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigurePayrollSetting(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollSetting> entity)
    {
        entity.ToTable("PayrollSettings");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Key).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Value).HasMaxLength(1000).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(512);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Key).IsUnique();
    }

    private static void ConfigurePayPeriod(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayPeriod> entity)
    {
        entity.ToTable("PayPeriods");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.PayFrequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.PeriodStartDate).HasColumnType("date");
        entity.Property(record => record.PeriodEndDate).HasColumnType("date");
        entity.Property(record => record.PayrollDate).HasColumnType("date");
        entity.Property(record => record.CutoffStartDate).HasColumnType("date");
        entity.Property(record => record.CutoffEndDate).HasColumnType("date");
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.PayrollDate);
        entity.HasIndex(record => new { record.PeriodStartDate, record.PeriodEndDate });

        entity.HasOne(record => record.PayPeriodTemplate)
            .WithMany(template => template.PayPeriods)
            .HasForeignKey(record => record.PayPeriodTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurePayrollRun(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollRun> entity)
    {
        entity.ToTable("PayrollRuns");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.ReferenceNumber).HasMaxLength(64).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.GeneratedAtUtc).HasPrecision(0);
        entity.Property(record => record.ApprovedAtUtc).HasPrecision(0);
        entity.Property(record => record.PaidAtUtc).HasPrecision(0);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.PayPeriodId);
        entity.HasIndex(record => record.ReferenceNumber).IsUnique();
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.GeneratedAtUtc);

        entity.HasOne(record => record.PayPeriod)
            .WithMany(payPeriod => payPeriod.PayrollRuns)
            .HasForeignKey(record => record.PayPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.GeneratedByUser)
            .WithMany(user => user.GeneratedPayrollRuns)
            .HasForeignKey(record => record.GeneratedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ApprovedByUser)
            .WithMany(user => user.ApprovedPayrollRuns)
            .HasForeignKey(record => record.ApprovedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigurePayrollRunItem(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollRunItem> entity)
    {
        entity.ToTable("PayrollRunItems");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.EmployeeCodeSnapshot).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EmployeeNameSnapshot).HasMaxLength(256).IsRequired();
        entity.Property(record => record.DepartmentSnapshot).HasMaxLength(128);
        entity.Property(record => record.PositionSnapshot).HasMaxLength(128);
        entity.Property(record => record.BranchSnapshot).HasMaxLength(128);
        entity.Property(record => record.PayTypeSnapshot).HasMaxLength(32).IsRequired();
        entity.Property(record => record.CurrencySnapshot).HasMaxLength(8).IsRequired();
        entity.Property(record => record.BasicSalarySnapshot).HasPrecision(18, 2);
        entity.Property(record => record.DailyRateSnapshot).HasPrecision(18, 4);
        entity.Property(record => record.HourlyRateSnapshot).HasPrecision(18, 4);
        entity.Property(record => record.RegularWorkedDays).HasPrecision(18, 2);
        entity.Property(record => record.RegularWorkedHours).HasPrecision(18, 2);
        entity.Property(record => record.PaidLeaveDays).HasPrecision(18, 2);
        entity.Property(record => record.UnpaidLeaveDays).HasPrecision(18, 2);
        entity.Property(record => record.AbsentDays).HasPrecision(18, 2);
        entity.Property(record => record.BasicPay).HasPrecision(18, 2);
        entity.Property(record => record.AllowanceTotal).HasPrecision(18, 2);
        entity.Property(record => record.OvertimePay).HasPrecision(18, 2);
        entity.Property(record => record.HolidayPay).HasPrecision(18, 2);
        entity.Property(record => record.LeavePay).HasPrecision(18, 2);
        entity.Property(record => record.BonusTotal).HasPrecision(18, 2);
        entity.Property(record => record.OtherEarningsTotal).HasPrecision(18, 2);
        entity.Property(record => record.GrossPay).HasPrecision(18, 2);
        entity.Property(record => record.GovernmentDeductionsTotal).HasPrecision(18, 2);
        entity.Property(record => record.TaxDeduction).HasPrecision(18, 2);
        entity.Property(record => record.AbsenceDeduction).HasPrecision(18, 2);
        entity.Property(record => record.LateDeduction).HasPrecision(18, 2);
        entity.Property(record => record.UndertimeDeduction).HasPrecision(18, 2);
        entity.Property(record => record.LoanDeduction).HasPrecision(18, 2);
        entity.Property(record => record.OtherDeductionsTotal).HasPrecision(18, 2);
        entity.Property(record => record.TotalDeductions).HasPrecision(18, 2);
        entity.Property(record => record.NetPay).HasPrecision(18, 2);
        entity.Property(record => record.EmployerContributionTotal).HasPrecision(18, 2);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.IssueSummary).HasMaxLength(4000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => new { record.PayrollRunId, record.EmployeeId }).IsUnique();
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.HasCriticalIssues);

        entity.HasOne(record => record.PayrollRun)
            .WithMany(run => run.Items)
            .HasForeignKey(record => record.PayrollRunId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.PayrollRunItems)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CompensationProfile)
            .WithMany(profile => profile.PayrollRunItems)
            .HasForeignKey(record => record.CompensationProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurePayrollEarningLine(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollEarningLine> entity)
    {
        entity.ToTable("PayrollEarningLines");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.EarningTypeCodeSnapshot).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EarningTypeNameSnapshot).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(256).IsRequired();
        entity.Property(record => record.Amount).HasPrecision(18, 2);
        entity.Property(record => record.Quantity).HasPrecision(18, 4);
        entity.Property(record => record.Rate).HasPrecision(18, 4);
        entity.Property(record => record.Source).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.PayrollRunItemId);
        entity.HasIndex(record => record.EarningTypeId);
        entity.HasIndex(record => record.Source);

        entity.HasOne(record => record.PayrollRunItem)
            .WithMany(item => item.EarningLines)
            .HasForeignKey(record => record.PayrollRunItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.EarningType)
            .WithMany(type => type.PayrollEarningLines)
            .HasForeignKey(record => record.EarningTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.PayrollAdjustment)
            .WithMany(adjustment => adjustment.PayrollEarningLines)
            .HasForeignKey(record => record.PayrollAdjustmentId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.EmployeeRecurringEarning)
            .WithMany(item => item.PayrollEarningLines)
            .HasForeignKey(record => record.EmployeeRecurringEarningId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurePayrollDeductionLine(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollDeductionLine> entity)
    {
        entity.ToTable("PayrollDeductionLines");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.DeductionTypeCodeSnapshot).HasMaxLength(32).IsRequired();
        entity.Property(record => record.DeductionTypeNameSnapshot).HasMaxLength(128).IsRequired();
        entity.Property(record => record.DeductionCategorySnapshot).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(256).IsRequired();
        entity.Property(record => record.Amount).HasPrecision(18, 2);
        entity.Property(record => record.Source).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.PayrollRunItemId);
        entity.HasIndex(record => record.DeductionTypeId);
        entity.HasIndex(record => record.Source);

        entity.HasOne(record => record.PayrollRunItem)
            .WithMany(item => item.DeductionLines)
            .HasForeignKey(record => record.PayrollRunItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.DeductionType)
            .WithMany(type => type.PayrollDeductionLines)
            .HasForeignKey(record => record.DeductionTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.PayrollAdjustment)
            .WithMany(adjustment => adjustment.PayrollDeductionLines)
            .HasForeignKey(record => record.PayrollAdjustmentId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.EmployeeRecurringDeduction)
            .WithMany(item => item.PayrollDeductionLines)
            .HasForeignKey(record => record.EmployeeRecurringDeductionId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureGovernmentContributionTable(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<GovernmentContributionTable> entity)
    {
        entity.ToTable("GovernmentContributionTables");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.EffectiveStartDate).HasColumnType("date");
        entity.Property(record => record.EffectiveEndDate).HasColumnType("date");
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.ContributionTypeId);
        entity.HasIndex(record => new { record.ContributionTypeId, record.EffectiveStartDate, record.EffectiveEndDate });

        entity.HasOne(record => record.ContributionType)
            .WithMany(type => type.GovernmentContributionTables)
            .HasForeignKey(record => record.ContributionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureGovernmentContributionBracket(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<GovernmentContributionBracket> entity)
    {
        entity.ToTable("GovernmentContributionBrackets");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.MinCompensation).HasPrecision(18, 2);
        entity.Property(record => record.MaxCompensation).HasPrecision(18, 2);
        entity.Property(record => record.EmployeeShareAmount).HasPrecision(18, 2);
        entity.Property(record => record.EmployeeShareRate).HasPrecision(18, 6);
        entity.Property(record => record.EmployerShareAmount).HasPrecision(18, 2);
        entity.Property(record => record.EmployerShareRate).HasPrecision(18, 6);
        entity.Property(record => record.Remarks).HasMaxLength(512);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.GovernmentContributionTableId);

        entity.HasOne(record => record.GovernmentContributionTable)
            .WithMany(table => table.Brackets)
            .HasForeignKey(record => record.GovernmentContributionTableId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTaxTable(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TaxTable> entity)
    {
        entity.ToTable("TaxTables");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Code).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.PayFrequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EffectiveStartDate).HasColumnType("date");
        entity.Property(record => record.EffectiveEndDate).HasColumnType("date");
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.Code).IsUnique();
        entity.HasIndex(record => new { record.PayFrequency, record.EffectiveStartDate, record.EffectiveEndDate });
    }

    private static void ConfigureTaxBracket(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TaxBracket> entity)
    {
        entity.ToTable("TaxBrackets");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.MinTaxableIncome).HasPrecision(18, 2);
        entity.Property(record => record.MaxTaxableIncome).HasPrecision(18, 2);
        entity.Property(record => record.BaseTax).HasPrecision(18, 2);
        entity.Property(record => record.TaxRate).HasPrecision(18, 6);
        entity.Property(record => record.ExcessOver).HasPrecision(18, 2);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.TaxTableId);

        entity.HasOne(record => record.TaxTable)
            .WithMany(table => table.Brackets)
            .HasForeignKey(record => record.TaxTableId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEmployeeRecurringEarning(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmployeeRecurringEarning> entity)
    {
        entity.ToTable("EmployeeRecurringEarnings");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Amount).HasPrecision(18, 2);
        entity.Property(record => record.Frequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EffectiveStartDate).HasColumnType("date");
        entity.Property(record => record.EffectiveEndDate).HasColumnType("date");
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.EarningTypeId);
        entity.HasIndex(record => record.IsActive);
        entity.HasIndex(record => new { record.EmployeeId, record.EffectiveStartDate, record.EffectiveEndDate });

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.RecurringEarnings)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.EarningType)
            .WithMany(type => type.EmployeeRecurringEarnings)
            .HasForeignKey(record => record.EarningTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEmployeeRecurringDeduction(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EmployeeRecurringDeduction> entity)
    {
        entity.ToTable("EmployeeRecurringDeductions");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Amount).HasPrecision(18, 2);
        entity.Property(record => record.Balance).HasPrecision(18, 2);
        entity.Property(record => record.Frequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EffectiveStartDate).HasColumnType("date");
        entity.Property(record => record.EffectiveEndDate).HasColumnType("date");
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.IsActive).HasDefaultValue(true);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.DeductionTypeId);
        entity.HasIndex(record => record.IsActive);
        entity.HasIndex(record => new { record.EmployeeId, record.EffectiveStartDate, record.EffectiveEndDate });

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.RecurringDeductions)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.DeductionType)
            .WithMany(type => type.EmployeeRecurringDeductions)
            .HasForeignKey(record => record.DeductionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePayrollAdjustment(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollAdjustment> entity)
    {
        entity.ToTable("PayrollAdjustments");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.AdjustmentType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Amount).HasPrecision(18, 2);
        entity.Property(record => record.Reason).HasMaxLength(1000).IsRequired();
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.DecisionRemarks).HasMaxLength(1000);
        entity.Property(record => record.ApprovedAtUtc).HasPrecision(0);
        entity.Property(record => record.AppliedAtUtc).HasPrecision(0);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.PayPeriodId);
        entity.HasIndex(record => record.PayrollRunId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.EarningTypeId);
        entity.HasIndex(record => record.DeductionTypeId);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.PayrollAdjustments)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.PayPeriod)
            .WithMany(payPeriod => payPeriod.PayrollAdjustments)
            .HasForeignKey(record => record.PayPeriodId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.PayrollRun)
            .WithMany(run => run.PayrollAdjustments)
            .HasForeignKey(record => record.PayrollRunId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.EarningType)
            .WithMany(type => type.PayrollAdjustments)
            .HasForeignKey(record => record.EarningTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.DeductionType)
            .WithMany(type => type.PayrollAdjustments)
            .HasForeignKey(record => record.DeductionTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.RequestedByUser)
            .WithMany(user => user.RequestedPayrollAdjustments)
            .HasForeignKey(record => record.RequestedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ApprovedByUser)
            .WithMany(user => user.ApprovedPayrollAdjustments)
            .HasForeignKey(record => record.ApprovedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigurePayrollAuditLog(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PayrollAuditLog> entity)
    {
        entity.ToTable("PayrollAuditLogs");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.EntityType).HasMaxLength(64).IsRequired();
        entity.Property(record => record.EntityId).HasMaxLength(64).IsRequired();
        entity.Property(record => record.Action).HasMaxLength(64).IsRequired();
        entity.Property(record => record.Summary).HasMaxLength(1000).IsRequired();
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.PayrollRunId);
        entity.HasIndex(record => record.PayrollRunItemId);
        entity.HasIndex(record => record.ActorUserId);
        entity.HasIndex(record => record.CreatedAtUtc);

        entity.HasOne(record => record.PayrollRun)
            .WithMany(run => run.AuditLogs)
            .HasForeignKey(record => record.PayrollRunId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.PayrollRunItem)
            .WithMany(item => item.AuditLogs)
            .HasForeignKey(record => record.PayrollRunItemId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(record => record.ActorUser)
            .WithMany(user => user.PayrollAuditLogs)
            .HasForeignKey(record => record.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureNotificationRecord(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<NotificationRecord> entity)
    {
        entity.ToTable("Notifications");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Title).HasMaxLength(160).IsRequired();
        entity.Property(record => record.Message).HasMaxLength(1000).IsRequired();
        entity.Property(record => record.Type).HasMaxLength(64).IsRequired();
        entity.Property(record => record.ReferenceType).HasMaxLength(64);
        entity.Property(record => record.ReferenceId).HasMaxLength(64);
        entity.Property(record => record.ActionUrl).HasMaxLength(256);
        entity.Property(record => record.ReadAtUtc).HasPrecision(0);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.UserId);
        entity.HasIndex(record => record.ReadAtUtc);
        entity.HasIndex(record => record.CreatedAtUtc);
        entity.HasIndex(record => new { record.UserId, record.ReadAtUtc });

        entity.HasOne(record => record.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAuditLog(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("AuditLogs");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.ActorNameSnapshot).HasMaxLength(256);
        entity.Property(record => record.Action).HasMaxLength(128).IsRequired();
        entity.Property(record => record.EntityType).HasMaxLength(128).IsRequired();
        entity.Property(record => record.EntityId).HasMaxLength(128).IsRequired();
        entity.Property(record => record.OldValuesJson).HasMaxLength(4000);
        entity.Property(record => record.NewValuesJson).HasMaxLength(4000);
        entity.Property(record => record.IpAddress).HasMaxLength(64);
        entity.Property(record => record.UserAgent).HasMaxLength(512);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.ActorUserId);
        entity.HasIndex(record => record.EntityType);
        entity.HasIndex(record => record.EntityId);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.Action);
        entity.HasIndex(record => record.CreatedAtUtc);

        entity.HasOne(record => record.ActorUser)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(record => record.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.Employee)
            .WithMany(employee => employee.AuditLogs)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSavedReport(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SavedReport> entity)
    {
        entity.ToTable("SavedReports");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.ReportKey).HasMaxLength(128).IsRequired();
        entity.Property(record => record.Name).HasMaxLength(128).IsRequired();
        entity.Property(record => record.FiltersJson).HasMaxLength(4000).IsRequired();
        entity.Property(record => record.IsDefault).HasDefaultValue(false);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.UserId);
        entity.HasIndex(record => record.ReportKey);
        entity.HasIndex(record => new { record.UserId, record.ReportKey });

        entity.HasOne(record => record.User)
            .WithMany(user => user.SavedReports)
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProvidentFundPolicy(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundPolicy> entity)
    {
        entity.ToTable("ProvidentFundPolicies");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.PolicyName).HasMaxLength(160).IsRequired();
        entity.Property(record => record.Description).HasMaxLength(1000);
        entity.Property(record => record.EligibilityRules).HasMaxLength(2000);
        entity.Property(record => record.EmployeeContributionType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EmployeeContributionValue).HasPrecision(18, 4);
        entity.Property(record => record.EmployerContributionType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EmployerContributionValue).HasPrecision(18, 4);
        entity.Property(record => record.ContributionFrequency).HasMaxLength(32).IsRequired();
        entity.Property(record => record.EffectiveDate).HasColumnType("date");
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.PolicyName).IsUnique();
        entity.HasIndex(record => record.Status);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.UpdatedByUser)
            .WithMany()
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundVestingRule(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundVestingRule> entity)
    {
        entity.ToTable("ProvidentFundVestingRules");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.VestedPercentage).HasPrecision(9, 4);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => new { record.PolicyId, record.YearsOfService }).IsUnique();

        entity.HasOne(record => record.Policy)
            .WithMany(policy => policy.VestingRules)
            .HasForeignKey(record => record.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProvidentFundEnrollment(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundEnrollment> entity)
    {
        entity.ToTable("ProvidentFundEnrollments");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.EnrollmentDate).HasColumnType("date");
        entity.Property(record => record.VestingStartDate).HasColumnType("date");
        entity.Property(record => record.EmployeeContributionOverrideType).HasMaxLength(32);
        entity.Property(record => record.EmployeeContributionOverrideValue).HasPrecision(18, 4);
        entity.Property(record => record.EmployerContributionOverrideType).HasMaxLength(32);
        entity.Property(record => record.EmployerContributionOverrideValue).HasPrecision(18, 4);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.Property(record => record.ClosedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.PolicyId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => new { record.EmployeeId, record.Status })
            .IsUnique()
            .HasFilter("[Status] = 'active'");

        entity.HasOne(record => record.Employee)
            .WithMany()
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Policy)
            .WithMany(policy => policy.Enrollments)
            .HasForeignKey(record => record.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.UpdatedByUser)
            .WithMany()
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundContributionBatch(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundContributionBatch> entity)
    {
        entity.ToTable("ProvidentFundContributionBatches");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.BatchNumber).HasMaxLength(64).IsRequired();
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.PostingDate).HasPrecision(0);
        entity.Property(record => record.ReviewedAtUtc).HasPrecision(0);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.BatchNumber).IsUnique();
        entity.HasIndex(record => new { record.Year, record.Month, record.PolicyId });
        entity.HasIndex(record => record.Status);

        entity.HasOne(record => record.Policy)
            .WithMany(policy => policy.ContributionBatches)
            .HasForeignKey(record => record.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ReviewedByUser)
            .WithMany()
            .HasForeignKey(record => record.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.PostedByUser)
            .WithMany()
            .HasForeignKey(record => record.PostedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundContributionBatchLine(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundContributionBatchLine> entity)
    {
        entity.ToTable("ProvidentFundContributionBatchLines");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.BasicSalary).HasPrecision(18, 2);
        entity.Property(record => record.EmployeeContribution).HasPrecision(18, 2);
        entity.Property(record => record.EmployerContribution).HasPrecision(18, 2);
        entity.Property(record => record.VoluntaryContribution).HasPrecision(18, 2);
        entity.Property(record => record.TotalContribution).HasPrecision(18, 2);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.BatchId);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.EnrollmentId);

        entity.HasOne(record => record.Batch)
            .WithMany(batch => batch.Lines)
            .HasForeignKey(record => record.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(record => record.Employee)
            .WithMany()
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Enrollment)
            .WithMany(enrollment => enrollment.ContributionBatchLines)
            .HasForeignKey(record => record.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProvidentFundLedgerTransaction(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundLedgerTransaction> entity)
    {
        entity.ToTable("ProvidentFundLedgerTransactions");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.TransactionNumber).HasMaxLength(64).IsRequired();
        entity.Property(record => record.TransactionDate).HasColumnType("date");
        entity.Property(record => record.TransactionType).HasMaxLength(64).IsRequired();
        entity.Property(record => record.SourceType).HasMaxLength(64).IsRequired();
        entity.Property(record => record.SourceReferenceId).HasMaxLength(128).IsRequired();
        entity.Property(record => record.EmployeeShareAmount).HasPrecision(18, 2);
        entity.Property(record => record.EmployerShareAmount).HasPrecision(18, 2);
        entity.Property(record => record.VoluntaryShareAmount).HasPrecision(18, 2);
        entity.Property(record => record.InterestAmount).HasPrecision(18, 2);
        entity.Property(record => record.DebitAmount).HasPrecision(18, 2);
        entity.Property(record => record.CreditAmount).HasPrecision(18, 2);
        entity.Property(record => record.RunningBalance).HasPrecision(18, 2);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.TransactionNumber).IsUnique();
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.EnrollmentId);
        entity.HasIndex(record => record.PolicyId);
        entity.HasIndex(record => record.TransactionDate);
        entity.HasIndex(record => record.TransactionType);
        entity.HasIndex(record => new { record.SourceType, record.SourceReferenceId });

        entity.HasOne(record => record.Employee)
            .WithMany()
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Enrollment)
            .WithMany(enrollment => enrollment.LedgerTransactions)
            .HasForeignKey(record => record.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Policy)
            .WithMany(policy => policy.LedgerTransactions)
            .HasForeignKey(record => record.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.ContributionBatch)
            .WithMany(batch => batch.LedgerTransactions)
            .HasForeignKey(record => record.ContributionBatchId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ContributionBatchLine)
            .WithMany(line => line.LedgerTransactions)
            .HasForeignKey(record => record.ContributionBatchLineId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ReversalReference)
            .WithMany(record => record.Reversals)
            .HasForeignKey(record => record.ReversalReferenceId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundWithdrawalRequest(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundWithdrawalRequest> entity)
    {
        entity.ToTable("ProvidentFundWithdrawalRequests");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.RequestNumber).HasMaxLength(64).IsRequired();
        entity.Property(record => record.RequestDate).HasColumnType("date");
        entity.Property(record => record.WithdrawalType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.RequestedAmount).HasPrecision(18, 2);
        entity.Property(record => record.EligibleWithdrawableAmount).HasPrecision(18, 2);
        entity.Property(record => record.ApprovedAmount).HasPrecision(18, 2);
        entity.Property(record => record.Reason).HasMaxLength(2000);
        entity.Property(record => record.AttachmentPath).HasMaxLength(512);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.PaymentDate).HasPrecision(0);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.RequestNumber).IsUnique();
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.EnrollmentId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.RequestDate);
        entity.HasIndex(record => record.WithdrawalType);

        entity.HasOne(record => record.Employee)
            .WithMany()
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Enrollment)
            .WithMany(enrollment => enrollment.WithdrawalRequests)
            .HasForeignKey(record => record.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.UpdatedByUser)
            .WithMany()
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundWithdrawalApproval(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundWithdrawalApproval> entity)
    {
        entity.ToTable("ProvidentFundWithdrawalApprovals");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.StepName).HasMaxLength(64).IsRequired();
        entity.Property(record => record.Action).HasMaxLength(64).IsRequired();
        entity.Property(record => record.ActorNameSnapshot).HasMaxLength(256);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.WithdrawalRequestId);
        entity.HasIndex(record => record.ActorUserId);

        entity.HasOne(record => record.WithdrawalRequest)
            .WithMany(request => request.Approvals)
            .HasForeignKey(record => record.WithdrawalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(record => record.ActorUser)
            .WithMany()
            .HasForeignKey(record => record.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundAdjustment(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundAdjustment> entity)
    {
        entity.ToTable("ProvidentFundAdjustments");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.AdjustmentType).HasMaxLength(32).IsRequired();
        entity.Property(record => record.AdjustmentDate).HasColumnType("date");
        entity.Property(record => record.Amount).HasPrecision(18, 2);
        entity.Property(record => record.ShareAffected).HasMaxLength(32).IsRequired();
        entity.Property(record => record.Reason).HasMaxLength(2000).IsRequired();
        entity.Property(record => record.AttachmentPath).HasMaxLength(512);
        entity.Property(record => record.Status).HasMaxLength(32).IsRequired();
        entity.Property(record => record.ApprovedAtUtc).HasPrecision(0);
        entity.Property(record => record.PostedAtUtc).HasPrecision(0);
        entity.Property(record => record.DecisionRemarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.Property(record => record.UpdatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.EmployeeId);
        entity.HasIndex(record => record.EnrollmentId);
        entity.HasIndex(record => record.Status);
        entity.HasIndex(record => record.AdjustmentDate);
        entity.HasIndex(record => record.ShareAffected);

        entity.HasOne(record => record.Employee)
            .WithMany()
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.Enrollment)
            .WithMany(enrollment => enrollment.Adjustments)
            .HasForeignKey(record => record.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(record => record.RequestedByUser)
            .WithMany()
            .HasForeignKey(record => record.RequestedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(record => record.ApprovedByUser)
            .WithMany()
            .HasForeignKey(record => record.ApprovedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureProvidentFundAdjustmentApproval(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ProvidentFundAdjustmentApproval> entity)
    {
        entity.ToTable("ProvidentFundAdjustmentApprovals");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Action).HasMaxLength(64).IsRequired();
        entity.Property(record => record.ActorNameSnapshot).HasMaxLength(256);
        entity.Property(record => record.Remarks).HasMaxLength(1000);
        entity.Property(record => record.CreatedAtUtc).HasPrecision(0);
        entity.HasIndex(record => record.AdjustmentId);
        entity.HasIndex(record => record.ActorUserId);

        entity.HasOne(record => record.Adjustment)
            .WithMany(adjustment => adjustment.Approvals)
            .HasForeignKey(record => record.AdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(record => record.ActorUser)
            .WithMany()
            .HasForeignKey(record => record.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private List<AuditLog> BuildAuditLogs()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        var principal = httpContext?.User;
        var actorUserId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorName = principal?.FindFirstValue(ClaimTypes.Name)
            ?? principal?.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return [];
        }

        var logs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries().Where(ShouldAudit))
        {
            var action = ResolveAuditAction(entry);
            var entityType = ResolveEntityType(entry.Entity.GetType());
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entityType))
            {
                continue;
            }

            var (oldValues, newValues) = CaptureAuditValues(entry);
            if ((oldValues?.Count ?? 0) == 0 && (newValues?.Count ?? 0) == 0)
            {
                continue;
            }

            logs.Add(new AuditLog
            {
                ActorUserId = actorUserId,
                ActorNameSnapshot = Normalize(actorName, 256),
                Action = action,
                EntityType = entityType,
                EntityId = ResolveEntityId(entry),
                EmployeeId = ResolveEmployeeId(entry),
                OldValuesJson = SerializeSanitized(oldValues),
                NewValuesJson = SerializeSanitized(newValues),
                IpAddress = Normalize(ipAddress, 64),
                UserAgent = Normalize(userAgent, 512),
                Remarks = string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return logs;
    }

    private static bool ShouldAudit(EntityEntry entry)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return false;
        }

        return entry.Entity switch
        {
            AuditLog => false,
            RefreshToken => false,
            SavedReport => false,
            NotificationRecord => false,
            PayrollAuditLog => false,
            _ => true
        };
    }

    private static string ResolveAuditAction(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => "create",
            EntityState.Deleted => "delete",
            EntityState.Modified => ResolveModifiedAction(entry),
            _ => string.Empty
        };
    }

    private static string ResolveModifiedAction(EntityEntry entry)
    {
        var statusProperty = entry.Properties.FirstOrDefault(item => item.Metadata.Name == "Status" && item.IsModified);
        if (statusProperty is not null)
        {
            var normalized = (statusProperty.CurrentValue?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "approved" => "approve",
                "rejected" => "reject",
                "cancelled" => "cancel",
                "paid" => "mark_paid",
                _ => "update"
            };
        }

        var isArchivedProperty = entry.Properties.FirstOrDefault(item => item.Metadata.Name == "IsArchived" && item.IsModified);
        if (isArchivedProperty?.CurrentValue is bool isArchived && isArchived)
        {
            return "archive";
        }

        var isActiveProperty = entry.Properties.FirstOrDefault(item => item.Metadata.Name == "IsActive" && item.IsModified);
        if (isActiveProperty?.CurrentValue is bool isActive && !isActive)
        {
            return "deactivate";
        }

        return "update";
    }

    private static (Dictionary<string, object?>? OldValues, Dictionary<string, object?>? NewValues) CaptureAuditValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return (null, CaptureValues(entry, includeModifiedOnly: false, useOriginal: false));
        }

        if (entry.State == EntityState.Deleted)
        {
            return (CaptureValues(entry, includeModifiedOnly: false, useOriginal: true), null);
        }

        var oldValues = CaptureValues(entry, includeModifiedOnly: true, useOriginal: true);
        var newValues = CaptureValues(entry, includeModifiedOnly: true, useOriginal: false);
        return (oldValues, newValues);
    }

    private static Dictionary<string, object?> CaptureValues(EntityEntry entry, bool includeModifiedOnly, bool useOriginal)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var primaryKey = entry.Metadata.FindPrimaryKey();
        var primaryKeyNames = primaryKey?.Properties.Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty())
            {
                continue;
            }

            if (includeModifiedOnly && !property.IsModified && !primaryKeyNames.Contains(property.Metadata.Name))
            {
                continue;
            }

            values[property.Metadata.Name] = entry.State == EntityState.Deleted || useOriginal
                ? property.OriginalValue
                : property.CurrentValue;
        }

        return values;
    }

    private static string ResolveEntityType(Type type)
    {
        var entityType = Nullable.GetUnderlyingType(type) ?? type;

        return entityType.Name switch
        {
            nameof(Employee) => AuditEntityTypes.Employee,
            nameof(Department) => AuditEntityTypes.Department,
            nameof(Position) => AuditEntityTypes.Position,
            nameof(Branch) => AuditEntityTypes.Branch,
            nameof(EmploymentType) => AuditEntityTypes.EmploymentType,
            nameof(EmploymentStatus) => AuditEntityTypes.EmploymentStatus,
            nameof(DocumentType) => AuditEntityTypes.DocumentType,
            nameof(EmployeeDocument) => AuditEntityTypes.EmployeeDocument,
            nameof(AttendanceRecord) => AuditEntityTypes.AttendanceRecord,
            nameof(AttendanceAdjustmentRequest) => AuditEntityTypes.AttendanceAdjustmentRequest,
            nameof(LeaveRequest) => AuditEntityTypes.LeaveRequest,
            nameof(EmployeeLeaveBalance) => AuditEntityTypes.LeaveBalance,
            nameof(EmployeeProfileChangeRequest) => AuditEntityTypes.ProfileChangeRequest,
            nameof(CompensationProfile) => AuditEntityTypes.CompensationProfile,
            nameof(EmployeeRecurringEarning) => AuditEntityTypes.RecurringEarning,
            nameof(EmployeeRecurringDeduction) => AuditEntityTypes.RecurringDeduction,
            nameof(PayrollRun) => AuditEntityTypes.PayrollRun,
            nameof(PayrollAdjustment) => AuditEntityTypes.PayrollAdjustment,
            nameof(ApplicationUser) => AuditEntityTypes.User,
            nameof(ApplicationUserRole) => AuditEntityTypes.RoleAssignment,
            nameof(PayrollRunItem) => AuditEntityTypes.Payslip,
            _ => ToSnakeCase(entityType.Name)
        };
    }

    private static string ResolveEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
        {
            return string.Empty;
        }

        var values = primaryKey.Properties
            .Select(property =>
            {
                var trackedProperty = entry.Property(property.Name);
                var value = trackedProperty.CurrentValue ?? trackedProperty.OriginalValue;
                return value?.ToString() ?? string.Empty;
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return values.Length == 0 ? string.Empty : string.Join("|", values);
    }

    private static Guid? ResolveEmployeeId(EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted && entry.Entity is Employee)
        {
            return null;
        }

        if (entry.Entity is Employee employee)
        {
            return employee.Id;
        }

        return TryReadGuid(entry, "EmployeeId");
    }

    private static Guid? ResolveDeferredEmployeeId(string? newValuesJson, string? oldValuesJson)
    {
        return ExtractGuidFromJson(newValuesJson, "EmployeeId")
            ?? ExtractGuidFromJson(oldValuesJson, "EmployeeId");
    }

    private static string ResolveDeferredEntityId(string entityType, string? newValuesJson, string? oldValuesJson)
    {
        var id = ExtractStringFromJson(newValuesJson, "Id")
            ?? ExtractStringFromJson(oldValuesJson, "Id")
            ?? string.Empty;

        return entityType == AuditEntityTypes.RoleAssignment
            ? ExtractStringFromJson(newValuesJson, "UserId") ?? ExtractStringFromJson(oldValuesJson, "UserId") ?? id
            : id;
    }

    private static Guid? TryReadGuid(EntityEntry entry, string propertyName)
    {
        var property = entry.Properties.FirstOrDefault(item => item.Metadata.Name == propertyName);
        if (property?.CurrentValue is Guid currentGuid && currentGuid != Guid.Empty)
        {
            return currentGuid;
        }

        if (property?.OriginalValue is Guid originalGuid && originalGuid != Guid.Empty)
        {
            return originalGuid;
        }

        return null;
    }

    private static string SerializeSanitized(Dictionary<string, object?>? values)
    {
        if (values is null || values.Count == 0)
        {
            return string.Empty;
        }

        var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            if (IsSecretField(key))
            {
                sanitized[key] = "[redacted]";
            }
            else if (IsGovernmentIdField(key))
            {
                sanitized[key] = MaskGovernmentValue(value?.ToString());
            }
            else
            {
                sanitized[key] = value;
            }
        }

        return JsonSerializer.Serialize(sanitized);
    }

    private static bool IsSecretField(string propertyName)
    {
        return propertyName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("securitystamp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGovernmentIdField(string propertyName)
    {
        return propertyName.Contains("sss", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("philhealth", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("pagibig", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("pag-ibig", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("tin", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("governmentid", StringComparison.OrdinalIgnoreCase);
    }

    private static string MaskGovernmentValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
    }

    private static string Normalize(string? value, int maxLength)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static Guid? ExtractGuidFromJson(string? json, string propertyName)
    {
        var value = ExtractStringFromJson(json, propertyName);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static string? ExtractStringFromJson(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(propertyName, out var value))
            {
                return value.ToString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new List<char>(value.Length + 8);
        for (var index = 0; index < value.Length; index += 1)
        {
            var character = value[index];
            if (char.IsUpper(character))
            {
                if (index > 0)
                {
                    buffer.Add('_');
                }

                buffer.Add(char.ToLowerInvariant(character));
            }
            else
            {
                buffer.Add(character);
            }
        }

        return new string(buffer.ToArray());
    }
}
