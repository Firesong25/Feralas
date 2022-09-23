using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Feralas
{
    public class DbUpdater
    {
        public async Task<string> DoUpdatesAsync(WowRealm realm, string json, string tag)
        {
            PostgresContext context = new PostgresContext();

            int cid = await WowApi.GetConnectedRealmId(realm.WowNamespace, realm.RealmSlug);

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
            string response = await DbAuctionsUpdaterAsync(context, realm, auctions, tag);
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

            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            DateTime ancientDeleteTime = DateTime.UtcNow - new TimeSpan(7, 0, 0, 0);

            storedAuctions = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).ToList();
#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to get {storedAuctions.Count} stored auctions.");
            sw.Restart();
#endif

            ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();
#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to get {ancientListings.Count} ancient listings.");
            sw.Restart();
#endif

            if (ancientListings.Count > 0)
            {
                context.WowAuctions.RemoveRange(ancientListings);
                context.SaveChanges();
            }
#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to delete {ancientListings.Count}. ancient listings.");
            sw.Restart();
#endif

            auctionsToAdd = incoming.Except(storedAuctions).ToList();

            Parallel.ForEach(auctionsToAdd, auction => 
            {
                auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
                auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            });

#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to add {auctionsToAdd.Count} new listings in with parallel.foreach.");
            sw.Restart();
#endif

            if (auctionsToAdd.Count > 0)
            {
                context.WowAuctions.AddRange(auctionsToAdd);
                try
                {
                    context.SaveChanges();
                }
                catch
                {
                    LogMaker.LogToTable($"{tag}", $"Exception adding {auctionsToAdd.Count} new listings.");
                }
            }


#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to add {auctionsToAdd.Count} new listings.");
            sw.Restart();
#endif

            auctionsToUpdate = storedAuctions.Intersect(incoming).ToList();

            absentListings = storedAuctions.Except(incoming).ToList();

            Parallel.ForEach(absentListings, auction => // 5 times faster than normal foreach
            {
                if (auction.TimeLeft.Equals(TimeLeft.VERY_LONG) || auction.TimeLeft.Equals(TimeLeft.LONG))
                {
                    soldListings.Add(auction);
                }
                else
                {
                    unsoldListings.Add(auction);
                }
            });
#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to process {absentListings.Count} listings in parallel.");
            sw.Restart();
#endif

            if (soldListings.Count > 0)
            {
                soldListings.ForEach(l => l.Sold = true); 
                context.WowAuctions.UpdateRange(soldListings);
                context.WowAuctions.RemoveRange(unsoldListings);
                try
                {
                    context.SaveChanges();
                }
                catch
                {
                    LogMaker.LogToTable($"{tag}", $"Exception handling {absentListings.Count} absent listings.");
                }

            }
#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to process {absentListings.Count} absent listings.");
            sw.Restart();
#endif

            auctionsToUpdate = auctionsToUpdate.Except(soldListings).ToList();

            Parallel.ForEach(auctionsToUpdate, auction => 
            {
                auction.LastSeenTime = DateTime.UtcNow;
                auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
                auction.TimeLeft = incoming.FirstOrDefault(l => l.Equals(auction)).TimeLeft;
            });

#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to update {auctionsToUpdate.Count} listings in parallel.");
            sw.Restart();
#endif
            if (auctionsToUpdate.Count > 0)
            {
                context.WowAuctions.UpdateRange(auctionsToUpdate);
                try
                {
                    context.SaveChanges();
                }
                catch
                {
                    LogMaker.LogToTable($"{tag}", $"Exception saving updates to {auctionsToUpdate.Count} listings.");
                }


            }
#if DEBUG

            LogMaker.LogToTable($"{tag}", $"{RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)} to update {auctionsToUpdate.Count} listings in the database.");
            sw.Restart();
#endif

            List<WowRealm> updatedRealms = context.WowRealms.Where(l => l.ConnectedRealmId.Equals(realm.ConnectedRealmId)).ToList();
            int auctionCount = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId &&
                l.Sold == false &&
                l.FirstSeenTime > cutOffTime).
                Count();

            foreach (WowRealm ur in updatedRealms)
            {

                string idTag = $"{ur.Name} US";
                if (ur.WowNamespace.Contains("-eu"))
                    idTag = $"{ur.Name} EU";
                response = $"{auctionsToAdd.Count} auctions added, {auctionsToUpdate.Count} updated, {absentListings.Count} deleted and {ancientListings.Count} over 7 days old purged. {idTag} has {auctionCount} live auctions";

                ur.ScanReport = response;
                ur.LastScanTime = DateTime.UtcNow;
            }

            if (updatedRealms.Count > 0)
            {
                context.WowRealms.UpdateRange(updatedRealms);
                context.SaveChanges();
            }

            return response;
        }
    }
}

