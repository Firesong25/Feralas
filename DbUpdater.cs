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

            // the live dataset is less than 48 hours old, is not sold and is same realm
            DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(50, 50, 50);
            DateTime ancientDeleteTime = DateTime.UtcNow - new TimeSpan(7, 0, 0, 0);

            // We only work with stored listings that are less than 48 hours old in the same realm group which are in storedAuctions
            storedAuctions = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.Sold == false && l.FirstSeenTime > cutOffTime).ToList();

            // Listings that are over 7 days old are in the ancientListings list and deleted


            ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();

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
                if (auction.UnitPrice == 0 && auction.Buyout > 0)
                {
                    auction.UnitPrice = auction.Buyout;
                }
            }

            if (auctionsToAdd.Count > 0)
            {
                context.WowAuctions.AddRange(auctionsToAdd);
                context.SaveChanges();
            }

            if (tag.ToLower().Contains("commodities"))
            {
                // Remove absent commodity listings
                absentListings = storedAuctions.Except(incoming).ToList();
                context.WowAuctions.RemoveRange(absentListings);
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
                    auction.TimeLeft = incoming.FirstOrDefault(l => l.Equals(auction)).TimeLeft;

                }
                if (auctionsToUpdate.Count > 0)
                {
                    context.WowAuctions.UpdateRange(auctionsToUpdate);
                    context.SaveChanges();
                }
            }           


            // Make a report of all changes.
            List<WowRealm> updatedRealms = context.WowRealms.Where(l => l.ConnectedRealmId.Equals(realm.ConnectedRealmId)).ToList();

            foreach (WowRealm ur in updatedRealms)
            {
                string idTag = $"{ur.Name} US";
                if (ur.WowNamespace.Contains("-eu"))
                    idTag = $"{ur.Name} EU";
                int liveAuctionsCount = auctionsToAdd.Count + auctionsToUpdate.Count;
                response = $"{auctionsToAdd.Count} auctions added, {auctionsToUpdate.Count} updated, {absentListings.Count} deleted and {ancientListings.Count} over 7 days old purged. {idTag} has {liveAuctionsCount} live auctions";

                if (ur.Name.Contains("Commodities"))
                {
                    response = $"There are {incoming.Count} auctions for {tag}.";
                }

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

