using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleItemTests
{
    [Theory(DisplayName = "Quantity below 4 yields no discount")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Given_QuantityBelowTier1_When_Created_Then_DiscountIsZero(int quantity)
    {
        var item = SaleTestData.GenerateItem(quantity, 10m);

        item.Discount.Should().Be(0m);
        item.TotalAmount.Should().Be(quantity * 10m);
    }

    [Theory(DisplayName = "Quantity 4–9 yields a 10% discount")]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(9)]
    public void Given_QuantityInTier1_When_Created_Then_TenPercentDiscountApplies(int quantity)
    {
        var item = SaleTestData.GenerateItem(quantity, 10m);

        item.Discount.Should().Be(0.10m);
        item.TotalAmount.Should().Be(quantity * 10m * 0.90m);
    }

    [Theory(DisplayName = "Quantity 10–20 yields a 20% discount")]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void Given_QuantityInTier2_When_Created_Then_TwentyPercentDiscountApplies(int quantity)
    {
        var item = SaleTestData.GenerateItem(quantity, 10m);

        item.Discount.Should().Be(0.20m);
        item.TotalAmount.Should().Be(quantity * 10m * 0.80m);
    }

    [Theory(DisplayName = "Quantity above 20 is rejected by the domain")]
    [InlineData(21)]
    [InlineData(100)]
    public void Given_QuantityAboveCap_When_Created_Then_ThrowsDomainException(int quantity)
    {
        Action act = () => SaleTestData.GenerateItem(quantity, 10m);

        act.Should().Throw<DomainException>()
           .WithMessage($"*{SaleItem.MaxQuantityPerItem}*");
    }

    [Fact(DisplayName = "Cancelling a line zeroes its total")]
    public void Given_ActiveItem_When_Cancelled_Then_TotalIsZeroAndFlagIsSet()
    {
        var item = SaleTestData.GenerateItem(5, 10m);

        item.Cancel();

        item.IsCancelled.Should().BeTrue();
        item.TotalAmount.Should().Be(0m);
    }

    [Fact(DisplayName = "Total is rounded to two decimals to avoid floating drift")]
    public void Given_PriceWithManyDecimals_When_Created_Then_TotalIsRoundedToCents()
    {
        var item = SaleTestData.GenerateItem(4, 3.333m);

        // 4 × 3.333 × 0.9 = 11.9988 → rounded to 12.00
        item.TotalAmount.Should().Be(12.00m);
    }
}
