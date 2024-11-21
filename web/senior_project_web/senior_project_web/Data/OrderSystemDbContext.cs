using Microsoft.EntityFrameworkCore;
using senior_project_web.Models;

namespace senior_project_web.Data
{
    public class OrderSystemDbContext:DbContext
    {
        public OrderSystemDbContext(DbContextOptions<OrderSystemDbContext> options) : base(options) {
            
        }

        public DbSet<UserModel> User { get; set; }
        public DbSet<AdminModel> Admin { get; set; }
        public DbSet<OrderModel> Order { get; set; }
        public DbSet<InventoryModel> Inventory { get; set; }
        public DbSet<MealModel> Meal { get; set; }
        public DbSet<Order_MealModel> Order_Meal { get; set; }
        public DbSet<PredictionModel> Prediction { get; set; }
        public DbSet<Daily_Sales_ReportModel> Daily_Sales_Report { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AdminModel>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<AdminModel>(a => a.user_id);
            modelBuilder.Entity<AdminModel>()
                .Property(a => a.admin_account)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<MealModel>()
                .HasOne(m => m.Inventory)
                .WithMany(i => i.Meal)
                .HasForeignKey(m => m.inventory_id);
        }
    }
}
