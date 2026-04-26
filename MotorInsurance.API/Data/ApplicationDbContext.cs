using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Common;
using MotorInsurance.API.Models;

namespace MotorInsurance.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Car>()
                .HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<Client>()
                .HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<Claim>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Car>()
                .Property(c => c.FuelType)
                .HasConversion<string>();

            modelBuilder.Entity<Car>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Quote>()
                .Property(q => q.Price)
                .HasPrecision(18, 2);
        }
    }
}