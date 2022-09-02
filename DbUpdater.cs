using Microsoft.EntityFrameworkCore;

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

            using (PostgresContext postgresContext = new())
            {
                try
                {
                    LogMaker.Log($"{tag}: Saving {auctionsToAdd.Count} auctions to add, {auctionsToUpdate.Count} auctions to update and {absentListings.Count} expired or sold auctions.");

                    // new auctions added
                    postgresContext.AddRange(auctionsToAdd);
                    await postgresContext.SaveChangesAsync();
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
                    // updating auctions
                    postgresContext.UpdateRange(auctionsToUpdate);
                    await postgresContext.SaveChangesAsync();
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
                    // marking sold auctions
                    postgresContext.UpdateRange(absentListings);
                    await postgresContext.SaveChangesAsync();
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
