using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Ecommerce.Models
{
    public class EcommerceContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<TrackingOrder> TrackingOrders { get; set; }
        public DbSet<BrowsingHistory> BrowsingHistories { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<RateItem> RateItems { get; set; }

        public EcommerceContext(DbContextOptions<EcommerceContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Address>().ToTable("Address");
            modelBuilder.Entity<Admin>().ToTable("Admin");
            modelBuilder.Entity<BrowsingHistory>().ToTable("BrowsingHistory");
            modelBuilder.Entity<Cart>().ToTable("Cart");
            modelBuilder.Entity<CartItem>().ToTable("CartItem");
            modelBuilder.Entity<Item>().ToTable("Item");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItem");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<TrackingOrder>().ToTable("TrackingOrder");
            modelBuilder.Entity<RateItem>().ToTable("RateItem");

            // Configure composite key for RateItem
            modelBuilder.Entity<RateItem>()
                .HasKey(ri => new { ri.UserId, ri.ItemId });

            // User - Address (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey<Address>(a => a.UserId);

            // User - Cart (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId);

            // Product - Department (Many-to-One)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Department)
                .WithMany(d => d.Products)
                .HasForeignKey(p => p.DepartmentId);

            // Item - Product (Many-to-One)
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.ProductId);

            // User - Order (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);

            // OrderItem - Order and Item (Many-to-Many)
            modelBuilder.Entity<OrderItem>()
                .HasKey(op => new { op.OrderId, op.ItemId });

            modelBuilder.Entity<OrderItem>()
                .HasOne(op => op.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(op => op.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(op => op.Item)
                .WithMany(i => i.OrderItems)
                .HasForeignKey(op => op.ItemId);

            // Configure composite key for TrackingOrder
            modelBuilder.Entity<TrackingOrder>()
                .HasKey(to => new { to.OrderId, to.ItemId });

            // TrackingOrder - OrderItem (One-to-One)
            modelBuilder.Entity<TrackingOrder>()
                .HasOne(to => to.OrderItem)
                .WithOne(oi => oi.TrackingOrder)
                .HasForeignKey<TrackingOrder>(to => new { to.OrderId, to.ItemId });

            // BrowsingHistory - User and Item (Many-to-One)
            modelBuilder.Entity<BrowsingHistory>()
                .HasOne(b => b.User)
                .WithMany(u => u.BrowsingHistories)
                .HasForeignKey(b => b.UserId);

            modelBuilder.Entity<BrowsingHistory>()
                .HasOne(b => b.Item)
                .WithMany(i => i.BrowsingHistories)
                .HasForeignKey(b => b.ItemId);

            // CartItem - Cart and Item (Many-to-Many)
            modelBuilder.Entity<CartItem>()
                .HasKey(ci => new { ci.CartId, ci.ItemId });

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Item)
                .WithMany(i => i.CartItems)
                .HasForeignKey(ci => ci.ItemId);

            // RateItem - User and Item (Many-to-One)
            modelBuilder.Entity<RateItem>()
                .HasOne(ri => ri.User)
                .WithMany(u => u.RateItems)
                .HasForeignKey(ri => ri.UserId);

            modelBuilder.Entity<RateItem>()
                .HasOne(ri => ri.Item)
                .WithMany(i => i.RateItem)
                .HasForeignKey(ri => ri.ItemId);
        }
    }
}
