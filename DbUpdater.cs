using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Feralas
{
    public class DbUpdater
    {
        public async Task DoUpdatesAsync(PostgresContext context, string json, string tag)
        {

            LogMaker.Log($"{context.WowItems.Count()} items already stored.");
            LogMaker.Log($"{context.WowAuctions.Count()} auctions already stored.");
            Listings auctions = new();
            await auctions.CreateLists(json);
            await auctions.GetExtraItemsAsync();
            await auctions.GetLiveAuctionsAsync();


            await DbItemUpdaterAsync(context, auctions, tag);
            LogMaker.Log($"{context.WowItems.Count()} items are now stored.");
            await DbAuctionsUpdaterAsync(context, auctions, tag);
            LogMaker.Log($"{context.WowAuctions.Count()} auctions are now stored.");
        }

        public async Task DbAuctionsUpdaterAsync(PostgresContext context, Listings auctions, string tag)
        {
            await Task.Delay(1);
            string PartitionKey = auctions.LiveAuctions.FirstOrDefault().PartitionKey;

            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            List<WowAuction> storedAuctions = context.WowAuctions.Where(l =>
                l.PartitionKey == PartitionKey &&
                l.FirstSeenTime > cutOffTime).ToList();
            List<WowAuction> auctionsToAdd = new();
            List<WowAuction> auctionsToUpdate = new();
            WowAuction trial = new();

            LogMaker.Log($"We have {auctions.LiveAuctions.Count} auctions to consider adding to database for {tag}.");

            int z = 0;

            LogMaker.Log($"Seeing which auctions on {tag} are already sold.");
            foreach (WowAuction auction in storedAuctions)
            {
                trial = auctions.LiveAuctions.FirstOrDefault(l => l.AuctionId == auction.AuctionId);
                if (trial == null && auction.ShortTimeLeftSeen == false)
                {
                    auction.Sold = true;
                    auctionsToUpdate.Add(auction);
                }
                trial = new();
            }
            LogMaker.Log($"Seeing which auctions on {tag} are new and which need to be updated.");
            foreach (WowAuction listing in auctions.LiveAuctions)
            {
                trial = storedAuctions.FirstOrDefault(l => l.PartitionKey == listing.PartitionKey && l.AuctionId == listing.AuctionId);
                if (trial == null)
                {
                    listing.Id = Guid.NewGuid();
                    listing.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                    listing.FirstSeenTime = DateTime.SpecifyKind(listing.FirstSeenTime, DateTimeKind.Utc);
                    listing.LastSeenTime = DateTime.UtcNow;
                    listing.LastSeenTime = DateTime.SpecifyKind(listing.LastSeenTime, DateTimeKind.Utc);
                    auctionsToAdd.Add(listing);
                }
                else
                {
                    listing.LastSeenTime = DateTime.UtcNow;
                    trial = auctionsToUpdate.FirstOrDefault(l => l.PartitionKey == listing.PartitionKey && l.AuctionId == listing.AuctionId);
                    if (trial != null)
                    {
                        auctionsToUpdate.Add(listing);
                    }                    
                }

                trial = new();
                z++;
                if (z % 15000 == 0)
                {
                    try
                    {
                        LogMaker.Log($"{z} considered for {tag}. Save changes and proceed to add {auctionsToAdd.Count} and update {auctionsToUpdate.Count}.");
                        context.AddRange(auctionsToAdd);
                        context.UpdateRange(auctionsToUpdate);
                        context.SaveChanges();
                        auctionsToAdd = new();
                        auctionsToUpdate = new();
                    }
                    catch (Exception ex)
                    {
                        LogMaker.Log("_______________DbUpdater_______________");
                        LogMaker.Log($"Failed to save chunk of auctions.");
                        LogMaker.Log($"{ex.Message}");
                        LogMaker.Log("_______________DbUpdater_______________");
                        if (ex.InnerException.ToString() != null)
                        {
                            LogMaker.Log($"{ex.InnerException}");
                        }
                    }
                }
            }


            try
            {
                LogMaker.Log($"We have {auctionsToAdd.Count} left to actually add to database for {tag}.");
                context.AddRange(auctionsToAdd);
                LogMaker.Log($"We have {auctionsToUpdate.Count} left to update in the database for {tag}.");
                context.UpdateRange(auctionsToUpdate);
            }
            catch (Exception ex)
            {
                LogMaker.Log("_______________DbUpdater_______________");
                LogMaker.Log("UPDATE FOR AUCTIONS FAILED");
                LogMaker.Log($"{ex.Message}");
                LogMaker.Log("_______________DbUpdater_______________");
                if (ex.InnerException.ToString() != null)
                {
                    LogMaker.Log($"{ex.InnerException}");
                }
                LogMaker.Log("_______________DbUpdater_______________");
            }
            try
            {
                LogMaker.Log($"Saving changes for {tag}.");
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                LogMaker.Log("_______________DbUpdater_______________");
                LogMaker.Log($"Failed to save upsert of auctions.");
                LogMaker.Log($"{ex.Message}");
            }
        }

        async Task DbItemUpdaterAsync(PostgresContext context, Listings auctions, string tag)
        {
            List<WowItem> storedItems = context.WowItems.ToList();
            List<WowItem> itemsToAdd = new();
            WowItem trialItem = new();

            LogMaker.Log($"{auctions.ExtraItems.Count} items from {tag} auction house to consider adding to the database.");

            foreach (WowItem item in auctions.ExtraItems)
            {
                trialItem = storedItems.FirstOrDefault(l => l.ItemId == item.ItemId);

                if (trialItem == null)
                {
                    item.Id = Guid.NewGuid();
                    itemsToAdd.Add(item);
                }
                    

                trialItem = new();
            }

            LogMaker.Log($"{itemsToAdd.Count} items from {tag} auction house to actually add to the database.");
            try
            {
                await context.WowItems.AddRangeAsync(itemsToAdd);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogMaker.Log("_______________DbUpdater_______________");
                LogMaker.Log("UPDATE FOR ITEMS FAILED");
                LogMaker.Log($"{ex.Message}");
                LogMaker.Log("_______________DbUpdater_______________");
                if (ex.InnerException.ToString() != null)
                {
                    LogMaker.Log($"{ex.InnerException}");
                }
                LogMaker.Log("_______________DbUpdater_______________");
            }
        }

        async Task OldDbAuctionsUpdaterAsync(LocalContext context, Listings auctions)
        {

            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            List<WowAuction> storedAuctions = context.WowAuctions.Where(l =>
                l.FirstSeenTime > cutOffTime).ToList();
            List<WowAuction> auctionsToAdd = new();
            List<WowAuction> auctionsToUpdate = new();
            WowAuction trial = new();

            LogMaker.Log($"We have {auctions.LiveAuctions.Count} to consider adding to database.");

            foreach (WowAuction listing in auctions.LiveAuctions)
            {
                trial = storedAuctions.FirstOrDefault(l => l.AuctionId == listing.AuctionId);
                if (trial == null)
                {
                    listing.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                    listing.LastSeenTime = DateTime.UtcNow;
                    auctionsToAdd.Add(listing);
                }
                else
                {
                    listing.LastSeenTime = DateTime.UtcNow;
                    auctionsToUpdate.Add(listing);
                }

                trial = new();
            }

            foreach (WowAuction auction in storedAuctions)
            {
                trial = auctions.LiveAuctions.FirstOrDefault(l => l.AuctionId == auction.AuctionId);
                if (trial == null && auction.ShortTimeLeftSeen == false)
                {
                    auction.Sold = true;
                    auctionsToUpdate.Add(auction);
                }
                trial = new();
            }

            LogMaker.Log($"We have {auctionsToAdd.Count} to actually add to database.");
            context.AddRange(auctionsToAdd);
            LogMaker.Log($"We have {auctionsToUpdate.Count} to update in the database.");
            context.UpdateRange(auctionsToUpdate.Where(l => l.Id != Guid.Empty));
            context.SaveChanges();

        }

        async Task OldDbItemUpdaterAsync(LocalContext context, Listings auctions)
        {
            List<WowItem> storedItems = context.WowItems.ToList();
            List<WowItem> itemsToAdd = new();
            WowItem trialItem = new();

            List<WowItem> newItems = auctions.ExtraItems;

            int e = newItems.Where(l => l.PetBreedId > 0).Count();
            int d = newItems.Where(l => l.BonusList != string.Empty).Count();
            int f = newItems.Where(l => l.BonusList == string.Empty && l.PetBreedId == 0).Count();
            LogMaker.Log($"{e} pet items, {d} gear items and {f} other items to consider adding to database.");

            d = 0; e = 0; f = 0;
            foreach (WowItem item in newItems)
            {
                if (item.BonusList == string.Empty && item.PetBreedId == 0)
                {
                    trialItem = storedItems.FirstOrDefault(l => l.ItemId == item.ItemId);
                    if (trialItem == null)
                    {
                        f++;
                        itemsToAdd.Add(item);
                    }
                }
                else if (item.PetBreedId == 0)
                {
                    trialItem = storedItems.FirstOrDefault(l => l.ItemId == item.ItemId && l.BonusList == item.BonusList);
                    if (trialItem == null)
                    {
                        e++;
                        itemsToAdd.Add(item);
                    }
                }

                if (item.PetBreedId > 0)
                {
                    trialItem = storedItems.FirstOrDefault(l => l.ItemId == item.ItemId &&
                        l.PetBreedId == item.PetBreedId &&
                        l.PetQualityId == item.PetQualityId &&
                        l.PetLevel == item.PetLevel &&
                        l.PetSpeciesId == item.PetSpeciesId);

                    if (trialItem == null)
                    {
                        d++;
                        itemsToAdd.Add(item);
                    }
                }

                trialItem = new();
            }

            LogMaker.Log($"{d} pet items, {e} gear items and {f} other items to actually add to database.");
            await context.WowItems.AddRangeAsync(itemsToAdd);
            await context.SaveChangesAsync();

        }

    }
}
