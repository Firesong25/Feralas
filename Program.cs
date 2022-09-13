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

            int count = 150;
            int pollingInterval = 1;
            int z = 0;

            LogMaker.LogToTable("Cleardragon", $"Auctions scans for all realms starting.");

            // Test area

            //LogMaker.LogToTable($"Program.cs", $"Delete This");
            //PostgresContext context = new PostgresContext();
            //WowRealm runeTotem = context.WowRealms.FirstOrDefault(l => l.Name.Equals("Runetotem"));
            //RealmRunner runner = new(runeTotem);
            //await runner.Run();
            //return;
            //DELETE UNTIL THIS

            List<WowRealm> activeRealms = await CreateActiveRealmList(count);
            Stopwatch sw = new();

            while (true)
            {
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
                        await Task.Delay(new TimeSpan(0, pollingInterval * 2, 0));
                    }
                    else
                    {
                        await Task.Delay(new TimeSpan(0, pollingInterval, 0));
                    }                    
                }

                z++;
                LogMaker.LogToTable("Cleardragon", $"Auctions scan {z} complete.");
            }
        }

        public static async Task<List<WowRealm>> CreateActiveRealmList(int count)
        {
            await Task.Delay(1);
            PostgresContext context = new();
            List<WowRealm> stored = context.WowRealms.ToList();
            List<WowRealm> active = new();



            List<WowRealm> usRealms = stored.Where(l => l.WowNamespace.Contains("-us") && l.ConnectedRealmId > 0).OrderBy(l => l.Id).ToList();
            List<WowRealm> euRealms = stored.Where(l => l.WowNamespace.Contains("-eu") && l.ConnectedRealmId > 0).OrderBy(l => l.Id).ToList();

            double half = count / 2;

            int usCount = (int)Math.Floor(half);

            WowRealm usCommodities = new();
            usCommodities.Name = "Commodities";
            usCommodities.WowNamespace = "dynamic-us";
            usCommodities.ConnectedRealmId = 12345;
            usCommodities.Id = 12345;
            usRealms.Add(usCommodities);

            foreach (WowRealm realm in usRealms.OrderBy(l => l.Name))
            {
                WowRealm testy = active.FirstOrDefault(l => l.ConnectedRealmId == (int)realm.ConnectedRealmId);
                if (testy == null)
                {
                    active.Add(realm);
                }
                if (active.Count >= half)
                {
                    break;
                }
            }

            WowRealm euCommodities = new();
            euCommodities.Name = "Commodities";
            euCommodities.WowNamespace = "dynamic-eu";
            euCommodities.ConnectedRealmId = 54321;
            euCommodities.Id = 54321;
            euRealms.Add(euCommodities);

            foreach (WowRealm realm in euRealms.OrderBy(l => l.Name))
            {
                WowRealm testy = active.FirstOrDefault(l => l.ConnectedRealmId == (int)realm.ConnectedRealmId);
                if (testy == null)
                {
                    active.Add((WowRealm)realm);
                }

                if (active.Count >= count)
                {
                    break;
                }
            }

            return active.ToList();


        }
    }
}