using Feralas;
using Microsoft.EntityFrameworkCore;

namespace Feralas
{
    public class LocalContext : DbContext
    {
        public DbSet<WowItem> WowItems { get; set; }
        public DbSet<WowAuction> WowAuctions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string[] paths = { Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Data", "blizzard_data.db" };
            string fullPath = Path.Combine(paths);
            optionsBuilder.UseSqlite($"Data Source={fullPath}");
        }

    }

}
