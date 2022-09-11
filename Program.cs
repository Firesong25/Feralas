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

            RealmRunner realmRunner = new("", "");

            // Test area
            //            LogMaker.Log($"DELETE THIS!");


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

                // divide realms into 2 batches and process the big commodity runs in between
                int divider = 0;
                if (activeRealms.Count % 2 == 0)
                {
                    divider = activeRealms.Count / 2;
                }
                else
                {
                    divider = (activeRealms.Count - 1) / 2;
                }

                realmRunner = new("Commodities", "dynamic-us");
                backgroundTask = realmRunner.Run();
                //commodities runs take twice as long
                await Task.Delay(new TimeSpan(0, pollingInterval * 2, 0));


                foreach (WowRealm realm in activeRealms.GetRange(0, divider))
                {
                    realmRunner = new(realm.Name, realm.WowNamespace);
                    backgroundTask = realmRunner.Run();
                    await Task.Delay(new TimeSpan(0, pollingInterval, 0));
                }

                realmRunner = new("Commodities", "dynamic-eu");
                backgroundTask = realmRunner.Run();
                await Task.Delay(new TimeSpan(0, pollingInterval * 2, 0));

                foreach (WowRealm realm in activeRealms.GetRange(divider, activeRealms.Count - divider))
                {
                    realmRunner = new(realm.Name, realm.WowNamespace);
                    backgroundTask = realmRunner.Run();
                    await Task.Delay(new TimeSpan(0, pollingInterval, 0));
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
            //I can't abandon the test realms for which I've already got data
            WowRealm nordrassil = stored.FirstOrDefault(l => l.Name.ToLower() == "nordrassil" && l.WowNamespace.Contains("-eu"));
            active.Add(nordrassil);
            WowRealm kazzak = stored.FirstOrDefault(l => l.Id == 1305);
            active.Add(kazzak);
            WowRealm illidan = stored.FirstOrDefault(l => l.Name.ToLower() == "illidan" && l.WowNamespace.Contains("-us"));
            active.Add(illidan);
            WowRealm anvilmar = stored.FirstOrDefault(l => l.Name.ToLower() == "anvilmar" && l.WowNamespace.Contains("-us"));
            active.Add(anvilmar);

            List<WowRealm> usRealms = stored.Where(l => l.WowNamespace.Contains("-us") && l.ConnectedRealmId > 0).OrderBy(l => l.Id).ToList();
            List<WowRealm> euRealms = stored.Where(l => l.WowNamespace.Contains("-eu") && l.ConnectedRealmId > 0).OrderBy(l => l.Id).ToList();

            double half = count / 2;

            int usCount = (int)Math.Floor(half);

            foreach (WowRealm realm in usRealms)
            {
                WowRealm testy = active.FirstOrDefault(l => l.ConnectedRealmId == (int)realm.ConnectedRealmId);
                if (testy == null)
                {
                    active.Add((WowRealm)realm);
                }
                if (active.Count >= half)
                {
                    break;
                }
            }

            foreach (WowRealm realm in euRealms)
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

            return active.OrderBy(l => l.Name).ToList();


        }
    }
}