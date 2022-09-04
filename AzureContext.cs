
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Net;

namespace Feralas
{
    public class AzureContext : DbContext
    {
        public DbSet<WowAuction> WowAuctions { get; set; }
        public DbSet<WowItem> WowItems { get; set; }

        #region Configuration
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseCosmos(Configurations.CosmosConnectionString, "dalarancosmossql");
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region DefaultContainer
            modelBuilder.HasDefaultContainer("WowAuctions");
            #endregion

            #region Container
            modelBuilder.Entity<WowItem>()
                .ToContainer("WowItems");
            #endregion

            #region NoDiscriminator
            modelBuilder.Entity<WowAuction>()
                .HasNoDiscriminator();
            #endregion

            #region PartitionKey
            modelBuilder.Entity<WowAuction>().HasPartitionKey(o => o.PartitionKey);
            #endregion

            //#region ETag
            //modelBuilder.Entity<WowAuction>()
            //    .UseETagConcurrency();
            //#endregion

            #region PropertyNames
            //modelBuilder.Entity<WowAuction>().OwnsOne(l => l.ItemId);
            #endregion

            #region OwnsMany
            //modelBuilder.Entity<WowItem>().OwnsMany(p => p.ShippingCenters);
            //#endregion

            //#region ETagProperty
            //modelBuilder.Entity<WowItem>()
            //    .Property(d => d.ETag)
            //    .IsETagConcurrency();
            #endregion
        }
    }
}