namespace Sixram.Api.Constants;

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string HumanResources = "HR";
    public const string Manager = "Manager";
    public const string PayrollOfficer = "PayrollOfficer";
    public const string User = "User";

    public static readonly IReadOnlyList<string> Defaults =
    [
        Administrator,
        HumanResources,
        Manager,
        PayrollOfficer,
        User
    ];
}
