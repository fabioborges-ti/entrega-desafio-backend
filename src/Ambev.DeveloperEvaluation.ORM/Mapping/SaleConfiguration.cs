using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.SaleNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.SaleNumber).IsUnique();

        builder.Property(x => x.SaleDate).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.IsCancelled).IsRequired();

        builder.Property(x => x.CustomerId).IsRequired();

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.BranchId).IsRequired();

        builder.HasOne(s => s.Branch)
            .WithMany(b => b.Sales)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CartId);

        builder.HasOne(s => s.Cart)
            .WithOne(c => c.Sale)
            .HasForeignKey<Sale>(s => s.CartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.CartId)
            .IsUnique();

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
