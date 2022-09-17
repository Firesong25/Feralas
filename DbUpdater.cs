using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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

            int estimatedSoldCutoff = 36;

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
             * 7. Stored listings that have had suration less than 36 hours are assumed to have been sold.  Put in soldlistings and update stored records.
             * 7. Stored listings that are in absentListings and marked SHORT are expired. Delete them.
             * 8. Stored listings that are still live are in auctionsToUpdate. Update their timestamps.
             * 9. Make a report of all changes.
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


            // Listings that have not been seen before are timestamped and stored in auctionsToAdd
            auctionsToAdd = incoming.Except(storedAuctions).ToList();
            foreach (WowAuction auction in auctionsToAdd)
            {
                auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
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

                int stlTest = auctionsToUpdate.Where(l => l.ShortTimeLeftSeen == true).Count();

                // Auctions that are stored but not in incoming listings are either sold or expired. Put in absentListings and then process
                absentListings = storedAuctions.Except(incoming).ToList();

                // Listings that are in absentListings and are not marked for SHORT duration are sold. Put in soldListings and update stored records.
                soldListings = absentListings.Where(l => l.ShortTimeLeftSeen == false).ToList();

                foreach (WowAuction auction in absentListings)
                {
                    TimeSpan duration = auction.LastSeenTime - auction.FirstSeenTime;
                    if (duration.Hours < estimatedSoldCutoff)
                    {
                        soldListings.Add(auction);
                    }
                }
                if (soldListings.Count > 0)
                {
                    soldListings.ForEach(l => l.Sold = true);  // is this good code?
                    context.WowAuctions.UpdateRange(soldListings);
                    context.SaveChanges();
                }


                // Stored listings that are in absentListings and marked SHORT are expired. Delete them.
                List<WowAuction> deleteTheseAbsentListings = absentListings.Where(l => l.ShortTimeLeftSeen == true).ToList();
                if (deleteTheseAbsentListings.Count > 0)
                {
                    context.WowAuctions.RemoveRange(deleteTheseAbsentListings);
                    context.SaveChanges();
                }


                // Sold auctions do not need any further updates
                auctionsToUpdate = auctionsToUpdate.Except(soldListings).ToList();

                // Stored listings that are still live are in auctionsToUpdate.  Update their timestamps.
                foreach (WowAuction auction in auctionsToUpdate)
                {
                    auction.LastSeenTime = DateTime.UtcNow;
                    auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
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
                absentListings = absentListings.Except(soldListings).ToList();
                if (ancientListings.Count == 0)
                {
                    response = $"{auctionsToAdd.Count} auctions added, {auctionsToUpdate.Count} updated and {absentListings.Count} deleted. {tag} has {context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).Count()} live auctions";
                }
                else
                {
                    int deletedCount = absentListings.Count + ancientListings.Count;
                    response = $"{auctionsToAdd.Count} auctions added, {auctionsToUpdate.Count} updated, {absentListings.Count} deleted and {ancientListings.Count} over 48 hours old purged. {tag} has {context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).Count()} live auctions";
                }
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

