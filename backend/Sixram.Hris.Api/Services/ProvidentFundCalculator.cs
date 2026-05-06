using Sixram.Api.Constants;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public readonly record struct ProvidentFundVestingRuleStep(int YearsOfService, decimal VestedPercentage);

public sealed record ProvidentFundBalanceComputation(
    decimal GrossFundBalance,
    decimal VestedEmployerBalance,
    decimal NonVestedEmployerBalance,
    decimal WithdrawableBalance);

public static class ProvidentFundCalculator
{
    public static decimal CalculateContribution(decimal basicSalary, string contributionType, decimal contributionValue)
    {
        if (basicSalary < 0m)
        {
            throw new BadRequestException("Basic salary cannot be negative.");
        }

        if (contributionValue < 0m)
        {
            throw new BadRequestException("Contribution value cannot be negative.");
        }

        return NormalizeContributionType(contributionType) == ProvidentFundContributionTypes.Percentage
            ? RoundMoney(basicSalary * contributionValue / 100m)
            : RoundMoney(contributionValue);
    }

    public static decimal ResolveVestingPercentage(IEnumerable<ProvidentFundVestingRuleStep> rules, DateOnly vestingStartDate, DateOnly asOfDate)
    {
        var years = CalculateWholeYears(vestingStartDate, asOfDate);
        return rules
            .Where(rule => rule.YearsOfService <= years)
            .OrderByDescending(rule => rule.YearsOfService)
            .Select(rule => rule.VestedPercentage)
            .FirstOrDefault();
    }

    public static ProvidentFundBalanceComputation CalculateBalance(
        decimal employeeShare,
        decimal employerShare,
        decimal voluntaryShare,
        decimal interest,
        decimal withdrawals,
        decimal adjustments,
        decimal vestingPercentage)
    {
        if (vestingPercentage is < 0m or > 100m)
        {
            throw new BadRequestException("Vesting percentage must be between 0 and 100.");
        }

        var vestedEmployer = Math.Max(0m, RoundMoney(employerShare * vestingPercentage / 100m));
        var nonVestedEmployer = Math.Max(0m, RoundMoney(employerShare - vestedEmployer));
        var grossBalance = RoundMoney(employeeShare + employerShare + voluntaryShare + interest - withdrawals + adjustments);
        var withdrawable = Math.Max(0m, RoundMoney(employeeShare + voluntaryShare + interest + vestedEmployer - withdrawals + adjustments));

        return new ProvidentFundBalanceComputation(
            grossBalance,
            vestedEmployer,
            nonVestedEmployer,
            withdrawable);
    }

    public static int CalculateWholeYears(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return 0;
        }

        var years = end.Year - start.Year;
        if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day))
        {
            years -= 1;
        }

        return years;
    }

    public static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeContributionType(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        return normalized switch
        {
            "percentage" => ProvidentFundContributionTypes.Percentage,
            "fixed" or "fixed_amount" => ProvidentFundContributionTypes.FixedAmount,
            _ => throw new BadRequestException("Contribution type must be percentage or fixed amount.")
        };
    }
}
