using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;

namespace Feralas
{
    public class DbUpdater
    {
        public async Task<string> DoUpdatesAsync(string json, string tag)
        {
            Listings auctions = new();
            await auctions.CreateLists(json, tag);

            PostgresContext context = new PostgresContext();

            await DbItemUpdaterAsync(context, auctions, tag);
            string response = await DbAuctionsUpdaterAsync(context, auctions, tag);
            //Task backgroundNamer = DbItemNamerAsync(context);
            return response;
        }

        public async Task<string> DbAuctionsUpdaterAsync(PostgresContext context, Listings auctions, string tag)
        {
            string response = string.Empty;

            await Task.Delay(1);
            List<WowAuction> incoming = auctions.LiveAuctions;
            List<WowAuction> storedAuctions = new();
            List<WowAuction> soldListings = new();
            List<WowAuction> ancientListings = new();
            List<WowAuction> shortTimeLeftAuctions = new();
            List<WowAuction> auctionsToAdd = new();
            List<WowAuction> auctionsToUpdate = new();
            List<WowAuction> absentListings = new();
            List<WowAuction> markAsShortTimeLeft = new();

            /*
             * Goals:
             * 1. We only work with stored listings that are less than 48 hours old in the same realm group which are in storedAuctions
             * 2. Listings that are over 7 days old are in the ancientListings list and deleted
             * 3. Listings that have not been seen before are timestamped and stored in auctionsToAdd             
             * 4. Incoming listings that have a SHORT duration are overpriced. List in shortTimeLeftAuctions and mark them in auctionsToUpdate
             * 5. Auctions that are stored but not in incoming listings are either sold or expired. Put in absentListings and then process
             * 6. Stored listings that are in absentListings and are not marked for SHORT duration are sold. Put in soldListings and update stored records.
             * 7. Stored listings that are in absentListings and marked SHORT are expired. Delete them.
             * 8. Stored listings that are still live are in auctionsToUpdate. Update their timestamps.
             * 9. Make a report of all changes.
             */
            string PartitionKey = incoming.FirstOrDefault().PartitionKey;
            int connectedRealmId = incoming.FirstOrDefault().ConnectedRealmId;
            // the live dataset is less than 48 hours old, is not sold and is same realm
            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            DateTime ancientDeleteTime = DateTime.UtcNow - new TimeSpan(7, 0, 0, 0);

            // We only work with stored listings that are less than 48 hours old in the same realm group which are in storedAuctions
            storedAuctions = context.WowAuctions.Where(l => l.ConnectedRealmId == connectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).ToList();

            // Listings that are over 7 days old are in the ancientListings list and deleted
            ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == connectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();
            context.WowAuctions.RemoveRange(ancientListings);

            // Listings that have not been seen before are timestamped and stored in auctionsToAdd
            auctionsToAdd = incoming.Except(storedAuctions).ToList();
            foreach (WowAuction auction in auctionsToAdd)
            {
                auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            }
            context.WowAuctions.AddRange(auctionsToAdd);

            // Listings in both incoming and stored are to be updated
            auctionsToUpdate = storedAuctions.Intersect(incoming).ToList();

            // Incoming listings that have a SHORT duration are overpriced. List in shortTimeLeftAuctions and mark them in auctionsToUpdate
            shortTimeLeftAuctions = incoming.Where(l => l.ShortTimeLeftSeen == true).ToList();
            markAsShortTimeLeft = auctionsToUpdate.Intersect(shortTimeLeftAuctions).ToList();

            foreach (WowAuction mark in markAsShortTimeLeft)
            {
                WowAuction stlseen = auctionsToUpdate.FirstOrDefault(l => l == mark);  // fastest 8ms
                // WowAuction stlseen = auctionsToUpdate.FirstOrDefault(l => l.Id == mark.Id); // 25ms
                // WowAuction stlseen = auctionsToUpdate.FirstOrDefault(l => l.AuctionId == mark.AuctionId && l.ItemId == mark.ItemId); //12ms
                stlseen.ShortTimeLeftSeen = true;
            }

            // Auctions that are stored but not in incoming listings are either sold or expired. Put in absentListings and then process
            absentListings = storedAuctions.Except(incoming).ToList();

            // Listings that are in absentListings and are not marked for SHORT duration are sold. Put in soldListings and update stored records.
            soldListings = absentListings.Where(l => l.ShortTimeLeftSeen == false).ToList();
            context.WowAuctions.UpdateRange(soldListings);

            // Stored listings that are in absentListings and marked SHORT are expired. Delete them.
            context.WowAuctions.RemoveRange(absentListings.Where(l => l.ShortTimeLeftSeen == true));

            // Sold auctions do not need any further updates
            auctionsToUpdate = auctionsToUpdate.Except(soldListings).ToList();

            // Stored listings that are still live are in auctionsToUpdate.  Update their timestamps.
            foreach (WowAuction auction in auctionsToUpdate)
            {
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            }
            context.WowAuctions.UpdateRange(auctionsToUpdate);

            // Make a report of all changes.
            absentListings = absentListings.Except(soldListings).ToList();
            if (ancientListings.Count == 0)
            {
                response = $"{auctionsToAdd.Count} auctions to add, {soldListings.Count} to mark sold, {auctionsToUpdate.Count} auctions to update and {absentListings.Count} auctions to delete.";
            }
            else
            {
                response = $"{auctionsToAdd.Count} auctions to add, {soldListings.Count} to mark sold, {auctionsToUpdate.Count} auctions to update and {absentListings.Count} expired and {ancientListings.Count} auctions to delete.";
            }

            // Save changes and report errors
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogMaker.LogToTable($"DbUpdater", $"_______________{tag} DbUpdater_______________");
                LogMaker.LogToTable($"DbUpdater", $"{ex.Message}");
                LogMaker.LogToTable($"DbUpdater", $"_______________{tag}UPDATE FAILED_______________");
            }
            return response;
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
