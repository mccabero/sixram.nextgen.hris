using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Sixram.Api.Configuration;
using Sixram.Api.Data;
using Sixram.Api.DTOs;
using Sixram.Api.Entities;
using Sixram.Api.Middleware;
using Sixram.Api.Repositories;
using Sixram.Api.Services;

const string defaultConnectionString = "Server=localhost\\SQLEXPRESS;Database=SixramDB;Trusted_Connection=True;TrustServerCertificate=True;";
const string defaultJwtSecret = "SixramDevelopmentOnlySuperSecretSigningKey1234567890";
const string defaultJwtIssuer = "Sixram.Hris.Api";
const string defaultJwtAudience = "Sixram.Hris.Web";
const string corsPolicyName = "FrontendDevelopment";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .PostConfigure(options =>
    {
        options.SecretKey = string.IsNullOrWhiteSpace(options.SecretKey) ? defaultJwtSecret : options.SecretKey;
        options.Issuer = string.IsNullOrWhiteSpace(options.Issuer) ? defaultJwtIssuer : options.Issuer;
        options.Audience = string.IsNullOrWhiteSpace(options.Audience) ? defaultJwtAudience : options.Audience;
        options.RefreshTokenCookieName = string.IsNullOrWhiteSpace(options.RefreshTokenCookieName)
            ? "sixram_refresh_token"
            : options.RefreshTokenCookieName;

        if (options.AccessTokenMinutes <= 0)
        {
            options.AccessTokenMinutes = 15;
        }

        if (options.RefreshTokenDays <= 0)
        {
            options.RefreshTokenDays = 7;
        }
    });

builder.Services
    .AddOptions<EmployeeDocumentOptions>()
    .Bind(builder.Configuration.GetSection(EmployeeDocumentOptions.SectionName))
    .PostConfigure(options =>
    {
        options.StorageRootPath = string.IsNullOrWhiteSpace(options.StorageRootPath)
            ? "App_Data/employee-documents"
            : options.StorageRootPath;

        if (options.MaxFileSizeMb <= 0)
        {
            options.MaxFileSizeMb = 10;
        }

        if (options.ExpiringSoonDays <= 0)
        {
            options.ExpiringSoonDays = 30;
        }

        if (options.AllowedExtensions is null || options.AllowedExtensions.Length == 0)
        {
            options.AllowedExtensions = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];
        }
    });

builder.Services
    .AddOptions<AttendanceOptions>()
    .Bind(builder.Configuration.GetSection(AttendanceOptions.SectionName))
    .PostConfigure(options =>
    {
        options.TimeZoneId = string.IsNullOrWhiteSpace(options.TimeZoneId)
            ? "Singapore Standard Time"
            : options.TimeZoneId;

        if (options.DashboardTrendDays <= 0)
        {
            options.DashboardTrendDays = 7;
        }

        if (options.MaxQueryRangeDays <= 0)
        {
            options.MaxQueryRangeDays = 31;
        }
    });

builder.Services
    .AddOptions<LeaveOptions>()
    .Bind(builder.Configuration.GetSection(LeaveOptions.SectionName))
    .PostConfigure(options =>
    {
        options.StorageRootPath = string.IsNullOrWhiteSpace(options.StorageRootPath)
            ? "App_Data/leave-attachments"
            : options.StorageRootPath;

        if (options.MaxAttachmentSizeMb <= 0)
        {
            options.MaxAttachmentSizeMb = 10;
        }

        if (options.ExpiringSoonDays <= 0)
        {
            options.ExpiringSoonDays = 30;
        }

        if (options.UpcomingWindowDays <= 0)
        {
            options.UpcomingWindowDays = 14;
        }

        if (options.LowBalanceThreshold < 0m)
        {
            options.LowBalanceThreshold = 0m;
        }

        if (options.AllowedExtensions is null || options.AllowedExtensions.Length == 0)
        {
            options.AllowedExtensions = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];
        }
    });

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.SecretKey = string.IsNullOrWhiteSpace(jwtOptions.SecretKey) ? defaultJwtSecret : jwtOptions.SecretKey;
jwtOptions.Issuer = string.IsNullOrWhiteSpace(jwtOptions.Issuer) ? defaultJwtIssuer : jwtOptions.Issuer;
jwtOptions.Audience = string.IsNullOrWhiteSpace(jwtOptions.Audience) ? defaultJwtAudience : jwtOptions.Audience;

if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || string.Equals(jwtOptions.SecretKey, defaultJwtSecret, StringComparison.Ordinal)))
{
    throw new InvalidOperationException("A non-default JWT secret key must be configured before starting Sixram HRIS API outside Development.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? defaultConnectionString;
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
var allowDevelopmentLoopbackOrigins = builder.Environment.IsDevelopment();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The provided value is invalid." : error.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = errors
        });
    };
});

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("The access token is missing the user identifier claim.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);

                if (user is null || !user.IsEnabled)
                {
                    context.Fail("The user account is disabled or no longer available.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        corsPolicyName,
        policy => policy
            .SetIsOriginAllowed(origin => IsAllowedCorsOrigin(origin, allowedOrigins, allowDevelopmentLoopbackOrigins))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sixram HRIS API",
        Version = "v1",
        Description = "Sixram HRIS API with Identity, JWT access tokens, refresh tokens, employee records, payroll, reporting, and RBAC management."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a bearer token generated by the login endpoint."
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
});

builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRbacReadRepository, RbacReadRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IOrganizationSetupService, OrganizationSetupService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IEmployeeDocumentStorageService, EmployeeDocumentStorageService>();
builder.Services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
builder.Services.AddScoped<IAttendanceSetupService, AttendanceSetupService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceAdjustmentService, AttendanceAdjustmentService>();
builder.Services.AddScoped<ILeaveAttachmentStorageService, LeaveAttachmentStorageService>();
builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IProfileChangeRequestService, ProfileChangeRequestService>();
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<IApprovalCenterService, ApprovalCenterService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IProductionReadinessService, ProductionReadinessService>();
builder.Services.AddScoped<IPayrollSetupService, PayrollSetupService>();
builder.Services.AddScoped<IPayrollCompensationService, PayrollCompensationService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IProvidentFundService, ProvidentFundService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var databaseSeeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await databaseSeeder.SeedAsync();
}

app.Run();

static bool IsAllowedCorsOrigin(string origin, IReadOnlyCollection<string> configuredOrigins, bool allowDevelopmentLoopbackOrigins)
{
    if (configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!allowDevelopmentLoopbackOrigins || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    var isLoopbackHost =
        uri.IsLoopback ||
        string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);

    var isHttpScheme =
        string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    return isLoopbackHost && isHttpScheme;
}
