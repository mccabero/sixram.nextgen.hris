using Sixram.Api.Constants;
using Sixram.Api.Exceptions;
using Sixram.Api.Services;

namespace Sixram.Hris.Api.Tests;

public sealed class ProvidentFundCalculatorTests
{
    [Fact]
    public void CalculateContribution_WhenPercentage_ReturnsSalaryPercentage()
    {
        var result = ProvidentFundCalculator.CalculateContribution(35_000m, ProvidentFundContributionTypes.Percentage, 5m);

        Assert.Equal(1_750m, result);
    }

    [Fact]
    public void CalculateContribution_WhenFixedAmount_RoundsToMoneyPrecision()
    {
        var result = ProvidentFundCalculator.CalculateContribution(35_000m, ProvidentFundContributionTypes.FixedAmount, 1_200.125m);

        Assert.Equal(1_200.13m, result);
    }

    [Fact]
    public void CalculateContribution_WhenNegativeSalary_ThrowsValidationError()
    {
        Assert.Throws<BadRequestException>(() =>
            ProvidentFundCalculator.CalculateContribution(-1m, ProvidentFundContributionTypes.Percentage, 5m));
    }

    [Fact]
    public void ResolveVestingPercentage_UsesHighestCompletedThreshold()
    {
        var rules = new[]
        {
            new ProvidentFundVestingRuleStep(0, 0m),
            new ProvidentFundVestingRuleStep(1, 20m),
            new ProvidentFundVestingRuleStep(2, 40m),
            new ProvidentFundVestingRuleStep(5, 100m)
        };

        var result = ProvidentFundCalculator.ResolveVestingPercentage(
            rules,
            new DateOnly(2022, 5, 7),
            new DateOnly(2026, 5, 6));

        Assert.Equal(40m, result);
    }

    [Fact]
    public void ResolveVestingPercentage_BeforeVestingAnniversary_DoesNotAdvanceThreshold()
    {
        var rules = new[]
        {
            new ProvidentFundVestingRuleStep(0, 0m),
            new ProvidentFundVestingRuleStep(1, 20m)
        };

        var result = ProvidentFundCalculator.ResolveVestingPercentage(
            rules,
            new DateOnly(2025, 5, 7),
            new DateOnly(2026, 5, 6));

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateBalance_ComputesGrossVestedAndWithdrawableAmounts()
    {
        var result = ProvidentFundCalculator.CalculateBalance(
            employeeShare: 10_000m,
            employerShare: 8_000m,
            voluntaryShare: 1_500m,
            interest: 250m,
            withdrawals: 2_000m,
            adjustments: 300m,
            vestingPercentage: 60m);

        Assert.Equal(18_050m, result.GrossFundBalance);
        Assert.Equal(4_800m, result.VestedEmployerBalance);
        Assert.Equal(3_200m, result.NonVestedEmployerBalance);
        Assert.Equal(14_850m, result.WithdrawableBalance);
    }

    [Fact]
    public void CalculateBalance_WhenWithdrawalsExceedWithdrawableShares_ClampsWithdrawableToZero()
    {
        var result = ProvidentFundCalculator.CalculateBalance(
            employeeShare: 500m,
            employerShare: 0m,
            voluntaryShare: 0m,
            interest: 0m,
            withdrawals: 2_000m,
            adjustments: 0m,
            vestingPercentage: 0m);

        Assert.Equal(0m, result.WithdrawableBalance);
    }
}
