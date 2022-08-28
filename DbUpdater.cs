using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feralas
{
    public class DbUpdater
    {
        public async Task DoUpdatesAsync(LocalContext context, string json)
        {

            LogMaker.Log($"{context.WowItems.Count()} items already stored.");
            LogMaker.Log($"{context.WowAuctions.Count()} auctions already stored.");
            Listings auctions = new();
            await auctions.CreateLists(json);
            await auctions.GetExtraItemsAsync();
            await auctions.GetLiveAuctionsAsync();


            await DbItemUpdaterAsync(context, auctions);
            LogMaker.Log($"{context.WowItems.Count()} items are now stored.");
            await DbAuctionsUpdaterAsync(context, auctions);
            LogMaker.Log($"{context.WowAuctions.Count()} auctions are now stored.");
        }

        public async Task DbAuctionsUpdaterAsync(LocalContext context, Listings auctions)
        {

            DateTime cutOffTime = DateTime.Now - new TimeSpan(50, 50, 50);
            List<WowAuction> storedAuctions = context.WowAuctions.Where(l =>
                l.FirstSeenTime > cutOffTime).ToList();
            List<WowAuction> auctionsToAdd = new();
            List<WowAuction> auctionsToUpdate = new();
            WowAuction trial = new();

            LogMaker.Log($"We have {auctions.LiveAuctions.Count} to consider adding to database.");

            foreach (WowAuction listing in auctions.LiveAuctions)
            {
                trial = storedAuctions.FirstOrDefault(l => l.ConnectedRealmId == listing.ConnectedRealmId && l.AuctionId == listing.AuctionId);
                if (trial == null)
                {
                    listing.FirstSeenTime = DateTime.Now - new TimeSpan(0, 5, 0);
                    listing.LastSeenTime = DateTime.Now;
                    auctionsToAdd.Add(listing);
                }
                else
                {
                    listing.LastSeenTime = DateTime.Now;
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
            try
            {
                context.UpdateRange(auctionsToUpdate.Where(l => l.PrimaryKey > 0));
            }
            catch
            {
                LogMaker.Log("___________________________________");
                LogMaker.Log($"The instance of entity type 'WowAuction' cannot be tracked because another instance with the same key value for {{'PrimaryKey'}} is already being tracked. ");
                LogMaker.Log("___________________________________");
            }
            context.SaveChanges();

        }

        public async Task DbItemUpdaterAsync(LocalContext context, Listings auctions)
        {
            List<WowItem> storedItems = context.WowItems.ToList();
            List<WowItem> itemsToAdd = new();
            WowItem trialItem = new();

            LogMaker.Log($"{auctions.ExtraItems.Count} items to consider adding to the database.");

            foreach (WowItem item in auctions.ExtraItems)
            {
                trialItem = storedItems.FirstOrDefault(l => l.ItemId == item.ItemId);

                if (trialItem == null)
                    itemsToAdd.Add(item);

                trialItem = new();
            }

            LogMaker.Log($"{itemsToAdd.Count} items to actually add to the database.");
            await context.WowItems.AddRangeAsync(itemsToAdd);
            await context.SaveChangesAsync();

        }

        public async Task OldDbAuctionsUpdaterAsync(LocalContext context, Listings auctions)
        {

            DateTime cutOffTime = DateTime.Now - new TimeSpan(50, 50, 50);
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
                    listing.FirstSeenTime = DateTime.Now - new TimeSpan(0, 5, 0);
                    listing.LastSeenTime = DateTime.Now;
                    auctionsToAdd.Add(listing);
                }
                else
                {
                    listing.LastSeenTime = DateTime.Now;
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
            context.UpdateRange(auctionsToUpdate.Where(l => l.PrimaryKey > 0));
            context.SaveChanges();

        }

        public async Task OldDbItemUpdaterAsync(LocalContext context, Listings auctions)
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
