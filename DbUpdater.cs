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
#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Starting database update for {realm.Name} in {realm.WowNamespace}.");
#endif
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

#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Got storedAuctions in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();

#endif
        // use this to add the sold listings to the margin report calcuations.
        List<WowAuction> reportables = storedAuctions.Where(l => l.Sold.Equals(true)).ToList();


        ancientListings = context.WowAuctions.Where(l => l.ConnectedRealmId == realm.ConnectedRealmId && l.FirstSeenTime < ancientDeleteTime).ToList();

#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Got ancientListings in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();

#endif
        if (ancientListings.Count > 0)
        {
            context.WowAuctions.RemoveRange(ancientListings);
            context.SaveChanges();
        }

#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Deleted ancientListings in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
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

        if (auctionsToAdd.Count > 0)
        {
            context.WowAuctions.AddRange(auctionsToAdd);
            context.SaveChanges();
        }

#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Inserted auctionsToAdd in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();
#endif

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
            context.SaveChanges();
            reportables.AddRange(soldListings);
        }
#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Updated soldListings in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();
#endif

        unsoldListings = absentListings.Except(soldListings).ToList();
        if (unsoldListings.Count > 0)
        {
            context.WowAuctions.RemoveRange(unsoldListings);
            context.SaveChanges();
        }
#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Deleted unsoldListings in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();
#endif
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
            context.SaveChanges();
        }
#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Updated auctionsToUpdate in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();
#endif

#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Completed listings database for {realm.Name} in {realm.WowNamespace}.");

#endif

        if (reportables.Count > 0)
        {
            ReportMargins reporter = new();
            await reporter.GetMarginReportsForScan(context, reportables, tag);
        }


#if DEBUG
        LogMaker.LogToTable($"{realm.Name}", $"Margin reports done for {realm.Name} in {realm.WowNamespace} in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        sw.Restart();
#endif

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
            response = $"{auctionsToAdd.Count} new listings added. {auctionsToUpdate.Count} updated. {absentListings.Count} expired listings deleted. {ancientListings.Count} sold auctions over 7 days old purged. {auctionCount} live auctions";
            ur.ScanReport = response;
            ur.LastScanTime = DateTime.UtcNow;
        }

        if (auctionsToAdd.Count > 0 || updatedRealms.Count > 0)
        {
            context.WowRealms.UpdateRange(updatedRealms);
            sw.Restart();
            try
            {
                context.SaveChanges();
#if DEBUG
                LogMaker.LogToTable($"{realm.Name}", $"Completed margins and listings database update for {realm.Name} in {realm.WowNamespace}.");
#endif

                //Task background = context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response = $"<b>Error when saving data for {tag}</b>";
#if DEBUG
                LogMaker.LogToTable($"{realm.Name}", $"{ex.Message}.");
#endif
            }

        }

        return response;
    }
}

