using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Password).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Phone).IsRequired().HasMaxLength(40);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.OwnsOne(u => u.Name, n =>
        {
            n.Property(x => x.FirstName).HasColumnName("NameFirstName").IsRequired().HasMaxLength(100);
            n.Property(x => x.LastName).HasColumnName("NameLastName").IsRequired().HasMaxLength(100);
        });

        builder.OwnsOne(u => u.Address, a =>
        {
            a.Property(x => x.City).HasColumnName("AddressCity").IsRequired().HasMaxLength(100);
            a.Property(x => x.Street).HasColumnName("AddressStreet").IsRequired().HasMaxLength(200);
            a.Property(x => x.Number).HasColumnName("AddressNumber").IsRequired();
            a.Property(x => x.Zipcode).HasColumnName("AddressZipcode").IsRequired().HasMaxLength(20);
            a.OwnsOne(x => x.Geolocation, g =>
            {
                g.Property(x => x.Lat).HasColumnName("AddressGeoLat").IsRequired().HasMaxLength(50);
                g.Property(x => x.Long).HasColumnName("AddressGeoLong").IsRequired().HasMaxLength(50);
            });
        });

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}
