using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "AddItem accumulates total across lines")]
    public void Given_Sale_When_AddingMultipleItems_Then_TotalSumsNonCancelledLines()
    {
        var sale = SaleTestData.GenerateValidSale(itemQuantity: 2, unitPrice: 10m);  // total 20
        sale.AddItem(Guid.NewGuid(), "X", quantity: 5, unitPrice: 10m);               // +45 (10% off)

        sale.Items.Should().HaveCount(2);
        sale.TotalAmount.Should().Be(20m + 45m);
    }

    [Fact(DisplayName = "Cancelling a sale flips the flag and raises SaleCancelledEvent")]
    public void Given_ActiveSale_When_Cancelled_Then_FlagSetAndEventRaised()
    {
        var sale = SaleTestData.GenerateValidSale();

        sale.Cancel();

        sale.IsCancelled.Should().BeTrue();
        sale.DomainEvents.OfType<SaleCancelledEvent>().Should().ContainSingle()
            .Which.SaleId.Should().Be(sale.Id);
    }

    [Fact(DisplayName = "Cancelling an already-cancelled sale throws")]
    public void Given_CancelledSale_When_CancelledAgain_Then_Throws()
    {
        var sale = SaleTestData.GenerateValidSale();
        sale.Cancel();

        Action act = () => sale.Cancel();

        act.Should().Throw<DomainException>().WithMessage("*already cancelled*");
    }

    [Fact(DisplayName = "CancelItem excludes the line from total and raises ItemCancelledEvent")]
    public void Given_SaleWithItems_When_CancellingOneItem_Then_TotalExcludesItAndEventRaised()
    {
        var sale = SaleTestData.GenerateValidSale(itemQuantity: 2, unitPrice: 10m);   // 20
        var extra = sale.AddItem(Guid.NewGuid(), "X", 5, 10m);                         // +45 → 65
        sale.ClearDomainEvents();

        sale.CancelItem(extra.Id);

        sale.TotalAmount.Should().Be(20m);
        extra.IsCancelled.Should().BeTrue();
        sale.DomainEvents.OfType<ItemCancelledEvent>().Should().ContainSingle()
            .Which.ItemId.Should().Be(extra.Id);
    }

    [Fact(DisplayName = "CancelItem on a non-existent line throws")]
    public void Given_Sale_When_CancellingUnknownItem_Then_Throws()
    {
        var sale = SaleTestData.GenerateValidSale();

        Action act = () => sale.CancelItem(Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*not found*");
    }

    [Fact(DisplayName = "Modifying a cancelled sale throws")]
    public void Given_CancelledSale_When_AddingItem_Then_Throws()
    {
        var sale = SaleTestData.GenerateValidSale();
        sale.Cancel();

        Action act = () => sale.AddItem(Guid.NewGuid(), "X", 1, 5m);

        act.Should().Throw<DomainException>().WithMessage("*cancelled*");
    }

    [Fact(DisplayName = "ReplaceItems swaps the line set and raises SaleModifiedEvent")]
    public void Given_Sale_When_ItemsReplaced_Then_NewTotalAndEventRaised()
    {
        var sale = SaleTestData.GenerateValidSale(itemQuantity: 2, unitPrice: 10m);
        sale.ClearDomainEvents();

        sale.ReplaceItems(new[]
        {
            (Guid.NewGuid(), "A", 10, 5m),   // 10 × 5 × 0.8 = 40
            (Guid.NewGuid(), "B",  1, 7m),   // 7
        });

        sale.Items.Should().HaveCount(2);
        sale.TotalAmount.Should().Be(47m);
        sale.DomainEvents.OfType<SaleModifiedEvent>().Should().ContainSingle();
    }

    [Fact(DisplayName = "MarkCreated raises SaleCreatedEvent with the current total")]
    public void Given_Sale_When_MarkedCreated_Then_EventCarriesTotal()
    {
        var sale = SaleTestData.GenerateValidSale(itemQuantity: 4, unitPrice: 10m);   // 4 × 10 × 0.9 = 36

        sale.MarkCreated();

        var evt = sale.DomainEvents.OfType<SaleCreatedEvent>().Should().ContainSingle().Subject;
        evt.SaleId.Should().Be(sale.Id);
        evt.TotalAmount.Should().Be(36m);
    }

    [Fact(DisplayName = "Validation passes for a well-formed sale")]
    public void Given_ValidSale_When_Validated_Then_NoErrors()
    {
        var sale = SaleTestData.GenerateValidSale();

        var result = sale.Validate();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validation fails when the sale has no items")]
    public void Given_EmptySale_When_Validated_Then_FailsOnItemsRule()
    {
        var sale = new Sale
        {
            SaleNumber = "S-0001",
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "C",
            BranchId = Guid.NewGuid(),
            BranchName = "B"
        };

        var result = sale.Validate();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Detail.Contains("at least one item"));
    }
}
