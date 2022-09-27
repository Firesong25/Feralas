using Microsoft.EntityFrameworkCore;
namespace Feralas;


public class LocalContext : DbContext
{
    public virtual DbSet<CraftedItem> CraftedItems { get; set; }
    public virtual DbSet<Profession> Professions { get; set; }
    public virtual DbSet<Recipe> Recipes { get; set; }
    public virtual DbSet<SkillTier> SkillTiers { get; set; }
    public virtual DbSet<WowAuction> WowAuctions { get; set; }
    public virtual DbSet<WowItem> WowItems { get; set; }
    public virtual DbSet<WowRealm> WowRealms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string[] paths = { Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Data", "auction_data.db" };
        string fullPath = Path.Combine(paths);
        optionsBuilder.UseSqlite($"Data Source={fullPath}");
    }

}

