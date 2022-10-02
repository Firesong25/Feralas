using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Feralas;

internal class Program
{
    static async Task Main(string[] args)
    {
        if (File.Exists("log.html"))
            File.Delete("log.html");

        await Configurations.Init();
        PostgresContext context = new PostgresContext();
        await CachedData.Init(context);
        ReportMargins reporter = new();
        Stopwatch sw = Stopwatch.StartNew();

        LogMaker.LogToTable($"Feralas", $"Initialising.");
        await reporter.PopulateEuCommodityPrices(context);
        await reporter.PopulateUsCommodityPrices(context);

        TimeSpan pollingInterval = new(0, 0, 15);
        int z = 0;


        List<WowRealm> activeRealms = await CreateActiveRealmList();

        // Test area

        //  LogMaker.LogToTable($"Program.cs", $"Delete This");



        //DELETE UNTIL THIS

        LogMaker.LogToTable($"Feralas", $"Initialisation took {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");

        LogMaker.LogToTable("Feralas", $"<em>Auctions scans for {activeRealms.Count} realms starting.</em>");


        while (true)
        {
            sw.Restart();
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isLinux)
            {
                var freeBytes = new DriveInfo("/var/lib/postgresql").AvailableFreeSpace;
                if (freeBytes / 1000000000 < 5)
                {
                    LogMaker.LogToTable("Feralas", "We are out of disk space. Stopping execution.");
                    Environment.Exit(-1);
                }
            }

            foreach (WowRealm realm in activeRealms)
            {
                RealmRunner realmRunner = new(realm);
                try
                {
                    if (realm.Name.ToLower().Contains("commodities"))
                    {
                        await realmRunner.Run();
                        await Task.Delay(pollingInterval);
                    }
                    else
                    {
                        _ = realmRunner.Run();
                        await Task.Delay(pollingInterval);
                    }
                }
                catch (Exception ex)
                {
                    LogMaker.LogToTable($"{realm.Name}", $"{ex.Message}");
                    LogMaker.LogToTable($"{realm.Name}", $"{ex.StackTrace}");  
                }

            }

            await reporter.PopulateEuCommodityPrices(context);
            await reporter.PopulateUsCommodityPrices(context);

            z++;
            LogMaker.LogToTable("Feralas", $"<em>Auctions scan {z} complete in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.</em>");

        }
    }
    public static async Task<List<WowRealm>> CreateActiveRealmList()
    {
        await Task.Delay(1);  // stop compiler warnings on Linux
        List<WowRealm> activeRealms = new();
        foreach (WowRealm realm in CachedData.Realms)
        {
            WowRealm trial = activeRealms.FirstOrDefault(l => l.ConnectedRealmId == realm.ConnectedRealmId);
            if (trial == null)
            {
                activeRealms.Add(realm);
            }
        }

        return activeRealms.OrderBy(l => l.WowNamespace).ThenBy(l => l.Name).ToList();
    }

}