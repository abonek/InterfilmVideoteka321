using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebApplication1.Data
{
    public class BazaDbContext : DbContext
    {
        public BazaDbContext(DbContextOptions<BazaDbContext> options) : base(options)
        {
        }

        public DbSet<User> PopisKorisnika { get; set; }
        public DbSet<Film> PopisFilmova { get; set; }
        public DbSet<Iznajmljivanje> Iznajmljivanja { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }

    public class BazaDbContextFactory : IDesignTimeDbContextFactory<BazaDbContext>
    {
        public BazaDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<BazaDbContext>();
            var cs = config.GetConnectionString("DefaultConnection")!;
            optionsBuilder.UseSqlite(cs);

            return new BazaDbContext(optionsBuilder.Options);
        }
    }
}