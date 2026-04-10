using Microsoft.EntityFrameworkCore;

namespace CafeManagementMVC.Models
{
    public class CafeDbContext : DbContext
    {
        public CafeDbContext(DbContextOptions<CafeDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CafeTable> CafeTables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}