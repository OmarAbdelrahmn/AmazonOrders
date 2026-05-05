using EmployeeOrderApi.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOrderApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Order>    Orders    { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Employee ──────────────────────────────────────────────────────────
        builder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IqamaNo).IsUnique();
            e.Property(x => x.NameAR).IsRequired().HasMaxLength(200);
            e.Property(x => x.NameEN).IsRequired().HasMaxLength(200);
            e.Property(x => x.IqamaNo).IsRequired();
            e.Property(x => x.ImgUrl).HasMaxLength(500);
            e.Property(x => x.RiderId).HasMaxLength(100);
            e.Property(x => x.HousingName).HasMaxLength(200);

            // Soft-delete filter – only return non-deleted employees by default
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Order ─────────────────────────────────────────────────────────────
        builder.Entity<Order>(o =>
        {
            o.HasKey(x => x.Id);
            o.Property(x => x.Service).IsRequired().HasMaxLength(300);
            o.Property(x => x.RequestedBy).IsRequired().HasMaxLength(256);
            o.Property(x => x.Notes).HasMaxLength(1000);

            o.HasOne(x => x.Employee)
             .WithMany(x => x.Orders)
             .HasForeignKey(x => x.IqamaNo)
             .HasPrincipalKey(x => x.IqamaNo)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
