using Microsoft.EntityFrameworkCore;

namespace Feralas
{
    public class PostgresContext : DbContext
    {
        public DbSet<WowAuction> WowAuctions { get; set; }
        public DbSet<WowItem> WowItems { get; set; }

        public DbSet<WowRealm> WowRealms { get; set; }

        public DbSet<Recipe> Recipes { get; set; }

        public DbSet<CraftedItem> CraftedItems { get; set; }
        public DbSet<MarginReport> MarginReports { get; set; }
        public DbSet<Profession> Professions { get; set; }
        public DbSet<SkillTier> SkillTier { get; set; }

 
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Configurations.OVHConnectionString);
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

}