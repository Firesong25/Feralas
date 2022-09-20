using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Feralas
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (File.Exists("log.html"))
                File.Delete("log.html");

            await Configurations.Init();
            Task backgroundTask;
            TimeSpan pollingInterval = new(0, 0, 30);
            int z = 0;

            List<WowRealm> activeRealms = await CreateActiveRealmList();

            // Test area

            //LogMaker.LogToTable($"Program.cs", $"Delete This");
            //PostgresContext context = new PostgresContext();


            //DELETE UNTIL THIS

            LogMaker.LogToTable("Cleardragon", $"<b><em>Auctions scans for {activeRealms.Count} realms starting.</em></b>");
            Stopwatch sw = new();

            while (true)
            {
                sw.Restart();
                bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                if (isLinux)
                {
                    var freeBytes = new DriveInfo("/var/lib/postgresql").AvailableFreeSpace;
                    if (freeBytes / 1000000000 < 5)
                    {
                        LogMaker.LogToTable("Cleardragon", "We are out of disk space. Stopping execution.");
                        Environment.Exit(-1);
                    }
                }


                foreach (WowRealm realm in activeRealms)
                {
                    RealmRunner realmRunner = new(realm);
                    backgroundTask = realmRunner.Run();
                    if (realm.Name.ToLower().Contains("commodities"))
                    {
                        await Task.Delay(pollingInterval * 4);
                    }
                    else
                    {
                        await Task.Delay(pollingInterval);
                    }
                }

                z++;
                LogMaker.LogToTable("Cleardragon", $"<b><em>Auctions scan {z} complete in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.</em></b>");
            }
        }

        public static async Task<List<WowRealm>> CreateActiveRealmList()
        {
            await Task.Delay(1);  // stop compiler warnings on Linux
            PostgresContext context = new();
            List<WowRealm> allRealms = context.WowRealms.ToList();
            List<WowRealm> activeRealms = new();
            foreach (WowRealm realm in allRealms)
            {
                WowRealm trial = activeRealms.FirstOrDefault(l => l.ConnectedRealmId == realm.ConnectedRealmId);
                if (trial == null)
                {
                    activeRealms.Add(realm);
                }
            }

            //WowRealm usCommodities = new();
            //usCommodities.Name = "Commodities";
            //usCommodities.WowNamespace = "dynamic-us";
            //usCommodities.ConnectedRealmId = 12345;
            //usCommodities.Id = 12345;
            //activeRealms.Add(usCommodities);

            //WowRealm euCommodities = new();
            //euCommodities.Name = "Commodities";
            //euCommodities.WowNamespace = "dynamic-eu";
            //euCommodities.ConnectedRealmId = 54321;
            //euCommodities.Id = 54321;
            //activeRealms.Add(euCommodities);

            return activeRealms.OrderBy(l => l.WowNamespace).ThenBy(l => l.Name).ToList();
        }
    }
}