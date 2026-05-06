using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Employees;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Operations;
using Sixram.Api.DTOs.Organization;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IProductionReadinessService
{
    Task<ProductionReadinessOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<DataImportPreviewDto> PreviewImportAsync(PreviewDataImportRequestDto request, CancellationToken cancellationToken = default);

    Task<DataImportApplyResultDto> ApplyImportAsync(ApplyDataImportRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);
}

public class ProductionReadinessService : IProductionReadinessService
{
    private const long MaxImportFileBytes = 5 * 1024 * 1024;
    private const int MaxImportRows = 5000;
    private static readonly string[] SupportedImportExtensions = [".csv"];
    private static readonly string[] SupportedDateFormats = ["yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy", "yyyy/M/d"];

    private readonly ApplicationDbContext _dbContext;
    private readonly IEmployeeService _employeeService;
    private readonly IOrganizationSetupService _organizationSetupService;
    private readonly IPayrollCompensationService _payrollCompensationService;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProductionReadinessService> _logger;

    public ProductionReadinessService(
        ApplicationDbContext dbContext,
        IEmployeeService employeeService,
        IOrganizationSetupService organizationSetupService,
        IPayrollCompensationService payrollCompensationService,
        IAttendanceCalculationService attendanceCalculationService,
        IAuditLogService auditLogService,
        ILogger<ProductionReadinessService> logger)
    {
        _dbContext = dbContext;
        _employeeService = employeeService;
        _organizationSetupService = organizationSetupService;
        _payrollCompensationService = payrollCompensationService;
        _attendanceCalculationService = attendanceCalculationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ProductionReadinessOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var businessDate = _attendanceCalculationService.GetBusinessToday();
        var activeEmployeeCount = await _dbContext.Employees.CountAsync(record => record.IsActive, cancellationToken);
        var employeeCount = await _dbContext.Employees.CountAsync(cancellationToken);
        var linkedEmployeeCount = await _dbContext.Employees.CountAsync(record => record.IsActive && record.UserId != null, cancellationToken);
        var activeDepartmentCount = await _dbContext.Departments.CountAsync(record => record.IsActive, cancellationToken);
        var activePositionCount = await _dbContext.Positions.CountAsync(record => record.IsActive, cancellationToken);
        var activeBranchCount = await _dbContext.Branches.CountAsync(record => record.IsActive, cancellationToken);
        var activeEmploymentTypeCount = await _dbContext.EmploymentTypes.CountAsync(record => record.IsActive, cancellationToken);
        var activeEmploymentStatusCount = await _dbContext.EmploymentStatuses.CountAsync(record => record.IsActive, cancellationToken);
        var activeDocumentTypeCount = await _dbContext.DocumentTypes.CountAsync(record => record.IsActive, cancellationToken);
        var requiredDocumentTypeCount = await _dbContext.DocumentTypes.CountAsync(record => record.IsActive && record.IsRequired, cancellationToken);
        var currentYear = businessDate.Year;

        var activeEmployeeIds = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.IsActive)
            .Select(record => record.Id)
            .ToListAsync(cancellationToken);

