using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Feralas
{
    public class DbUpdater
    {
        public async Task DoUpdatesAsync(string json, string tag)
        {
            Listings auctions = new();
            await auctions.CreateLists(json, tag);

            PostgresContext context = new PostgresContext();

            await DbItemUpdaterAsync(context, auctions, tag);
            await DbAuctionsUpdaterAsync(context, auctions, tag);
            //Task backgroundNamer = DbItemNamerAsync(context);
        }

        public async Task DbAuctionsUpdaterAsync(PostgresContext context, Listings auctions, string tag)
        {

            await Task.Delay(1);
            List<WowAuction> incoming = auctions.LiveAuctions;
            List<WowAuction> storedAuctions = new();
            List<WowAuction> ancientListings = new();
            string PartitionKey = incoming.FirstOrDefault().PartitionKey;
            int connectedRealmId = incoming.FirstOrDefault().ConnectedRealmId;
            // the live dataset is less than 48 hours old, is not sold and is same realm
            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            DateTime ancientDeleteTime = DateTime.UtcNow - new TimeSpan(7,0,0,0);


            try
            {
                ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == connectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();
                storedAuctions = context.WowAuctions.Where(l => l.ConnectedRealmId == connectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).ToList();
                context.Dispose();
            }
            catch (Exception ex)
            {

                LogMaker.LogToTable($"Oops I can't count.", ex.Message);
            }



            List<WowAuction> auctionsToAdd = incoming.Except(storedAuctions).ToList();
            List<WowAuction> auctionsToUpdate = incoming.Intersect(storedAuctions).ToList();
            List<WowAuction> absentListings = storedAuctions.Except(incoming).ToList();
            List<WowAuction> soldListings = new();


            // set right now as last time the auction was seen
            foreach (WowAuction auction in auctionsToAdd)
            {
                auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            }

            // many absent listings are sold
            foreach (WowAuction auction in absentListings)
            {
                if (auction.ShortTimeLeftSeen == false)
                {
                    auction.Sold = true;
                    soldListings.Add(auction);
                }
            }

            // no reason to set datestamp on a sold listing
            auctionsToUpdate = auctionsToUpdate.Except(soldListings).ToList();

            // set right now as last time the auction was seen
            foreach (WowAuction auction in auctionsToUpdate)
            {
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            }

            absentListings = absentListings.Except(soldListings).ToList();

            if (absentListings.Count == 0)
            {
                LogMaker.LogToTable($"{tag}", $"{auctionsToAdd.Count} auctions to add, {soldListings.Count} to mark sold, {auctionsToUpdate.Count} auctions to update and {ancientListings.Count} auctions to delete.");
            }
            else
            {
                LogMaker.LogToTable($"{tag}", $"{auctionsToAdd.Count} auctions to add, {soldListings.Count} to mark sold, {auctionsToUpdate.Count} auctions to update and {absentListings.Count} expired and {ancientListings.Count} auctions to delete.");
            }





            using (PostgresContext postgresContext = new())
            {
                try
                {
                    postgresContext.RemoveRange(ancientListings);
                    postgresContext.RemoveRange(absentListings);
                    postgresContext.AddRange(auctionsToAdd);
                    postgresContext.UpdateRange(soldListings);
                    postgresContext.UpdateRange(auctionsToUpdate);
                    await postgresContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    LogMaker.LogToTable($"DbUpdater", $"_______________DbUpdater_______________");
                    LogMaker.LogToTable($"DbUpdater", $"{ex.Message}");
                    LogMaker.LogToTable($"DbUpdater", "_______________UPDATE FAILED_______________");
                }
            }
        }

        async Task DbItemUpdaterAsync(PostgresContext context, Listings auctions, string tag)
        {
            List<WowItem> storedItems = context.WowItems.ToList();
            List<WowItem> itemsToAdd = new();
            WowItem trialItem = new();

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

            try
            {
                await context.WowItems.AddRangeAsync(itemsToAdd);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogMaker.LogToTable($"DbUpdater", "_______________DbUpdater_______________");
                LogMaker.LogToTable($"DbUpdater", "UPDATE FOR ITEMS FAILED");
                LogMaker.LogToTable($"DbUpdater", $"{ex.Message}");
                LogMaker.LogToTable($"DbUpdater", "_______________DbUpdater_______________");
                if (ex.InnerException.ToString() != null)
                {
                    LogMaker.LogToTable($"DbUpdater", $"{ex.InnerException}");
                }
                LogMaker.LogToTable($"DbUpdater", "_______________DbUpdater_______________");
            }


        }

        async Task DbItemNamerAsync(PostgresContext context)
        {
            List<WowItem> itemsToBeNamed = context.WowItems.Where(l => l.Name.Length < 2).ToList();

            if (itemsToBeNamed.Count > 100)
            {
                itemsToBeNamed = itemsToBeNamed.Take(100).ToList();
            }


            foreach (WowItem itemm in itemsToBeNamed)
            {
                try
                {
                    itemm.Name = await WowApi.GetItemName(itemm.ItemId);
                    if (itemm.Name.Length > 2)
                    {
                        // LogMaker.LogToTable($"DbUpdater", $"{itemm.Name} added to the database.");
                    }

                }
                catch
                {
                    LogMaker.LogToTable($"DbUpdater", $"Blizzard API timeout");
                    await Task.Delay(1000 * 60);
                }
                await Task.Delay(100);
            }

            if (itemsToBeNamed.Count > 0)
            {
                try
                {
                    context.WowItems.UpdateRange(itemsToBeNamed);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    LogMaker.LogToTable($"DbUpdater", "_______________DbUpdater_______________");
                    LogMaker.LogToTable($"DbUpdater", "-----------NAMING FOR ITEMS FAILED");
                    LogMaker.LogToTable($"DbUpdater", $"{ex.Message}");
                    LogMaker.LogToTable($"DbUpdater", "_______________DbUpdater_______________");
                    if (ex.InnerException.ToString() != null)
                    {
                        LogMaker.LogToTable($"DbUpdater", $"{ex.InnerException}");
                    }
                    LogMaker.LogToTable($"DbUpdater", "_______________DbUpdater_______________");
                }
            }
        }
    }
}
