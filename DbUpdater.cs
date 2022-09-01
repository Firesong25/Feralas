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
        public async Task DoUpdatesAsync(string json, string tag)
        {
            Listings auctions = new();
            await auctions.CreateLists(json);

            PostgresContext context = new PostgresContext();

            await DbItemUpdaterAsync(context, auctions, tag);
            await DbAuctionsUpdaterAsync(auctions, tag);
        }

        public async Task DbAuctionsUpdaterAsync(Listings auctions, string tag)
        {

            await Task.Delay(1);
            List<WowAuction> incoming = auctions.LiveAuctions;
            List<WowAuction> storedAuctions = new();
            string PartitionKey = incoming.FirstOrDefault().PartitionKey;

            // the live dataset is less than 48 hours old, is not sold and is same realm
            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);

            using (PostgresContext postgresContext = new())
            {
                storedAuctions = postgresContext.WowAuctions.Where(l =>
                l.PartitionKey == PartitionKey &&
                l.Sold == false &&
                l.FirstSeenTime > cutOffTime).AsNoTracking().ToList();
            }

            List<WowAuction> auctionsToAdd = incoming.Except(storedAuctions).ToList();
            // set right now as last time the auction was seen
            foreach (WowAuction auction in auctionsToAdd)
            {
                auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            }

            List<WowAuction> auctionsToUpdate = incoming.Intersect(storedAuctions).ToList();

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
                LogMaker.Log($"We have {auctionsToAdd.Count} to add and {auctionsToUpdate.Count} auctions to update and {absentListings.Count} expired or sold auctions in the database for {tag}.");

                using (PostgresContext postgresContext = new())
                {
                    postgresContext.AddRange(auctionsToAdd);
                    await postgresContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                LogMaker.Log("_______________DbUpdater_______________");
                LogMaker.Log($"{ex.Message}");
                LogMaker.Log("_______________ADDING NEW AUCTIONS FAILED_______________");
                if (ex.InnerException.ToString() != null)
                {
                    LogMaker.Log($"{ex.InnerException}");
                }
                LogMaker.Log("_______________DbUpdater_______________");
            }
            try
            {
                LogMaker.Log($"Saving changes for {tag}.");
                if (auctionsToUpdate.Count > 150000)
                {
                    int iter = 0;
                    int numToTake = 10000;
                    List<List<WowAuction>> chunks = new();

                    while (iter < auctionsToUpdate.Count - numToTake)
                    {
                        int cnt = auctionsToUpdate.Count;
                        List<WowAuction> shortList = auctionsToUpdate.GetRange(iter, numToTake);
                        chunks.Add(shortList);
                        iter += numToTake;
                    }

                    if (chunks.Count > 0)
                    {
                        List<WowAuction> shortList = auctionsToUpdate.GetRange(iter, auctionsToUpdate.Count - iter);
                        chunks.Add(shortList);
                    }
                    iter = 0;

                    foreach (List<WowAuction> shortList in chunks)
                    {
                        using (PostgresContext postgresContext = new())
                        {
                            postgresContext.UpdateRange(shortList);
                            await postgresContext.SaveChangesAsync();
                        }
                        iter++;
                    }

                    LogMaker.Log($"Updated {auctionsToUpdate.Count} auction listings for {tag} in {iter} batches.");
                }
                else
                {
                    using (PostgresContext postgresContext = new())
                    {
                        postgresContext.UpdateRange(auctionsToUpdate);
                        await postgresContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogMaker.Log($"_______________DbUpdater_______________");
                LogMaker.Log($"{ex.Message}");
                LogMaker.Log("_______________UPDATE FOR AUCTIONS FAILED_______________");
                if (ex.InnerException.ToString() != null)
                {
                    LogMaker.Log($"_______________DbUpdater InnerException_______________");
                    LogMaker.Log($"{ex.InnerException}");
                }
                LogMaker.Log("_______________DbUpdater_______________");
            }

            try
            {
                LogMaker.Log($"Marking expired auctions for {tag}.");
                if (absentListings.Count > 150000)
                {
                    int iter = 0;
                    int numToTake = 10000;
                    List<List<WowAuction>> chunks = new();

                    while (iter < auctionsToUpdate.Count - numToTake)
                    {
                        int cnt = auctionsToUpdate.Count;
                        List<WowAuction> shortList = auctionsToUpdate.GetRange(iter, numToTake);
                        chunks.Add(shortList);
                        iter += numToTake;
                    }

                    if (chunks.Count > 0)
                    {
                        List<WowAuction> shortList = auctionsToUpdate.GetRange(iter, auctionsToUpdate.Count - iter);
                        chunks.Add(shortList);
                    }
                    iter = 0;

                    foreach (List<WowAuction> shortList in chunks)
                    {
                        using (PostgresContext postgresContext = new())
                        {
                            postgresContext.UpdateRange(shortList);
                            await postgresContext.SaveChangesAsync();
                            iter++;
                        }
                    }

                    LogMaker.Log($"Updated {auctionsToUpdate.Count} auction listings for {tag} in {iter} batches.");
                }
                else
                {
                    using (PostgresContext postgresContext = new())
                    {
                        postgresContext.UpdateRange(absentListings);
                        await postgresContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogMaker.Log($"_______________DbUpdater_______________");
                LogMaker.Log($"{ex.Message}");
                LogMaker.Log("_______________UPDATE FOR EXPIRED AUCTIONS FAILED_______________");
                if (ex.InnerException.ToString() != null)
                {
                    LogMaker.Log($"{ex.InnerException}");
                }
                LogMaker.Log("_______________DbUpdater_______________");
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
    }
}
