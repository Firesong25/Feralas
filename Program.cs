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

            PostgresContext context = new PostgresContext();
            List<WowRealm> activeRealms = await CreateActiveRealmList(context);

            // Test area

            //  LogMaker.LogToTable($"Program.cs", $"Delete This");



            //DELETE UNTIL THIS

            LogMaker.LogToTable("Cleardragon", $"<em>Auctions scans for {activeRealms.Count} realms starting.</em>");
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
                    await realmRunner.Run();
                }

                z++;
                LogMaker.LogToTable("Cleardragon", $"<em>Auctions scan {z} complete in {RealmRunner.GetReadableTimeByMs(sw.ElapsedMilliseconds)}.</em>");

            }
        }
        public static async Task<List<WowRealm>> CreateActiveRealmList(PostgresContext context)
        {
            await Task.Delay(1);  // stop compiler warnings on Linux
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

            return activeRealms.OrderBy(l => l.WowNamespace).ThenBy(l => l.Name).ToList();
        }

    }
}