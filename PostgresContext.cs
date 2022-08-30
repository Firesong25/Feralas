using Feralas;
using Microsoft.EntityFrameworkCore;

namespace Feralas
{
    public class PostgresContext : DbContext
    {
        public DbSet<WowAuction> WowAuctions { get; set; }
        public DbSet<WowItem> WowItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Configurations.PostgresConnectionString);
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}