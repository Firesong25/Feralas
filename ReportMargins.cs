﻿using System;
using System.Diagnostics;

namespace Feralas;

internal class ReportMargins
{

    public async Task PopulateEuCommodityPrices(PostgresContext context)
    {
        await Task.Delay(1);
        Dictionary<int, long> tmp = new();
        foreach (WowAuction auction in CachedData.EuCommodities)
        {
            if (tmp.ContainsKey(auction.ItemId))
            {
                continue;
            }
            long price = await GetRealPrice(auction.ItemId, CachedData.EuCommodities);
            tmp.Add(auction.ItemId, price);
        }

        CachedData.EuCommodityPrices = tmp;
        await GetMarginReportForCommodities(context, "eu");
    }

    public async Task PopulateUsCommodityPrices(PostgresContext context)
    {
        await Task.Delay(1);
        Dictionary<int, long> tmp = new();
        foreach (WowAuction auction in CachedData.UsCommodities)
        {
            if (tmp.ContainsKey(auction.ItemId))
            {
                continue;
            }
            long price = await GetRealPrice(auction.ItemId, CachedData.UsCommodities);
            tmp.Add(auction.ItemId, price);
        }

        CachedData.UsCommodityPrices = tmp;

        await GetMarginReportForCommodities(context, "us");
         
    }

    public async Task GetMarginReportForCommodities(PostgresContext context, string zone)
    {
        List<MarginReport> reports = new();
        Stopwatch sw = Stopwatch.StartNew();
        foreach (CraftedItem item in CachedData.CommodityItems)
        {
            MarginReport mr = new();
            if (zone.Equals("eu"))
            {
                mr = await GetMargin(item, CachedData.EuCommodities, zone);
            }
            else
            {
                mr = await GetMargin(item, CachedData.UsCommodities, zone);
            }
            
            reports.Add(mr);
        }
        context.MarginReports.AddRange(reports);

        LogMaker.LogToTable($"ReportMargins.cs", $"{reports.Count} {zone} commodity margin reports took {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        await context.SaveChangesAsync();
    }
    public async Task GetMarginReportsForRealm(PostgresContext context, List<WowAuction> auctions)
    {
        WowRealm realm = CachedData.Realms.FirstOrDefault(l => l.ConnectedRealmId.Equals(auctions.FirstOrDefault().ConnectedRealmId));

        string zone = "eu";
        if (realm.WowNamespace.Contains("-us"))
        {
            zone = "us";
        }

        List<MarginReport> reports = new();
        Stopwatch sw = Stopwatch.StartNew();
        foreach (CraftedItem item in CachedData.RealmItems)
        {
            MarginReport mr = await GetMargin(item, auctions, zone);
            reports.Add(mr);
        }
        context.MarginReports.AddRange(reports);

        LogMaker.LogToTable($"ReportMargins.cs", $"{reports.Count} margin reports took {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
        await context.SaveChangesAsync();
    }
    public async Task<MarginReport> GetMargin(CraftedItem item, List<WowAuction> auctions, string zone)
    {
        int totalListings = 0;
        foreach (WowAuction auction in auctions.Where(l => l.ItemId == item.Id))
        {
            totalListings += auction.Quantity;
        }

        long medianPrice = await GetRealPrice(item.Id, auctions) / item.CraftedQuantity;

        Dictionary<int, int> costsDictionary = await GetsMatsDictionary(item.Id);

        long totalCost = 0;

        Stopwatch sw = Stopwatch.StartNew();
        foreach (KeyValuePair<int, int> pair in costsDictionary)
        {
            if (CachedData.VendorItemCosts.ContainsKey(pair.Key))
            {
                totalCost += CachedData.VendorItemCosts[pair.Key] * pair.Value;
            }
            else if (zone.Equals("eu") && CachedData.EuCommodityPrices.ContainsKey(pair.Key))
            {
                totalCost += CachedData.EuCommodityPrices[pair.Key] * pair.Value;
            }
            else if (zone.Equals("us") && CachedData.UsCommodityPrices.ContainsKey(pair.Key))
            {
                totalCost += CachedData.UsCommodityPrices[pair.Key] * pair.Value;
            }
            else
            {
                long matCost = await GetRealPrice(pair.Key, auctions);
                totalCost += matCost * pair.Value;
            }
        }


        MarginReport pf = new();
        pf.ConnectedRealmId = auctions.FirstOrDefault().ConnectedRealmId;
        pf.ItemId = item.Id;
        pf.ProfessionId = item.ProfessionId;
        pf.SalesPrice = medianPrice;
        pf.CostsPrice = totalCost / item.CraftedQuantity;
        pf.Volume48Hours = totalListings;


        await Task.Delay(1);
        return pf;
    }
    public async Task<Dictionary<int, int>> GetsMatsDictionary(int itemId)
    {
        List<Reagent> reagents = CachedData.Reagents.Where(l => l.ItemForeignKey.Equals(itemId)).ToList();

        Dictionary<int, int> matsDictionary = new();

        foreach (Reagent reagent in reagents)
        {
            matsDictionary.Add(reagent.ItemId, reagent.Quantity);
        }

        await Task.Delay(1);
        return matsDictionary;
    }

    public async Task<long> GetRealPrice(int itemId, List<WowAuction> auctions)
    {
        long price = 0;
        int count = 0;
        double priceAverage = 0;
        long priceTotal = 0;
        List<WowAuction> itemListings = auctions.Where(l => l.ItemId.Equals(itemId)).OrderBy(l => l.UnitPrice).Take(10).ToList();
        if (itemListings.Count > 0)
        {
            foreach (WowAuction auction in itemListings)
            {
                count += auction.Quantity;
                priceTotal += auction.UnitPrice * auction.Quantity;
            }
            priceAverage = priceTotal / count;
            price = Convert.ToInt64(Math.Round(priceAverage));
        }
        await Task.Delay(1);
        return price;
    }
}
