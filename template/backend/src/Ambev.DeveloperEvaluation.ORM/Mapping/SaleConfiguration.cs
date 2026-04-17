using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SaleDate).IsRequired();

        builder.Property(s => s.CustomerId).IsRequired();
        builder.Property(s => s.CustomerName).IsRequired().HasMaxLength(200);

        builder.Property(s => s.BranchId).IsRequired();
        builder.Property(s => s.BranchName).IsRequired().HasMaxLength(200);

        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.IsCancelled).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt);

        // xmin is Postgres' system row-version column; EF uses it for optimistic
        // concurrency without needing an application-managed timestamp column.
        builder.Property(s => s.RowVersion)
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasIndex(s => s.SaleNumber).IsUnique();
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.BranchId);
        builder.HasIndex(s => s.SaleDate);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Sale.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.DomainEvents);
    }
}
