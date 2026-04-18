using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// End-to-end API smoke tests. Repository is mocked so the tests are
/// deterministic and don't require a running Postgres — everything else
/// (middleware, validators, auth, AutoMapper, controller, response envelopes)
/// is exercised for real.
/// </summary>
public class SalesEndpointsTests : IClassFixture<SalesApiFactory>
{
    private readonly SalesApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SalesEndpointsTests(SalesApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "POST /api/sales with empty body returns 400 from request validator")]
    public async Task Create_EmptyBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/sales", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/sales with quantity above the cap returns 400")]
    public async Task Create_QuantityAboveCap_Returns400()
    {
        var payload = new
        {
            saleNumber = "S-CAP-01",
            saleDate = DateTime.UtcNow,
            customerId = Guid.NewGuid(),
            customerName = "Distribuidora de Bebidas Sergio Andrade",
            branchId = Guid.NewGuid(),
            branchName = "São Paulo - Centro",
            items = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Beer", quantity = 21, unitPrice = 1.0m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/sales with a valid payload returns 201 and the created id")]
    public async Task Create_ValidPayload_Returns201()
    {
        _factory.Repository.GetBySaleNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Sale?)null);
        _factory.Repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci => (Sale)ci.Args()[0]);

        var payload = new
        {
            saleNumber = "S-OK-01",
            saleDate = DateTime.UtcNow,
            customerId = Guid.NewGuid(),
            customerName = "Distribuidora de Bebidas Sergio Andrade",
            branchId = Guid.NewGuid(),
            branchName = "São Paulo - Centro",
            items = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Beer", quantity = 12, unitPrice = 9.90m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await _factory.Repository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GET /api/sales/{unknown-id} returns 404 via the exception middleware")]
    public async Task Get_Unknown_Returns404()
    {
        _factory.Repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Sale?)null);

        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "POST /api/sales with a duplicate sale number returns 409 via the exception middleware")]
    public async Task Create_DuplicateSaleNumber_Returns409()
    {
        var existing = new Sale
        {
            SaleNumber = "S-DUP-01",
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Distribuidora de Bebidas Sergio Andrade",
            BranchId = Guid.NewGuid(),
            BranchName = "São Paulo - Centro"
        };
        existing.AddItem(Guid.NewGuid(), "x", 1, 1m);
        _factory.Repository.GetBySaleNumberAsync("S-DUP-01", Arg.Any<CancellationToken>())
            .Returns(existing);

        var payload = new
        {
            saleNumber = "S-DUP-01",
            saleDate = DateTime.UtcNow,
            customerId = Guid.NewGuid(),
            customerName = "Distribuidora de Bebidas Sergio Andrade",
            branchId = Guid.NewGuid(),
            branchName = "São Paulo - Centro",
            items = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Beer", quantity = 5, unitPrice = 10m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
