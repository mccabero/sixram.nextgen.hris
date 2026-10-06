using Sixram.Api.Repositories;

namespace Sixram.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSixramApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRbacReadRepository, RbacReadRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IRbacService, RbacService>();
        services.AddScoped<IOrganizationSetupService, OrganizationSetupService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        services.AddScoped<IEmployeeDocumentStorageService, EmployeeDocumentStorageService>();
        services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
        services.AddScoped<IAttendanceSetupService, AttendanceSetupService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceAdjustmentService, AttendanceAdjustmentService>();
        services.AddScoped<ILeaveAttachmentStorageService, LeaveAttachmentStorageService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IProfileChangeRequestService, ProfileChangeRequestService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IApprovalCenterService, ApprovalCenterService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IProductionReadinessService, ProductionReadinessService>();
        services.AddScoped<IPayrollSetupService, PayrollSetupService>();
        services.AddScoped<IPayrollCompensationService, PayrollCompensationService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IProvidentFundService, ProvidentFundService>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        return services;
    }
}
