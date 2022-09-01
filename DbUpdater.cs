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
            Listings auctions = new();
            await auctions.CreateLists(json);
            await auctions.GetExtraItemsAsync();
            await auctions.GetLiveAuctionsAsync();


            await DbItemUpdaterAsync(context, auctions, tag);
            await DbAuctionsUpdaterAsync(context, auctions, tag);
        }

        public async Task DbAuctionsUpdaterAsync(PostgresContext context, Listings auctions, string tag)
        {

            await Task.Delay(1);
            List<WowAuction> incoming = auctions.LiveAuctions;
            string PartitionKey = incoming.FirstOrDefault().PartitionKey;

            // the live dataset is less than 48 hours old, is not sold and is same realm
            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            List<WowAuction> storedAuctions = context.WowAuctions.Where(l =>
                l.PartitionKey == PartitionKey &&
                l.Sold == false &&
                l.FirstSeenTime > cutOffTime).ToList();

            List<WowAuction> auctionsToAdd = incoming.Except(storedAuctions).ToList();
            List<WowAuction> auctionsToUpdate = (from b in storedAuctions
                                                 join bl in incoming on
                                                 new { AuctionId = b.AuctionId } equals
                                                 new { AuctionId = bl.AuctionId }
                                                 select b).ToList();

            // set right now as last time the auction was seen
            foreach (WowAuction auction in auctionsToUpdate)
            {
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            }

            // many absent listings are sold
            List<WowAuction> absentListings = storedAuctions.Except(incoming).ToList();
            foreach (WowAuction auction in absentListings)
            {
                if (auction.ShortTimeLeftSeen == false)
                {
                    auction.Sold = true;
                }
            }


            try
            {
                LogMaker.Log($"We have {auctionsToAdd.Count} to add and {auctionsToUpdate.Count}to update in the database for {tag}.");
                context.AddRange(auctionsToAdd);
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
