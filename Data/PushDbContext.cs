using CapstoneProjectPUSH_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstoneProjectPUSH_API.Data
{
    public class PushDbContext : DbContext
    {
        public PushDbContext(DbContextOptions<PushDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();
        }
    }
}