using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Feralas
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (File.Exists("log.html"))
                File.Delete("log.html");

            await Configurations.Init();

            int count = 10;
            int pollingInterval = 3;
            int y = 0;
            int z = 0;

            LogMaker.LogToTable("Cleardragon", $"Auctions scans for {count} realms starting.");

            RealmRunner commoditiesUs = new("Commodities", "dynamic-us");
            RealmRunner commoditiesEu = new("Commodities", "dynamic-eu");


            // Test area
            //LogMaker.Log($"DELETE AFTER THIS");

            //LogMaker.Log($"DELETE UNTIL THIS");

            List<WowRealm> activeRealms = await CreateActiveRealmList(count);
            RealmRunner realmRunner = new("", "");

            while (true)
            {
                foreach (WowRealm realm in activeRealms)
                {
                    realmRunner = new(realm.Name, realm.WowNamespace);
                    realmRunner.Run();
                    await Task.Delay(new TimeSpan(0, pollingInterval, 0));
                }

                realmRunner = new("Commodities", "dynamic-us");
                realmRunner.Run();
                //commodities runs take twice as long
                await Task.Delay(new TimeSpan(0, pollingInterval * 2, 0));
                realmRunner = new("Commodities", "dynamic-eu");
                realmRunner.Run();
                await Task.Delay(new TimeSpan(0, pollingInterval * 2, 0));

                z++;
                LogMaker.LogToTable("Cleardragon", $"Auctions scan {z} complete.");

            }

            LogMaker.Log($"If you see this, something has gone terribly wrong.");

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

            return active;


        }
    }
}