        var employeesWithScheduleCount = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Where(record =>
                record.IsActive &&
                record.EffectiveStartDate <= businessDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= businessDate))
            .Select(record => record.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var employeesWithCompensationCount = await _dbContext.CompensationProfiles
            .AsNoTracking()
            .Where(record =>
                record.IsActive &&
                record.EffectiveStartDate <= businessDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= businessDate))
            .Select(record => record.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var employeesWithLeaveBalancesCount = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Where(record => record.PeriodYear == currentYear)
            .Select(record => record.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var roleNames = await _dbContext.Roles
            .AsNoTracking()
            .Select(record => record.Name ?? string.Empty)
            .ToListAsync(cancellationToken);

        var payrollSettingsCount = await _dbContext.PayrollSettings.CountAsync(cancellationToken);
        var payPeriodTemplateCount = await _dbContext.PayPeriodTemplates.CountAsync(record => record.IsActive, cancellationToken);
        var earningTypeCount = await _dbContext.EarningTypes.CountAsync(record => record.IsActive, cancellationToken);
        var deductionTypeCount = await _dbContext.DeductionTypes.CountAsync(record => record.IsActive, cancellationToken);

        var reportExportCount = await _dbContext.AuditLogs
            .AsNoTracking()
            .CountAsync(
                record =>
                    record.EntityType == AuditEntityTypes.Report &&
                    record.Action == "export" &&
                    record.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30),
                cancellationToken);

        var requiredDocumentTypes = await _dbContext.DocumentTypes
            .AsNoTracking()
            .Where(record => record.IsActive && record.IsRequired)
            .Select(record => new { record.Id })
            .ToListAsync(cancellationToken);
        var activeEmployeeDocuments = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Where(record => activeEmployeeIds.Contains(record.EmployeeId) && !record.IsArchived)
            .Select(record => new { record.EmployeeId, record.DocumentTypeId, record.ExpiryDate })
            .ToListAsync(cancellationToken);
        var employeeDocumentsByEmployee = activeEmployeeDocuments
            .GroupBy(record => record.EmployeeId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var missingRequiredDocumentCount = 0;
        foreach (var employeeId in activeEmployeeIds)
        {
            var employeeDocuments = employeeDocumentsByEmployee.GetValueOrDefault(employeeId) ?? [];
            foreach (var documentType in requiredDocumentTypes)
            {
                if (!employeeDocuments.Any(record => record.DocumentTypeId == documentType.Id))
                {
                    missingRequiredDocumentCount++;
                }
            }
        }

        var expiredDocumentCount = activeEmployeeDocuments.Count(record => record.ExpiryDate.HasValue && record.ExpiryDate.Value < businessDate);

        var sections = new List<ProductionReadinessSectionDto>
        {
            new()
            {
                Key = "foundation",
                Title = "Foundation",
                Description = "Verify master data, employee records, and access setup before live transactions begin.",
                Items =
                [
                    BuildStatusItem(
                        "organization_setup_complete",
                        "Organization setup complete",
                        activeDepartmentCount > 0 && activePositionCount > 0 && activeBranchCount > 0 && activeEmploymentTypeCount > 0 && activeEmploymentStatusCount > 0
                            ? ProductionReadinessStates.Ready
                            : ProductionReadinessStates.Blocked,
                        $"Departments: {activeDepartmentCount}, positions: {activePositionCount}, branches: {activeBranchCount}, employment types: {activeEmploymentTypeCount}, employment statuses: {activeEmploymentStatusCount}.",
                        "/admin/organization"),
                    BuildStatusItem(
                        "employee_records_imported",
                        "Employee records imported",
                        employeeCount == 0 ? ProductionReadinessStates.Blocked : ProductionReadinessStates.Ready,
                        $"{employeeCount} employee profile(s) currently loaded.",
                        "/admin/employees"),
                    BuildStatusItem(
                        "user_accounts_linked",
                        "User accounts linked to employees",
                        activeEmployeeCount == 0
                            ? ProductionReadinessStates.Attention
                            : linkedEmployeeCount == activeEmployeeCount
                                ? ProductionReadinessStates.Ready
                                : ProductionReadinessStates.Attention,
                        $"{linkedEmployeeCount} of {activeEmployeeCount} active employee(s) have linked user accounts.",
                        "/admin/employees"),
                    BuildStatusItem(
                        "roles_reviewed",
                        "Roles and permissions reviewed",
                        SystemRoles.Defaults.All(roleName => roleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                            ? ProductionReadinessStates.Ready
                            : ProductionReadinessStates.Attention,
                        $"Configured roles: {string.Join(", ", roleNames.OrderBy(roleName => roleName))}.",
                        "/admin/rbac")
                ]
            },
            new()
            {
                Key = "operations",
                Title = "Operational Readiness",
                Description = "Confirm the records that drive attendance, leave, compliance, and payroll are in place.",
                Items =
                [
                    BuildStatusItem(
                        "leave_balances_imported",
                        "Leave balances imported",
                        activeEmployeeCount == 0
                            ? ProductionReadinessStates.Attention
                            : employeesWithLeaveBalancesCount == 0
                                ? ProductionReadinessStates.Blocked
                                : employeesWithLeaveBalancesCount == activeEmployeeCount
                                    ? ProductionReadinessStates.Ready
                                    : ProductionReadinessStates.Attention,
                        $"{employeesWithLeaveBalancesCount} of {activeEmployeeCount} active employee(s) have leave balances for {currentYear}.",
                        "/admin/production-readiness"),
                    BuildStatusItem(
                        "compensation_profiles_completed",
                        "Compensation profiles completed",
                        activeEmployeeCount == 0
                            ? ProductionReadinessStates.Attention
                            : employeesWithCompensationCount == 0
                                ? ProductionReadinessStates.Blocked
                                : employeesWithCompensationCount == activeEmployeeCount
                                    ? ProductionReadinessStates.Ready
                                    : ProductionReadinessStates.Attention,
                        $"{employeesWithCompensationCount} of {activeEmployeeCount} active employee(s) have a current compensation profile.",
                        "/admin/payroll/compensation"),
                    BuildStatusItem(
                        "schedules_assigned",
                        "Schedules assigned",
                        activeEmployeeCount == 0
                            ? ProductionReadinessStates.Attention
                            : employeesWithScheduleCount == 0
                                ? ProductionReadinessStates.Blocked
                                : employeesWithScheduleCount == activeEmployeeCount
                                    ? ProductionReadinessStates.Ready
                                    : ProductionReadinessStates.Attention,
                        $"{employeesWithScheduleCount} of {activeEmployeeCount} active employee(s) have an effective work schedule today.",
                        "/admin/attendance/assignments"),
                    BuildStatusItem(
                        "payroll_settings_configured",
                        "Payroll settings configured",
                        payrollSettingsCount > 0 && payPeriodTemplateCount > 0 && earningTypeCount > 0 && deductionTypeCount > 0
                            ? ProductionReadinessStates.Ready
                            : ProductionReadinessStates.Attention,
                        $"Settings: {payrollSettingsCount}, pay period templates: {payPeriodTemplateCount}, earning types: {earningTypeCount}, deduction types: {deductionTypeCount}.",
                        "/admin/payroll/setup"),
                    BuildStatusItem(
                        "document_types_configured",
                        "Document types configured",
                        activeDocumentTypeCount == 0
                            ? ProductionReadinessStates.Blocked
                            : requiredDocumentTypeCount == 0
                                ? ProductionReadinessStates.Attention
                                : ProductionReadinessStates.Ready,
                        $"Active document types: {activeDocumentTypeCount}, required document types: {requiredDocumentTypeCount}.",
                        "/admin/document-types"),
                    BuildStatusItem(
                        "required_documents_reviewed",
                        "Required documents reviewed",
                        missingRequiredDocumentCount == 0 && expiredDocumentCount == 0
                            ? ProductionReadinessStates.Ready
                            : ProductionReadinessStates.Attention,
                        $"Missing required documents: {missingRequiredDocumentCount}. Expired documents: {expiredDocumentCount}.",
                        "/compliance")
                ]
            },
            new()
            {
                Key = "go_live",
                Title = "Go-Live Controls",
                Description = "Final manual checks that should be confirmed by the implementation team before launch.",
                Items =
                [
                    BuildStatusItem(
                        "reports_tested",
                        "Reports and exports tested",
                        reportExportCount > 0 ? ProductionReadinessStates.Ready : ProductionReadinessStates.Manual,
                        reportExportCount > 0
                            ? $"{reportExportCount} report export action(s) were audited in the last 30 days."
                            : "Run at least one end-to-end report export during UAT and verify permissions by role.",
                        "/reports"),
                    BuildStatusItem(
                        "backup_plan_confirmed",
                        "Database and file backup plan confirmed",
                        ProductionReadinessStates.Manual,
                        "Review the documented SQL Server backup plan, uploaded-file backup requirement, and retention guidance before go-live.",
                        string.Empty),
                    BuildStatusItem(
                        "uat_completed",
                        "UAT and sign-off completed",
                        ProductionReadinessStates.Manual,
                        "Confirm workflow testing, user training, and go-live approvals with HR, payroll, and IT stakeholders.",
                        string.Empty)
                ]
            }
        };

        var scoredItems = sections
            .SelectMany(section => section.Items)
            .Where(item => item.Status != ProductionReadinessStates.Manual)
            .ToList();

        var score = scoredItems.Sum(item => item.Status switch
        {
            ProductionReadinessStates.Ready => 1m,
            ProductionReadinessStates.Attention => 0.5m,
            _ => 0m
        });

        return new ProductionReadinessOverviewDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ReadinessPercent = scoredItems.Count == 0 ? 0 : (int)Math.Round((score / scoredItems.Count) * 100m, MidpointRounding.AwayFromZero),
            ReadyItemCount = sections.SelectMany(section => section.Items).Count(item => item.Status == ProductionReadinessStates.Ready),
            AttentionItemCount = sections.SelectMany(section => section.Items).Count(item => item.Status == ProductionReadinessStates.Attention),
            BlockedItemCount = sections.SelectMany(section => section.Items).Count(item => item.Status == ProductionReadinessStates.Blocked),
            Sections = sections,
            AvailableImports = GetImportDefinitions(),
            OperationalGuidance =
            [
                new OperationalGuidanceItemDto
                {
                    Key = "db_backup",
                    Title = "SQL Server backup",
                    Description = "Use a scheduled full backup plan for SixramDB plus differential or transaction-log backups that match your RPO and RTO."
                },
                new OperationalGuidanceItemDto
                {
                    Key = "file_backup",
                    Title = "Uploaded file backup",
                    Description = "Back up the App_Data document and attachment directories together with the database so document metadata never points to missing files."
                },
                new OperationalGuidanceItemDto
                {
                    Key = "audit_retention",
                    Title = "Audit and export retention",
                    Description = "Review retention and cleanup intervals for audit logs, notification history, generated exports, and temporary operational files."
                },
                new OperationalGuidanceItemDto
                {
                    Key = "monitoring",
                    Title = "Operational monitoring",
                    Description = "Capture application errors, failed imports, blocked permissions, and SQL Server health signals in the environment that will host the API."
                }
            ]
        };
    }

    public async Task<DataImportPreviewDto> PreviewImportAsync(PreviewDataImportRequestDto request, CancellationToken cancellationToken = default)
    {
        var batch = await BuildBatchAsync(request.ImportType, request.File, null, cancellationToken);
        return batch.ToPreview();
    }

    public async Task<DataImportApplyResultDto> ApplyImportAsync(ApplyDataImportRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var batch = await BuildBatchAsync(request.ImportType, request.File, actorUserId, cancellationToken);
        if (batch.InvalidRowCount > 0)
        {
            throw new BadRequestException(
                "The uploaded file still contains invalid rows. Fix the preview errors before applying the import.",
                new Dictionary<string, string[]>
                {
                    ["file"] = ["The import preview contains invalid rows."]
                });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var action in batch.Actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action.ApplyAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation(
            "Production import {ImportType} applied with {CreatedCount} created and {UpdatedCount} updated row(s).",
            batch.ImportType,
            batch.CreatedCount,
            batch.UpdatedCount);

        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Action = "import",
                EntityType = AuditEntityTypes.DataImport,
                EntityId = batch.ImportType,
                NewValues = new
                {
                    batch.ImportType,
                    batch.ImportName,
                    batch.FileName,
                    ProcessedCount = batch.Actions.Count,
                    batch.CreatedCount,
                    batch.UpdatedCount
                },
                Remarks = "Applied production-readiness data import."
            },
            cancellationToken);

        return new DataImportApplyResultDto
        {
            ImportType = batch.ImportType,
            ImportName = batch.ImportName,
            FileName = batch.FileName,
            ProcessedCount = batch.Actions.Count,
            CreatedCount = batch.CreatedCount,
            UpdatedCount = batch.UpdatedCount,
            SkippedCount = 0,
            ErrorCount = 0,
            AppliedAtUtc = DateTime.UtcNow,
            Rows = batch.Rows
        };
    }

    private async Task<ImportBatch> BuildBatchAsync(string importType, IFormFile? file, string? actorUserId, CancellationToken cancellationToken)
    {
        var definition = GetImportDefinition(importType);
        ValidateImportFile(file);
        var csvFile = ReadCsvAsync(file!, cancellationToken);

        if (csvFile.Rows.Count == 0)
        {
            throw new BadRequestException("The CSV file does not contain any data rows.");
        }

        return definition.Key switch
        {
            DataImportTypes.Employees => await BuildEmployeeBatchAsync(definition, file!, csvFile, cancellationToken),
            DataImportTypes.Departments => await BuildDepartmentBatchAsync(definition, file!, csvFile, cancellationToken),
            DataImportTypes.Positions => await BuildPositionBatchAsync(definition, file!, csvFile, cancellationToken),
            DataImportTypes.Branches => await BuildBranchBatchAsync(definition, file!, csvFile, cancellationToken),
            DataImportTypes.EmploymentTypes => await BuildEmploymentTypeBatchAsync(definition, file!, csvFile, cancellationToken),
            DataImportTypes.EmploymentStatuses => await BuildEmploymentStatusBatchAsync(definition, file!, csvFile, cancellationToken),
            DataImportTypes.LeaveBalances => await BuildLeaveBalanceBatchAsync(definition, file!, csvFile, actorUserId, cancellationToken),
            DataImportTypes.CompensationProfiles => await BuildCompensationProfileBatchAsync(definition, file!, csvFile, actorUserId, cancellationToken),
            _ => throw new NotFoundException($"Import type '{importType}' is not supported.")
        };
    }

    private async Task<ImportBatch> BuildEmployeeBatchAsync(DataImportDefinitionDto definition, IFormFile file, ParsedCsvFile csvFile, CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);

        var departmentMap = await _dbContext.Departments.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var positionMap = await _dbContext.Positions.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var branchMap = await _dbContext.Branches.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var employmentTypeMap = await _dbContext.EmploymentTypes.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var employmentStatusMap = await _dbContext.EmploymentStatuses.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var managerMap = await _dbContext.Employees.AsNoTracking()
            .ToDictionaryAsync(record => record.EmployeeCode, record => record.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var userMap = await _dbContext.Users.AsNoTracking()
            .Where(record => record.Email != null)
            .ToDictionaryAsync(record => record.Email!, record => record.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingEmployees = await _dbContext.Employees.AsNoTracking()
            .Select(record => new { record.Id, record.EmployeeCode, record.UserId })
            .ToListAsync(cancellationToken);
        var employeeIdByCode = existingEmployees.ToDictionary(record => record.EmployeeCode, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var linkedEmployeeByUserId = existingEmployees
            .Where(record => !string.IsNullOrWhiteSpace(record.UserId))
            .ToDictionary(record => record.UserId!, record => record.Id, StringComparer.OrdinalIgnoreCase);

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<DataImportPreviewRowDto>();
        var actions = new List<PreparedImportAction>();

        foreach (var row in csvFile.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = new List<string>();
            var employeeCode = NormalizeCode(GetValue(row, "employee_code"));
            var identifier = employeeCode;

            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                messages.Add("Employee code is required.");
            }
            else if (!seenKeys.Add(employeeCode))
            {
                messages.Add($"Employee code '{employeeCode}' appears more than once in this file.");
            }

            var firstName = GetRequiredText(row, "first_name", "First name", messages);
            var lastName = GetRequiredText(row, "last_name", "Last name", messages);
            var gender = GetRequiredText(row, "gender", "Gender", messages);
            var departmentCode = NormalizeCode(GetRequiredText(row, "department_code", "Department code", messages));
            var positionCode = NormalizeCode(GetRequiredText(row, "position_code", "Position code", messages));
            var branchCode = NormalizeCode(GetRequiredText(row, "branch_code", "Branch code", messages));
            var employmentTypeCode = NormalizeCode(GetRequiredText(row, "employment_type_code", "Employment type code", messages));
            var employmentStatusCode = NormalizeCode(GetRequiredText(row, "employment_status_code", "Employment status code", messages));
            var dateHired = ParseDate(row, "date_hired", messages, isRequired: true);

            var department = ResolveLookup(departmentMap, departmentCode, "department", "department_code", messages);
            var position = ResolveLookup(positionMap, positionCode, "position", "position_code", messages);
            var branch = ResolveLookup(branchMap, branchCode, "branch", "branch_code", messages);
            var employmentType = ResolveLookup(employmentTypeMap, employmentTypeCode, "employment type", "employment_type_code", messages);
            var employmentStatus = ResolveLookup(employmentStatusMap, employmentStatusCode, "employment status", "employment_status_code", messages);

            if (department is not null && position is not null && position.DepartmentId is not null && position.DepartmentId != department.Id)
            {
                messages.Add($"Position '{position.Code}' belongs to a different department.");
            }

            var managerCode = NormalizeCode(GetValue(row, "manager_employee_code"));
            Guid? managerId = null;
            if (!string.IsNullOrWhiteSpace(managerCode))
            {
                if (string.Equals(managerCode, employeeCode, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add("An employee cannot report to themselves.");
                }
                else if (!managerMap.TryGetValue(managerCode, out var resolvedManagerId))
                {
                    messages.Add($"Manager employee code '{managerCode}' was not found.");
                }
                else
                {
                    managerId = resolvedManagerId;
                }
            }

            string? linkedUserId = null;
            var userEmail = NormalizeEmail(GetValue(row, "user_email"));
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                if (!userMap.TryGetValue(userEmail, out var resolvedUserId))
                {
                    messages.Add($"User email '{userEmail}' was not found.");
                }
                else
                {
                    linkedUserId = resolvedUserId;
                    if (linkedEmployeeByUserId.TryGetValue(resolvedUserId, out var linkedEmployeeId) &&
                        (!employeeIdByCode.TryGetValue(employeeCode, out var existingEmployeeId) || existingEmployeeId != linkedEmployeeId))
                    {
                        messages.Add($"User email '{userEmail}' is already linked to another employee profile.");
                    }
                }
            }

            var request = new SaveEmployeeRequestDto
            {
                EmployeeCode = employeeCode,
                FirstName = firstName,
                MiddleName = GetValue(row, "middle_name"),
                LastName = lastName,
                Suffix = GetValue(row, "suffix"),
                Gender = gender,
                BirthDate = ParseDate(row, "birth_date", messages),
                CivilStatus = GetValue(row, "civil_status"),
                Nationality = GetValue(row, "nationality"),
                MobileNumber = GetValue(row, "mobile_number"),
                Email = NormalizeEmail(GetValue(row, "email")),
                Address = GetValue(row, "address"),
                CityProvince = GetValue(row, "city_province"),
                PostalCode = GetValue(row, "postal_code"),
                EmergencyContactName = GetValue(row, "emergency_contact_name"),
                EmergencyContactRelationship = GetValue(row, "emergency_contact_relationship"),
                EmergencyContactPhone = GetValue(row, "emergency_contact_phone"),
                DepartmentId = department?.Id,
                PositionId = position?.Id,
                BranchId = branch?.Id,
                EmploymentTypeId = employmentType?.Id,
                EmploymentStatusId = employmentStatus?.Id,
                ManagerId = managerId,
                WorkSchedule = GetValue(row, "work_schedule"),
                DateHired = dateHired,
                DateRegularized = ParseDate(row, "date_regularized", messages),
                DateSeparated = ParseDate(row, "date_separated", messages),
                SssNumber = GetValue(row, "sss_number"),
                PhilHealthNumber = GetValue(row, "philhealth_number"),
                PagIbigNumber = GetValue(row, "pagibig_number"),
                TinNumber = GetValue(row, "tin_number"),
                OtherGovernmentId = GetValue(row, "other_government_id"),
                UserId = linkedUserId,
                IsActive = ParseBoolean(row, "is_active", defaultValue: true, messages)
            };

            ValidateModel(request, messages);

            var operation = employeeIdByCode.ContainsKey(employeeCode) ? "update" : "create";
            var previewRow = BuildPreviewRow(row, operation, identifier, messages);
            rows.Add(previewRow);

            if (messages.Count == 0)
            {
                actions.Add(new PreparedImportAction(
                    operation,
                    previewRow,
                    async token =>
                    {
                        if (employeeIdByCode.TryGetValue(employeeCode, out var existingEmployeeId))
                        {
                            await _employeeService.UpdateEmployeeAsync(existingEmployeeId, request, token);
                        }
                        else
                        {
                            var created = await _employeeService.CreateEmployeeAsync(request, token);
                            employeeIdByCode[employeeCode] = created.Id;
                        }
                    }));
            }
        }

        return new ImportBatch(definition.Key, definition.Name, file.FileName, GetDisplayColumns(definition), rows, actions);
    }

    private async Task<ImportBatch> BuildDepartmentBatchAsync(DataImportDefinitionDto definition, IFormFile file, ParsedCsvFile csvFile, CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var existingRecords = await _dbContext.Departments.AsNoTracking()
            .Select(record => new { record.Id, record.Code, record.Name })
            .ToListAsync(cancellationToken);

        var codeMap = existingRecords.ToDictionary(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var nameMap = existingRecords.ToDictionary(record => record.Name, record => record.Id, StringComparer.OrdinalIgnoreCase);

        return BuildOrganizationBatch(
            definition,
            file,
            csvFile,
            codeMap,
            nameMap,
            (row, isActive) => new SaveDepartmentRequestDto
            {
                Code = NormalizeCode(GetRequiredText(row, "code", "Code", [])),
                Name = GetValue(row, "name").Trim(),
                Description = GetValue(row, "description"),
                IsActive = isActive
            },
            async (request, existingId, token) =>
            {
                if (existingId is null)
                {
                    await _organizationSetupService.CreateDepartmentAsync(request, token);
                }
                else
                {
                    await _organizationSetupService.UpdateDepartmentAsync(existingId.Value, request, token);
                }
            },
            cancellationToken);
    }

    private async Task<ImportBatch> BuildPositionBatchAsync(DataImportDefinitionDto definition, IFormFile file, ParsedCsvFile csvFile, CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var departmentMap = await _dbContext.Departments.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingRecords = await _dbContext.Positions.AsNoTracking()
            .Select(record => new { record.Id, record.Code, record.Name })
            .ToListAsync(cancellationToken);

        var codeMap = existingRecords.ToDictionary(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var nameMap = existingRecords.ToDictionary(record => record.Name, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<DataImportPreviewRowDto>();
        var actions = new List<PreparedImportAction>();

        foreach (var row in csvFile.Rows)
        {
            var messages = new List<string>();
            var code = NormalizeCode(GetValue(row, "code"));
            if (string.IsNullOrWhiteSpace(code))
            {
                messages.Add("Code is required.");
            }
            else if (!seenCodes.Add(code))
            {
                messages.Add($"Code '{code}' appears more than once in this file.");
            }

            var name = GetRequiredText(row, "name", "Name", messages);
            var departmentCode = NormalizeCode(GetValue(row, "department_code"));
            Guid? departmentId = null;
            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                if (!departmentMap.TryGetValue(departmentCode, out var resolvedDepartmentId))
                {
                    messages.Add($"Department code '{departmentCode}' was not found.");
                }
                else
                {
                    departmentId = resolvedDepartmentId;
                }
            }

            codeMap.TryGetValue(code, out var existingIdValue);
            Guid? existingId = existingIdValue == Guid.Empty ? null : existingIdValue;

            if (!existingId.HasValue && !string.IsNullOrWhiteSpace(name) && nameMap.TryGetValue(name, out _))
            {
                messages.Add($"A position named '{name}' already exists.");
            }

            var request = new SavePositionRequestDto
            {
                Code = code,
                Name = name,
                Description = GetValue(row, "description"),
                DepartmentId = departmentId,
                IsActive = ParseBoolean(row, "is_active", true, messages)
            };

            ValidateModel(request, messages);
            var operation = existingId.HasValue ? "update" : "create";
            var previewRow = BuildPreviewRow(row, operation, code, messages);
            rows.Add(previewRow);

            if (messages.Count == 0)
            {
                actions.Add(new PreparedImportAction(
                    operation,
                    previewRow,
                    async token =>
                    {
                        if (!existingId.HasValue)
                        {
                            await _organizationSetupService.CreatePositionAsync(request, token);
                        }
                        else
                        {
                            await _organizationSetupService.UpdatePositionAsync(existingId.Value, request, token);
                        }
                    }));
            }
        }

        return new ImportBatch(definition.Key, definition.Name, file.FileName, GetDisplayColumns(definition), rows, actions);
    }

    private async Task<ImportBatch> BuildBranchBatchAsync(DataImportDefinitionDto definition, IFormFile file, ParsedCsvFile csvFile, CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var existingRecords = await _dbContext.Branches.AsNoTracking()
            .Select(record => new { record.Id, record.Code, record.Name })
            .ToListAsync(cancellationToken);

        var codeMap = existingRecords.ToDictionary(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var nameMap = existingRecords.ToDictionary(record => record.Name, record => record.Id, StringComparer.OrdinalIgnoreCase);

        return BuildOrganizationBatch(
            definition,
            file,
            csvFile,
            codeMap,
            nameMap,
            (row, isActive) => new SaveBranchRequestDto
            {
                Code = NormalizeCode(GetRequiredText(row, "code", "Code", [])),
                Name = GetValue(row, "name").Trim(),
                Description = GetValue(row, "description"),
                Address = GetValue(row, "address"),
                IsActive = isActive
            },
            async (request, existingId, token) =>
            {
                if (existingId is null)
                {
                    await _organizationSetupService.CreateBranchAsync(request, token);
                }
                else
                {
                    await _organizationSetupService.UpdateBranchAsync(existingId.Value, request, token);
                }
            },
            cancellationToken);
    }

    private async Task<ImportBatch> BuildEmploymentTypeBatchAsync(DataImportDefinitionDto definition, IFormFile file, ParsedCsvFile csvFile, CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var existingRecords = await _dbContext.EmploymentTypes.AsNoTracking()
            .Select(record => new { record.Id, record.Code, record.Name })
            .ToListAsync(cancellationToken);

        var codeMap = existingRecords.ToDictionary(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var nameMap = existingRecords.ToDictionary(record => record.Name, record => record.Id, StringComparer.OrdinalIgnoreCase);

        return BuildOrganizationBatch(
            definition,
            file,
            csvFile,
            codeMap,
            nameMap,
            (row, isActive) => new SaveEmploymentTypeRequestDto
            {
                Code = NormalizeCode(GetRequiredText(row, "code", "Code", [])),
                Name = GetValue(row, "name").Trim(),
                Description = GetValue(row, "description"),
                IsActive = isActive
            },
            async (request, existingId, token) =>
            {
                if (existingId is null)
                {
                    await _organizationSetupService.CreateEmploymentTypeAsync(request, token);
                }
                else
                {
                    await _organizationSetupService.UpdateEmploymentTypeAsync(existingId.Value, request, token);
                }
            },
            cancellationToken);
    }

    private async Task<ImportBatch> BuildEmploymentStatusBatchAsync(DataImportDefinitionDto definition, IFormFile file, ParsedCsvFile csvFile, CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var existingRecords = await _dbContext.EmploymentStatuses.AsNoTracking()
            .Select(record => new { record.Id, record.Code, record.Name })
            .ToListAsync(cancellationToken);

        var codeMap = existingRecords.ToDictionary(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase);
        var nameMap = existingRecords.ToDictionary(record => record.Name, record => record.Id, StringComparer.OrdinalIgnoreCase);

        return BuildOrganizationBatch(
            definition,
            file,
            csvFile,
            codeMap,
            nameMap,
            (row, isActive) => new SaveEmploymentStatusRequestDto
            {
                Code = NormalizeCode(GetRequiredText(row, "code", "Code", [])),
                Name = GetValue(row, "name").Trim(),
                Description = GetValue(row, "description"),
                IsActive = isActive
            },
            async (request, existingId, token) =>
            {
                if (existingId is null)
                {
                    await _organizationSetupService.CreateEmploymentStatusAsync(request, token);
                }
                else
                {
                    await _organizationSetupService.UpdateEmploymentStatusAsync(existingId.Value, request, token);
                }
            },
            cancellationToken);
    }

    private async Task<ImportBatch> BuildLeaveBalanceBatchAsync(
        DataImportDefinitionDto definition,
        IFormFile file,
        ParsedCsvFile csvFile,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var employeeMap = await _dbContext.Employees.AsNoTracking()
            .ToDictionaryAsync(record => record.EmployeeCode, record => record.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var leaveTypeMap = await _dbContext.LeaveTypes.AsNoTracking()
            .ToDictionaryAsync(record => record.Code, record => record.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingBalances = await _dbContext.EmployeeLeaveBalances.AsNoTracking()
            .Select(record => new { record.Id, record.EmployeeId, record.LeaveTypeId, record.PeriodYear })
            .ToListAsync(cancellationToken);

        var balanceMap = existingBalances.ToDictionary(
            record => BuildCompositeKey(record.EmployeeId, record.LeaveTypeId, record.PeriodYear),
            record => record.Id,
            StringComparer.OrdinalIgnoreCase);

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<DataImportPreviewRowDto>();
        var actions = new List<PreparedImportAction>();

        foreach (var row in csvFile.Rows)
        {
            var messages = new List<string>();
            var employeeCode = NormalizeCode(GetRequiredText(row, "employee_code", "Employee code", messages));
            var leaveTypeCode = NormalizeCode(GetRequiredText(row, "leave_type_code", "Leave type code", messages));
            var periodYear = ParseInt(row, "period_year", messages, isRequired: true);
            var identifier = string.Join(" / ", new[] { employeeCode, leaveTypeCode, periodYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty }.Where(part => !string.IsNullOrWhiteSpace(part)));
            var dedupeKey = $"{employeeCode}|{leaveTypeCode}|{periodYear}";

            if (!string.IsNullOrWhiteSpace(employeeCode) && !string.IsNullOrWhiteSpace(leaveTypeCode) && periodYear is not null && !seenKeys.Add(dedupeKey))
            {
                messages.Add("This employee, leave type, and year combination appears more than once in the file.");
            }

            employeeMap.TryGetValue(employeeCode, out var employeeId);
            if (employeeId == Guid.Empty)
            {
                messages.Add($"Employee code '{employeeCode}' was not found.");
            }

            leaveTypeMap.TryGetValue(leaveTypeCode, out var leaveTypeId);
            if (leaveTypeId == Guid.Empty)
            {
                messages.Add($"Leave type code '{leaveTypeCode}' was not found.");
            }

            var openingBalance = ParseDecimal(row, "opening_balance", messages);
            var accrued = ParseDecimal(row, "accrued", messages);
            var used = ParseDecimal(row, "used", messages);
            var pending = ParseDecimal(row, "pending", messages);
            var adjusted = ParseDecimal(row, "adjusted", messages, allowNegative: true);
            var carriedForward = ParseDecimal(row, "carried_forward", messages);

            if (openingBalance < 0m)
            {
                messages.Add("Opening balance cannot be negative.");
            }

            if (accrued < 0m || used < 0m || pending < 0m || carriedForward < 0m)
            {
                messages.Add("Accrued, used, pending, and carried-forward balances cannot be negative.");
            }

            var operation = balanceMap.ContainsKey(BuildCompositeKey(employeeId, leaveTypeId, periodYear ?? 0)) ? "update" : "create";
            var previewRow = BuildPreviewRow(row, operation, identifier, messages);
            rows.Add(previewRow);

            if (messages.Count == 0)
            {
                var year = periodYear!.Value;
                var resolvedEmployeeId = employeeId;
                var resolvedLeaveTypeId = leaveTypeId;
                actions.Add(new PreparedImportAction(
                    operation,
                    previewRow,
                    async token =>
                    {
                        var now = DateTime.UtcNow;
                        var record = await _dbContext.EmployeeLeaveBalances
                            .SingleOrDefaultAsync(
                                item => item.EmployeeId == resolvedEmployeeId && item.LeaveTypeId == resolvedLeaveTypeId && item.PeriodYear == year,
                                token);

                        var balanceBefore = record?.AvailableBalance ?? 0m;
                        if (record is null)
                        {
                            record = new EmployeeLeaveBalance
                            {
                                EmployeeId = resolvedEmployeeId,
                                LeaveTypeId = resolvedLeaveTypeId,
                                PeriodYear = year,
                                CreatedAtUtc = now
                            };

                            _dbContext.EmployeeLeaveBalances.Add(record);
                        }
                        else
                        {
                            record.UpdatedAtUtc = now;
                        }

                        record.OpeningBalance = openingBalance;
                        record.Accrued = accrued;
                        record.Used = used;
                        record.Pending = pending;
                        record.Adjusted = adjusted;
                        record.CarriedForward = carriedForward;
                        record.AvailableBalance = openingBalance + accrued + adjusted + carriedForward - used - pending;

                        if (record.AvailableBalance != balanceBefore)
                        {
                            _dbContext.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
                            {
                                EmployeeId = resolvedEmployeeId,
                                LeaveTypeId = resolvedLeaveTypeId,
                                PeriodYear = year,
                                TransactionType = LeaveBalanceTransactionTypes.Adjustment,
                                Amount = record.AvailableBalance - balanceBefore,
                                BalanceBefore = balanceBefore,
                                BalanceAfter = record.AvailableBalance,
                                Remarks = "Imported leave balance snapshot from CSV.",
                                CreatedByUserId = NormalizeUserId(actorUserId),
                                CreatedAtUtc = now
                            });
                        }

                        await _dbContext.SaveChangesAsync(token);
                    }));
            }
        }

        return new ImportBatch(definition.Key, definition.Name, file.FileName, GetDisplayColumns(definition), rows, actions);
    }

    private async Task<ImportBatch> BuildCompensationProfileBatchAsync(
        DataImportDefinitionDto definition,
        IFormFile file,
        ParsedCsvFile csvFile,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        EnsureHeaders(definition, csvFile.Headers);
        var employeeMap = await _dbContext.Employees.AsNoTracking()
            .ToDictionaryAsync(record => record.EmployeeCode, record => record.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingProfiles = await _dbContext.CompensationProfiles.AsNoTracking()
            .Select(record => new
            {
                record.Id,
                record.EmployeeId,
                record.EffectiveStartDate,
                record.EffectiveEndDate,
                record.IsActive
            })
            .ToListAsync(cancellationToken);

        var profileMap = existingProfiles.ToDictionary(
            record => BuildCompositeKey(record.EmployeeId, record.EffectiveStartDate),
            record => record.Id,
            StringComparer.OrdinalIgnoreCase);

        var profilesByEmployee = existingProfiles
            .GroupBy(record => record.EmployeeId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<DataImportPreviewRowDto>();
        var actions = new List<PreparedImportAction>();

        foreach (var row in csvFile.Rows)
        {
            var messages = new List<string>();
            var employeeCode = NormalizeCode(GetRequiredText(row, "employee_code", "Employee code", messages));
            var effectiveStartDate = ParseDate(row, "effective_start_date", messages, isRequired: true);
            var identifier = string.Join(" / ", new[] { employeeCode, effectiveStartDate?.ToString("yyyy-MM-dd") ?? string.Empty }.Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(employeeCode) && effectiveStartDate is not null && !seenKeys.Add($"{employeeCode}|{effectiveStartDate:yyyy-MM-dd}"))
            {
                messages.Add("This employee and effective start date combination appears more than once in the file.");
            }

            employeeMap.TryGetValue(employeeCode, out var employeeId);
            if (employeeId == Guid.Empty)
            {
                messages.Add($"Employee code '{employeeCode}' was not found.");
            }

            var request = new SaveCompensationProfileRequestDto
            {
                EmployeeId = employeeId == Guid.Empty ? null : employeeId,
                PayType = GetRequiredText(row, "pay_type", "Pay type", messages).Trim().ToLowerInvariant(),
                PayFrequency = GetRequiredText(row, "pay_frequency", "Pay frequency", messages).Trim().ToLowerInvariant(),
                BasicSalary = ParseDecimal(row, "basic_salary", messages),
                DailyRate = ParseOptionalDecimal(row, "daily_rate", messages),
                HourlyRate = ParseOptionalDecimal(row, "hourly_rate", messages),
                Currency = string.IsNullOrWhiteSpace(GetValue(row, "currency")) ? "PHP" : GetValue(row, "currency").Trim().ToUpperInvariant(),
                EffectiveStartDate = effectiveStartDate,
                EffectiveEndDate = ParseDate(row, "effective_end_date", messages),
                IsActive = ParseBoolean(row, "is_active", defaultValue: true, messages),
                Remarks = GetValue(row, "remarks")
            };

            ValidateModel(request, messages);

            Guid? existingId = null;
            if (employeeId != Guid.Empty && effectiveStartDate is not null)
            {
                profileMap.TryGetValue(BuildCompositeKey(employeeId, effectiveStartDate.Value), out var resolvedExistingId);
                existingId = resolvedExistingId == Guid.Empty ? null : resolvedExistingId;

                if (request.IsActive)
                {
                    var overlapExists = profilesByEmployee.GetValueOrDefault(employeeId, [])
                        .Any(profile =>
                            profile.IsActive &&
                            profile.Id != existingId &&
                            profile.EffectiveStartDate <= (request.EffectiveEndDate ?? DateOnly.MaxValue) &&
                            (profile.EffectiveEndDate ?? DateOnly.MaxValue) >= request.EffectiveStartDate);

                    if (overlapExists)
                    {
                        messages.Add("This employee already has an overlapping active compensation profile for the selected date range.");
                    }
                }
            }

            var operation = existingId is null ? "create" : "update";
            var previewRow = BuildPreviewRow(row, operation, identifier, messages);
            rows.Add(previewRow);

            if (messages.Count == 0)
            {
                actions.Add(new PreparedImportAction(
                    operation,
                    previewRow,
                    async token =>
                    {
                        if (existingId is null)
                        {
                            await _payrollCompensationService.CreateCompensationProfileAsync(request, actorUserId, token);
                        }
                        else
                        {
                            await _payrollCompensationService.UpdateCompensationProfileAsync(existingId.Value, request, actorUserId, token);
                        }
                    }));
            }
        }

        return new ImportBatch(definition.Key, definition.Name, file.FileName, GetDisplayColumns(definition), rows, actions);
    }

    private ImportBatch BuildOrganizationBatch<TRequest>(
        DataImportDefinitionDto definition,
        IFormFile file,
        ParsedCsvFile csvFile,
        IReadOnlyDictionary<string, Guid> codeMap,
        IReadOnlyDictionary<string, Guid> nameMap,
        Func<ParsedCsvRow, bool, TRequest> buildRequest,
        Func<TRequest, Guid?, CancellationToken, Task> applyAsync,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<DataImportPreviewRowDto>();
        var actions = new List<PreparedImportAction>();

        foreach (var row in csvFile.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = new List<string>();
            var code = NormalizeCode(GetValue(row, "code"));
            var name = GetValue(row, "name").Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                messages.Add("Code is required.");
            }
            else if (!seenCodes.Add(code))
            {
                messages.Add($"Code '{code}' appears more than once in this file.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                messages.Add("Name is required.");
            }

            codeMap.TryGetValue(code, out var existingIdValue);
            Guid? existingId = existingIdValue == Guid.Empty ? null : existingIdValue;

            if (!string.IsNullOrWhiteSpace(name) &&
                nameMap.TryGetValue(name, out var nameOwnerId) &&
                (!existingId.HasValue || nameOwnerId != existingId.Value))
            {
                messages.Add($"A record named '{name}' already exists.");
            }

            var isActive = ParseBoolean(row, "is_active", defaultValue: true, messages);
            var request = buildRequest(row, isActive);

            ValidateModel(request, messages);

            var operation = existingId is null ? "create" : "update";
            var previewRow = BuildPreviewRow(row, operation, code, messages);
            rows.Add(previewRow);

            if (messages.Count == 0)
            {
                actions.Add(new PreparedImportAction(operation, previewRow, token => applyAsync(request, existingId, token)));
            }
        }

        return new ImportBatch(definition.Key, definition.Name, file.FileName, GetDisplayColumns(definition), rows, actions);
    }

    private static void ValidateImportFile(IFormFile? file)
    {
        if (file is null)
        {
            throw new BadRequestException("An import file is required.");
        }

        if (file.Length <= 0)
        {
            throw new BadRequestException("The selected import file is empty.");
        }

        if (file.Length > MaxImportFileBytes)
        {
            throw new BadRequestException($"The selected import file exceeds the {MaxImportFileBytes / (1024 * 1024)} MB limit.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!SupportedImportExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Only CSV import files are supported in this baseline.");
        }
    }

    private static void EnsureHeaders(DataImportDefinitionDto definition, IReadOnlyList<string> headers)
    {
        var headerSet = headers
            .Select(NormalizeHeader)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingHeaders = definition.RequiredColumns
            .Where(column => !headerSet.Contains(NormalizeHeader(column)))
            .ToArray();

        if (missingHeaders.Length > 0)
        {
            throw new BadRequestException($"The import file is missing required column(s): {string.Join(", ", missingHeaders)}.");
        }
    }

    private static ParsedCsvFile ReadCsvAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        using var parser = new TextFieldParser(stream, Encoding.UTF8)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };

        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            throw new BadRequestException("The CSV file does not contain a header row.");
        }

        string[]? rawHeaders;
        try
        {
            rawHeaders = parser.ReadFields();
        }
        catch (MalformedLineException exception)
        {
            throw new BadRequestException($"The CSV header row could not be parsed: {exception.Message}");
        }

        if (rawHeaders is null || rawHeaders.Length == 0)
        {
            throw new BadRequestException("The CSV file does not contain any headers.");
        }

        var normalizedHeaders = rawHeaders
            .Select(header => NormalizeHeader(header))
            .ToArray();

        if (normalizedHeaders.Any(string.IsNullOrWhiteSpace))
        {
            throw new BadRequestException("The CSV header row contains an empty column name.");
        }

        if (normalizedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedHeaders.Length)
        {
            throw new BadRequestException("The CSV header row contains duplicate column names.");
        }

        var rows = new List<ParsedCsvRow>();
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string[]? fields;
            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException exception)
            {
                throw new BadRequestException($"The CSV file contains a malformed row: {exception.Message}");
            }

            if (fields is null || fields.All(field => string.IsNullOrWhiteSpace(field)))
            {
                continue;
            }

            if (rows.Count >= MaxImportRows)
            {
                throw new BadRequestException($"The CSV file exceeds the maximum supported row count of {MaxImportRows}.");
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < normalizedHeaders.Length; index += 1)
            {
                values[normalizedHeaders[index]] = index < fields.Length ? fields[index]?.Trim() ?? string.Empty : string.Empty;
            }

            rows.Add(new ParsedCsvRow(rows.Count + 2, values));
        }

        return new ParsedCsvFile(rawHeaders, rows);
    }

    private static string GetValue(ParsedCsvRow row, string columnName)
    {
        return row.Values.GetValueOrDefault(NormalizeHeader(columnName), string.Empty);
    }

    private static string GetRequiredText(ParsedCsvRow row, string columnName, string label, ICollection<string> messages)
    {
        var value = GetValue(row, columnName).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            messages.Add($"{label} is required.");
        }

        return value;
    }

    private static bool ParseBoolean(ParsedCsvRow row, string columnName, bool defaultValue, ICollection<string> messages)
    {
        var value = GetValue(row, columnName).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "y" or "active" => true,
            "0" or "false" or "no" or "n" or "inactive" => false,
            _ => AddInvalidBoolean(messages, columnName, defaultValue)
        };
    }

    private static bool AddInvalidBoolean(ICollection<string> messages, string columnName, bool defaultValue)
    {
        messages.Add($"Column '{columnName}' must be yes/no, true/false, active/inactive, or 1/0.");
        return defaultValue;
    }

    private static DateOnly? ParseDate(ParsedCsvRow row, string columnName, ICollection<string> messages, bool isRequired = false)
    {
        var value = GetValue(row, columnName).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            if (isRequired)
            {
                messages.Add($"Column '{columnName}' is required.");
            }

            return null;
        }

        if (DateOnly.TryParseExact(value, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
            DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed;
        }

        messages.Add($"Column '{columnName}' must contain a valid date.");
        return null;
    }

    private static int? ParseInt(ParsedCsvRow row, string columnName, ICollection<string> messages, bool isRequired = false)
    {
        var value = GetValue(row, columnName).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            if (isRequired)
            {
                messages.Add($"Column '{columnName}' is required.");
            }

            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        messages.Add($"Column '{columnName}' must contain a whole number.");
        return null;
    }

    private static decimal ParseDecimal(ParsedCsvRow row, string columnName, ICollection<string> messages, bool allowNegative = false)
    {
        var value = GetValue(row, columnName).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            if (!allowNegative && parsed < 0m)
            {
                messages.Add($"Column '{columnName}' cannot be negative.");
                return 0m;
            }

            return parsed;
        }

        messages.Add($"Column '{columnName}' must contain a valid amount.");
        return 0m;
    }

    private static decimal? ParseOptionalDecimal(ParsedCsvRow row, string columnName, ICollection<string> messages)
    {
        var value = GetValue(row, columnName).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        messages.Add($"Column '{columnName}' must contain a valid amount.");
        return null;
    }

    private static TEntity? ResolveLookup<TEntity>(
        IReadOnlyDictionary<string, TEntity> map,
        string code,
        string entityLabel,
        string columnName,
        ICollection<string> messages)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        if (map.TryGetValue(code, out var record))
        {
            return record;
        }

        messages.Add($"The {entityLabel} referenced by '{columnName}' was not found.");
        return null;
    }

    private static void ValidateModel(object request, ICollection<string> messages)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            foreach (var result in results.Where(result => !string.IsNullOrWhiteSpace(result.ErrorMessage)))
            {
                messages.Add(result.ErrorMessage!);
            }
        }
    }

    private static DataImportPreviewRowDto BuildPreviewRow(ParsedCsvRow row, string operation, string identifier, IReadOnlyList<string> messages)
    {
        return new DataImportPreviewRowDto
        {
            RowNumber = row.RowNumber,
            Identifier = string.IsNullOrWhiteSpace(identifier) ? $"Row {row.RowNumber}" : identifier,
            Operation = operation,
            Status = messages.Count == 0 ? "valid" : "invalid",
            Messages = messages.ToArray(),
            Values = new Dictionary<string, string>(row.Values, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static ProductionReadinessItemDto BuildStatusItem(string key, string label, string status, string detail, string actionUrl)
    {
        return new ProductionReadinessItemDto
        {
            Key = key,
            Label = label,
            Status = status,
            Detail = detail,
            ActionUrl = actionUrl
        };
    }

    private static IReadOnlyList<DataImportDefinitionDto> GetImportDefinitions()
    {
        return
        [
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.Employees,
                Name = "Employee import",
                Description = "Create or update employee master profiles from a CSV file. Save Excel workbooks as CSV before uploading.",
                SampleFileName = "employees.csv",
                RequiredColumns =
                [
                    "employee_code",
                    "first_name",
                    "last_name",
                    "gender",
                    "department_code",
                    "position_code",
                    "branch_code",
                    "employment_type_code",
                    "employment_status_code",
                    "date_hired"
                ],
                OptionalColumns =
                [
                    "middle_name",
                    "suffix",
                    "birth_date",
                    "civil_status",
                    "nationality",
                    "mobile_number",
                    "email",
                    "address",
                    "city_province",
                    "postal_code",
                    "emergency_contact_name",
                    "emergency_contact_relationship",
                    "emergency_contact_phone",
                    "manager_employee_code",
                    "work_schedule",
                    "date_regularized",
                    "date_separated",
                    "sss_number",
                    "philhealth_number",
                    "pagibig_number",
                    "tin_number",
                    "other_government_id",
                    "user_email",
                    "is_active"
                ]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.Departments,
                Name = "Department import",
                Description = "Create or update departments by code.",
                SampleFileName = "departments.csv",
                RequiredColumns = ["code", "name"],
                OptionalColumns = ["description", "is_active"]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.Positions,
                Name = "Position import",
                Description = "Create or update positions with an optional department reference.",
                SampleFileName = "positions.csv",
                RequiredColumns = ["code", "name"],
                OptionalColumns = ["description", "department_code", "is_active"]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.Branches,
                Name = "Branch import",
                Description = "Create or update branches and locations.",
                SampleFileName = "branches.csv",
                RequiredColumns = ["code", "name"],
                OptionalColumns = ["description", "address", "is_active"]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.EmploymentTypes,
                Name = "Employment type import",
                Description = "Create or update employment type reference records.",
                SampleFileName = "employment-types.csv",
                RequiredColumns = ["code", "name"],
                OptionalColumns = ["description", "is_active"]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.EmploymentStatuses,
                Name = "Employment status import",
                Description = "Create or update employment status reference records.",
                SampleFileName = "employment-statuses.csv",
                RequiredColumns = ["code", "name"],
                OptionalColumns = ["description", "is_active"]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.LeaveBalances,
                Name = "Leave balance import",
                Description = "Upsert employee leave balances for a specific year and keep a balance-ledger trace.",
                SampleFileName = "leave-balances.csv",
                RequiredColumns = ["employee_code", "leave_type_code", "period_year"],
                OptionalColumns = ["opening_balance", "accrued", "used", "pending", "adjusted", "carried_forward"]
            },
            new DataImportDefinitionDto
            {
                Key = DataImportTypes.CompensationProfiles,
                Name = "Compensation profile import",
                Description = "Create or update compensation profiles using employee code and effective start date.",
                SampleFileName = "compensation-profiles.csv",
                RequiredColumns = ["employee_code", "pay_type", "pay_frequency", "effective_start_date"],
                OptionalColumns = ["basic_salary", "daily_rate", "hourly_rate", "currency", "effective_end_date", "is_active", "remarks"]
            }
        ];
    }

    private static DataImportDefinitionDto GetImportDefinition(string importType)
    {
        return GetImportDefinitions()
            .SingleOrDefault(definition => string.Equals(definition.Key, importType?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException($"Import type '{importType}' is not supported.");
    }

    private static IReadOnlyList<string> GetDisplayColumns(DataImportDefinitionDto definition)
    {
        return definition.RequiredColumns.Concat(definition.OptionalColumns).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string NormalizeHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer.Append(char.ToLowerInvariant(character));
            }
        }

        return buffer.ToString();
    }

    private static string NormalizeCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string BuildCompositeKey(Guid firstId, Guid secondId, int number)
    {
        return $"{firstId:N}|{secondId:N}|{number}";
    }

    private static string BuildCompositeKey(Guid firstId, DateOnly date)
    {
        return $"{firstId:N}|{date:yyyy-MM-dd}";
    }

    private static string? NormalizeUserId(string? actorUserId)
    {
        return string.IsNullOrWhiteSpace(actorUserId) ? null : actorUserId.Trim();
    }

    private sealed record ParsedCsvFile(
        IReadOnlyList<string> Headers,
        IReadOnlyList<ParsedCsvRow> Rows);

    private sealed record ParsedCsvRow(
        int RowNumber,
        IReadOnlyDictionary<string, string> Values);

    private sealed class PreparedImportAction
    {
        public PreparedImportAction(string operation, DataImportPreviewRowDto previewRow, Func<CancellationToken, Task> applyAsync)
        {
            Operation = operation;
            PreviewRow = previewRow;
            ApplyAsync = applyAsync;
        }

        public string Operation { get; }

        public DataImportPreviewRowDto PreviewRow { get; }

        public Func<CancellationToken, Task> ApplyAsync { get; }
    }

    private sealed class ImportBatch
    {
        public ImportBatch(
            string importType,
            string importName,
            string fileName,
            IReadOnlyList<string> columns,
            IReadOnlyList<DataImportPreviewRowDto> rows,
            IReadOnlyList<PreparedImportAction> actions)
        {
            ImportType = importType;
            ImportName = importName;
            FileName = fileName;
            Columns = columns;
            Rows = rows;
            Actions = actions;
        }

        public string ImportType { get; }

        public string ImportName { get; }

        public string FileName { get; }

        public IReadOnlyList<string> Columns { get; }

        public IReadOnlyList<DataImportPreviewRowDto> Rows { get; }

        public IReadOnlyList<PreparedImportAction> Actions { get; }

        public int InvalidRowCount => Rows.Count(row => row.Status == "invalid");

        public int ValidRowCount => Rows.Count - InvalidRowCount;

        public int CreatedCount => Actions.Count(action => action.Operation == "create");

        public int UpdatedCount => Actions.Count(action => action.Operation == "update");

        public DataImportPreviewDto ToPreview()
        {
            return new DataImportPreviewDto
            {
                ImportType = ImportType,
                ImportName = ImportName,
                FileName = FileName,
                TotalRows = Rows.Count,
                ValidRowCount = ValidRowCount,
                InvalidRowCount = InvalidRowCount,
                CanApply = InvalidRowCount == 0 && Actions.Count > 0,
                Columns = Columns,
                Rows = Rows
            };
        }
    }
}
