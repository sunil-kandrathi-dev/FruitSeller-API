using FruitSeller.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Persistence;

public class FruitSellerDbContext : DbContext
{
    public FruitSellerDbContext(DbContextOptions<FruitSellerDbContext> options) : base(options)
    {
    }

    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Fruit> Fruits => Set<Fruit>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("Shops");
            entity.HasKey(x => x.ShopId);
            entity.Property(x => x.ShopName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.ShopName).IsUnique();
            entity.Property(x => x.OwnerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(10).IsFixedLength().IsRequired();
            entity.Property(x => x.OpeningBalance).HasColumnType("decimal(19,2)").HasDefaultValue(0m);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<Fruit>(entity =>
        {
            entity.ToTable("Fruits");
            entity.HasKey(x => x.FruitId);
            entity.Property(x => x.FruitName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.FruitName).IsUnique();
            entity.Property(x => x.Variety).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.ToTable("Purchases");
            entity.HasKey(x => x.PurchaseId);
            entity.Property(x => x.SupplierName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TotalCost).HasColumnType("decimal(19,2)");
            entity.Property(x => x.PurchaseDate).HasColumnType("date");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(x => x.SaleId);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(19,2)").HasComputedColumnSql("[BoxQuantity] * [PricePerBox]", stored: true);
            entity.Property(x => x.PricePerBox).HasColumnType("decimal(19,2)");
            entity.Property(x => x.SaleDate).HasColumnType("date");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.PaymentId);
            entity.Property(x => x.Amount).HasColumnType("decimal(19,2)");
            entity.Property(x => x.PaymentMethod).HasMaxLength(10).IsRequired();
            entity.Property(x => x.PaymentDate).HasColumnType("date");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.ToTable("SupplierPayments");
            entity.HasKey(x => x.SupplierPaymentId);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PaymentDate).HasColumnType("date");
            entity.Property(x => x.ReferenceNumber).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Purchase)
                .WithMany()
                .HasForeignKey(x => x.PurchaseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");
            entity.HasKey(x => x.ExpenseId);
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.ExpenseDate).HasColumnType("date");
            entity.Property(x => x.Amount).HasColumnType("decimal(19,2)");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.ToTable("InventoryMovements");
            entity.HasKey(x => x.InventoryMovementId);
            entity.Property(x => x.MovementType).HasMaxLength(10).IsRequired();
            entity.Property(x => x.MovementDate).HasColumnType("date");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.PurchaseId).IsUnique();
            entity.HasIndex(x => x.SaleId).IsUnique();
        });
    }
}
