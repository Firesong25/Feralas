using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Feralas;
public class DbUpdater
{
    public async Task<string> DoUpdatesAsync(WowRealm realm, string json, string tag)
    {
        PostgresContext context = new();

        int cid = await WowApi.GetConnectedRealmId(realm.WowNamespace, realm.RealmSlug);

        if (cid != 0 && cid != (int)realm.ConnectedRealmId)
        {
            LogMaker.LogToTable($"IMPORTANT", $"{realm.Name} has changed connected realm id from {realm.ConnectedRealmId} to {cid}.");
            WowRealm brokenRealm = context.WowRealms.FirstOrDefault(l => l.Id == realm.Id);
            brokenRealm.ConnectedRealmId = cid;
            context.Update(brokenRealm);
            try
            {
                Task background = context.SaveChangesAsync();
            }
            catch { LogMaker.LogToTable($"IMPORTANT", $"<b>{realm.Name} connected realm id update FAILED.</b>"); }
            
        }

        Listings auctions = new();
        await auctions.CreateLists(context, realm, json, tag);

        context.Dispose();

        string response = string.Empty;
        if (auctions.LiveAuctions.Count > 0)
        {
            response = await DbAuctionsUpdaterAsync(realm, auctions, tag);
        }

        
        return response;
    }

    public async Task<string> DbAuctionsUpdaterAsync(WowRealm realm, Listings auctions, string tag)
    {
        PostgresContext context = new();
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

        // October 1 - increasing list size to include sold items for margin reports to be accurate.
        storedAuctions = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.FirstSeenTime > cutOffTime).ToList();
        // use this to add the sold listings to the margin report calcuations.
        List<WowAuction> reportables = storedAuctions.Where(l => l.Sold.Equals(true)).ToList();


        ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();
        if (ancientListings.Count > 0)
        {
            context.WowAuctions.RemoveRange(ancientListings);
        }

        auctionsToAdd = incoming.Except(storedAuctions).ToList();

        Parallel.ForEach(auctionsToAdd, auction => 
        {
            auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(0, 5, 0);
            auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
            auction.LastSeenTime = DateTime.UtcNow;
            auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
        });

        if (auctionsToAdd.Count > 0)
        {
            context.WowAuctions.AddRange(auctionsToAdd);
        }

        

        // October 1 - exclude the extra sold listings
        absentListings = storedAuctions.Where(l => l.Sold.Equals(false)).Except(incoming).ToList();

        foreach (WowAuction auction in absentListings)
        {
            if (auction.TimeLeft.Equals(TimeLeft.VERY_LONG) || auction.TimeLeft.Equals(TimeLeft.LONG))
            {
                soldListings.Add(auction);
            }
        }

        if (soldListings.Count > 0)
        {
            soldListings.ForEach(l => l.Sold = true);
            context.WowAuctions.UpdateRange(soldListings);
            reportables.AddRange(soldListings);
            ReportMargins reporter = new();
            Task backgroundReporter = reporter.GetMarginReportsForScan(context, reportables, tag);
        }

        unsoldListings = absentListings.Except(soldListings).ToList();
        if (unsoldListings.Count > 0)
        {
            context.WowAuctions.RemoveRange(unsoldListings);
        }

        // October 1 - the except must go before the intersect for performance
        auctionsToUpdate = storedAuctions.Except(soldListings).Intersect(incoming).ToList();

        Parallel.ForEach(auctionsToUpdate, auction => 
        {
            auction.LastSeenTime = DateTime.UtcNow;
            auction.LastSeenTime = DateTime.SpecifyKind(auction.LastSeenTime, DateTimeKind.Utc);
            auction.TimeLeft = incoming.FirstOrDefault(l => l.Equals(auction)).TimeLeft;
        });

        

        if (auctionsToUpdate.Count > 0)
        {
            context.WowAuctions.UpdateRange(auctionsToUpdate); 
        }

        // all work done now store the results
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
            response = $"{auctionsToAdd.Count} added. {auctionsToUpdate.Count} updated. {absentListings.Count} deleted. {ancientListings.Count} purged. {auctionCount} live auctions";
            ur.ScanReport = response;
            ur.LastScanTime = DateTime.UtcNow;
        }

        if (updatedRealms.Count > 0)
        {
            context.WowRealms.UpdateRange(updatedRealms);
            sw.Restart();
            try
            {
                Task background = context.SaveChangesAsync();
            }
            catch
            {
                response = $"<b>Error when saving data for {tag}</b>";
            }
            
        }

        return response;
    }
}

