using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Feralas
{
    public class DbUpdater
    {
        public async Task<string> DoUpdatesAsync(WowRealm realm, string json, string tag)
        {
            PostgresContext context = new PostgresContext();

            // Make sure the stored connect realm id is correct
            int cid = await WowApi.GetConnectedRealmId(realm.WowNamespace, realm.Name);

            if (cid != 0 && cid != (int)realm.ConnectedRealmId)
            {
                LogMaker.LogToTable($"IMPORTANT", $"{realm.Name} has changed connected realm id from {realm.ConnectedRealmId} to {cid}.");
                WowRealm brokenRealm = context.WowRealms.FirstOrDefault(l => l.Id == realm.Id);
                brokenRealm.ConnectedRealmId = cid;                
                context.Update(brokenRealm);
                context.SaveChanges();
            }

            Listings auctions = new();
            await auctions.CreateLists(realm, json, tag);



            //await DbItemUpdaterAsync(context, auctions, tag);
            string response = await DbAuctionsUpdaterAsync(context, realm, auctions, tag);
            //Task backgroundNamer = DbItemNamerAsync(context);
            return response;
        }

        public async Task<string> DbAuctionsUpdaterAsync(PostgresContext context, WowRealm realm, Listings auctions, string tag)
        {
            string response = string.Empty;
            Stopwatch sw = Stopwatch.StartNew();

            await Task.Delay(1);
            List<WowAuction> incoming = auctions.LiveAuctions;
            List<WowAuction> storedAuctions = new();
            List<WowAuction> soldListings = new();
            List<WowAuction> unsoldListings = new();
            List<WowAuction> ancientListings = new();
            List<WowAuction> auctionsToAdd = new();
            List<WowAuction> auctionsToUpdate = new();
            List<WowAuction> absentListings = new();
            

            /*
             * Goals:
             * 1. We only work with stored listings that are less than 48 hours old in the same realm group which are in storedAuctions
             * 2. Listings that are over 7 days old are in the ancientListings list and deleted
             * 3. Listings that have not been seen before are timestamped and stored in auctionsToAdd             
             * 4. Auctions that are stored but not in incoming listings are either sold or expired. Put in absentListings and then process
             * 5. Only stored listings that are in absentListings and marked VERY_LONG are assumed sold.
             * 6. Stored listings that are still live are in auctionsToUpdate. Update their timestamps and timeleft tags.
             * 7. Make a report of all changes.
             */
            string PartitionKey = realm.ConnectedRealmId.ToString();
            // the live dataset is less than 48 hours old, is not sold and is same realm
            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            DateTime ancientDeleteTime = DateTime.UtcNow - new TimeSpan(7, 0, 0, 0);

            // We only work with stored listings that are less than 48 hours old in the same realm group which are in storedAuctions
            storedAuctions = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).ToList();

            // Listings that are over 7 days old are in the ancientListings list and deleted


            if (tag.ToLower().Contains("commodities"))
            {
                sw.Restart();
                // there is no value in analysing sold commodity listings as deals have to be done today
                ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId).ToList();
                ancientListings = ancientListings.Except(incoming).ToList();
            }
            else
            {
                ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();
            }

            if (ancientListings.Count > 0)
            {
                context.WowAuctions.RemoveRange(ancientListings);
                context.SaveChanges();
            }

            int vl = incoming.Where(l => l.TimeLeft == TimeLeft.VERY_LONG).Count();


            // Listings that have not been seen before are timestamped and stored in auctionsToAdd
            auctionsToAdd = incoming.Except(storedAuctions).ToList();
            foreach (WowAuction auction in auctionsToAdd)
            {
                auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
                if (auction.UnitPrice == 0 && auction.Buyout > 0)
                {
                    auction.UnitPrice = auction.Buyout;
                }

                if (!auction.PartitionKey.Equals(realm.ConnectedRealmId.ToString()))
                {
                    auction.PartitionKey = realm.ConnectedRealmId.ToString();
                }
            }
            if (auctionsToAdd.Count > 0)
            {
                context.WowAuctions.AddRange(auctionsToAdd);
                context.SaveChanges();
            }


            // Commodity auctions do not need updates
            if (!tag.ToLower().Contains("commodities"))
            {
                // Listings in both incoming and stored are to be updated
                auctionsToUpdate = storedAuctions.Intersect(incoming).ToList();

                // Auctions that are stored but not in incoming listings are either sold or expired. Put in absentListings and then process
                absentListings = storedAuctions.Except(incoming).ToList();

                // Listings that are in absentListings and are not marked for SHORT or MEDIUM duration are sold. Put in soldListings and delete the others.

                foreach (WowAuction auction in absentListings)
                {
                    if (auction.TimeLeft.Equals(TimeLeft.VERY_LONG) || auction.TimeLeft.Equals(TimeLeft.LONG))
                    {
                        soldListings.Add(auction);
                    }
                    else
                    {
                        unsoldListings.Add(auction);
                    }
                }
                if (soldListings.Count > 0)
                {
                    soldListings.ForEach(l => l.Sold = true);  // is this good code?
                    context.WowAuctions.UpdateRange(soldListings);
                    context.WowAuctions.RemoveRange(unsoldListings);
                    context.SaveChanges();
                }


                // Sold auctions do not need any further updates
                auctionsToUpdate = auctionsToUpdate.Except(soldListings).ToList();

                // Stored listings that are still live are in auctionsToUpdate.  Update their timestamps and time left stamps.
                foreach (WowAuction auction in auctionsToUpdate)
                {
                    auction.LastSeenTime = DateTime.UtcNow;
                    auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);

                    // is there a better way to do timestamps???
                    auction.TimeLeft = incoming.FirstOrDefault(l => l.Equals(auction)).TimeLeft;

                }
                if (auctionsToUpdate.Count > 0)
                {
                    context.WowAuctions.UpdateRange(auctionsToUpdate);
                    context.SaveChanges();
                }
            }


            // Make a report of all changes.
            if (tag.ToLower().Contains("commodities"))
            {
                response = $"{auctionsToAdd.Count} auctions added and {ancientListings.Count} deleted. {tag} has {context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).Count()} live auctions";
            }
            else
            {
                int liveAuctionsCount = auctionsToAdd.Count + auctionsToUpdate.Count;
                if (ancientListings.Count == 0)
                {
                    response = $"{auctionsToAdd.Count} auctions added, {auctionsToUpdate.Count} updated and {unsoldListings.Count} deleted. {tag} has {liveAuctionsCount} live auctions";
                }
                else
                {
                    response = $"{auctionsToAdd.Count} auctions added, {auctionsToUpdate.Count} updated, {absentListings.Count} deleted and {ancientListings.Count} over 7 days old purged. {tag} has {liveAuctionsCount} live auctions";
                }
            }

            realm.LastScanTime = DateTime.UtcNow;
            context.WowRealms.Update(realm);
            context.SaveChanges();

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
                    //LogMaker.LogToTable($"DbUpdater", $"Blizzard API timeout");
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

