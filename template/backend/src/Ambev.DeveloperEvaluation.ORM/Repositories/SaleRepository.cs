using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context) => _context = context;

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default) =>
        _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber, cancellationToken);

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        // the aggregate is tracked if it was loaded via GetByIdAsync; SaveChanges
        // will flush the in memory mutations and the xmin concurrency token
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale is null) return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? saleNumber = null,
        Guid? customerId = null,
        Guid? branchId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeCancelled = true,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sale> query = _context.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(saleNumber))
            query = query.Where(s => s.SaleNumber == saleNumber);
        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);
        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);
        if (startDate.HasValue)
            query = query.Where(s => s.SaleDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.SaleDate <= endDate.Value);
        if (!includeCancelled)
            query = query.Where(s => !s.IsCancelled);

        query = ApplyOrder(query, orderBy);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Items)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <summary>Supports REST-style ordering (e.g. "saleDate desc, saleNumber asc"). Unknown fields fall back to SaleDate desc.</summary>
    private static IQueryable<Sale> ApplyOrder(IQueryable<Sale> query, string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return query.OrderByDescending(s => s.SaleDate);

        IOrderedQueryable<Sale>? ordered = null;
        foreach (var token in orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = parts[0];
            var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            ordered = (field.ToLowerInvariant(), desc, ordered) switch
            {
                ("salenumber",   false, null) => query.OrderBy(s => s.SaleNumber),
                ("salenumber",   true,  null) => query.OrderByDescending(s => s.SaleNumber),
                ("saledate",     false, null) => query.OrderBy(s => s.SaleDate),
                ("saledate",     true,  null) => query.OrderByDescending(s => s.SaleDate),
                ("totalamount",  false, null) => query.OrderBy(s => s.TotalAmount),
                ("totalamount",  true,  null) => query.OrderByDescending(s => s.TotalAmount),
                ("salenumber",   false, _) => ordered!.ThenBy(s => s.SaleNumber),
                ("salenumber",   true,  _) => ordered!.ThenByDescending(s => s.SaleNumber),
                ("saledate",     false, _) => ordered!.ThenBy(s => s.SaleDate),
                ("saledate",     true,  _) => ordered!.ThenByDescending(s => s.SaleDate),
                ("totalamount",  false, _) => ordered!.ThenBy(s => s.TotalAmount),
                ("totalamount",  true,  _) => ordered!.ThenByDescending(s => s.TotalAmount),
                _ => ordered ?? query.OrderByDescending(s => s.SaleDate)
            };
        }
        return ordered ?? query.OrderByDescending(s => s.SaleDate);
    }
}
