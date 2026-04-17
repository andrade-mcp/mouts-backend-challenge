using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>Bogus generators for Sale and SaleItem fixtures.</summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    /// <summary>A valid SaleItem with a caller-controlled quantity (needed for tier-specific tests).</summary>
    public static SaleItem GenerateItem(int quantity = 1, decimal unitPrice = 10m)
        => new(Guid.NewGuid(), Faker.Commerce.ProductName(), quantity, unitPrice);

    /// <summary>A valid Sale populated with one item so the aggregate validates out of the box.</summary>
    public static Sale GenerateValidSale(int itemQuantity = 2, decimal unitPrice = 10m)
    {
        var sale = new Sale
        {
            SaleNumber = Faker.Commerce.Ean8(),
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Company.CompanyName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Address.City()
        };
        sale.AddItem(Guid.NewGuid(), Faker.Commerce.ProductName(), itemQuantity, unitPrice);
        return sale;
    }
}
