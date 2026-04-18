using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

/// <summary>Bogus fixtures for the Sales handler test classes.</summary>
public static class SalesHandlerTestData
{
    private static readonly Faker Faker = new();

    public static CreateSaleCommand ValidCreateCommand(int itemCount = 2, int quantity = 2, decimal unitPrice = 10m)
    {
        var cmd = new CreateSaleCommand
        {
            SaleNumber = Faker.Commerce.Ean8(),
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Company.CompanyName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Address.City(),
        };
        for (var i = 0; i < itemCount; i++)
        {
            cmd.Items.Add(new CreateSaleItemCommand
            {
                ProductId = Guid.NewGuid(),
                ProductName = Faker.Commerce.ProductName(),
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }
        return cmd;
    }

    public static UpdateSaleCommand ValidUpdateCommand(Guid id, int itemCount = 2, int quantity = 4, decimal unitPrice = 10m)
    {
        var cmd = new UpdateSaleCommand
        {
            Id = id,
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Company.CompanyName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Address.City(),
        };
        for (var i = 0; i < itemCount; i++)
        {
            cmd.Items.Add(new UpdateSaleItemCommand
            {
                ProductId = Guid.NewGuid(),
                ProductName = Faker.Commerce.ProductName(),
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }
        return cmd;
    }

    public static ListSalesQuery ValidListQuery(int page = 1, int pageSize = 10) =>
        new() { Page = page, PageSize = pageSize };

    public static Sale ValidSale(string? saleNumber = null, int itemQuantity = 2, decimal unitPrice = 10m)
    {
        var sale = new Sale
        {
            SaleNumber = saleNumber ?? Faker.Commerce.Ean8(),
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Company.CompanyName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Address.City(),
        };
        sale.AddItem(Guid.NewGuid(), Faker.Commerce.ProductName(), itemQuantity, unitPrice);
        return sale;
    }
}
