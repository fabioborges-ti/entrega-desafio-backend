using Ambev.DeveloperEvaluation.Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace Ambev.DeveloperEvaluation.ORM.Mapping;



public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>

{

    public void Configure(EntityTypeBuilder<Inventory> builder)

    {

        builder.ToTable("Inventories");



        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).ValueGeneratedOnAdd();



        builder.Property(i => i.ProductId).IsRequired();

        builder.HasIndex(i => i.ProductId).IsUnique();



        builder.Property(i => i.AvailableQuantity).IsRequired();

        builder.Property(i => i.MinimumStockAlert)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(i => i.Product)

            .WithOne(p => p.Inventory)

            .HasForeignKey<Inventory>(i => i.ProductId)

            .OnDelete(DeleteBehavior.Cascade);

    }

}


