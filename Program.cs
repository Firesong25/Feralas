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


            int y = 0;
            int z = 0;

            RealmRunner kazzakEu = new("Kazzak", "dynamic-eu");
            RealmRunner nordrassilEu = new("Nordrassil", "dynamic-eu");
            RealmRunner anvilmarUs = new("Anvilmar", "dynamic-us");
            RealmRunner commoditiesUs = new("Commodities", "dynamic-us");
            RealmRunner commoditiesEu = new("Commodities", "dynamic-eu");

            // Test area
            //LogMaker.Log($"DELETE AFTER THIS");
            //LogMaker.Log($"DELETE UNTIL THIS");

            while (true)
            {
                RealmRunner realmRunner = new("Kazzak", "dynamic-eu");
                realmRunner.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                realmRunner = new("Illidan", "dynamic-us");
                realmRunner.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                realmRunner = new("Anvilmar", "dynamic-us");
                realmRunner.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                realmRunner = new("Nordrassil", "dynamic-eu");
                realmRunner.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                z++;
                LogMaker.Log($"Auctions scan {z} complete.");
                //if (z % 5 == 0)
                //{
                //    realmRunner = new("Commodities", "dynamic-us");
                //    realmRunner.Run();
                //    await Task.Delay(new TimeSpan(0, 5, 0));
                //    realmRunner = new("Commodities", "dynamic-eu");
                //    realmRunner.Run();
                //    await Task.Delay(new TimeSpan(0, 5, 0));
                //    y++;
                //    LogMaker.Log($"Commodities scan {y} complete.");
                //}
                realmRunner = new("Free this memory", "please!");

            }

            LogMaker.Log($"If you see this, something has gone terribly wrong.");

        }
    }
}