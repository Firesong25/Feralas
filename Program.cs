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
            RealmRunner IllidanUs = new("Illidan", "dynamic-us");
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
                // in order of size
                kazzakEu.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                IllidanUs.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                anvilmarUs.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                nordrassilEu.Run();
                await Task.Delay(new TimeSpan(0, 5, 0));
                z++;
                LogMaker.Log($"Auctions scan {z} complete.");
                if (z % 5 == 0)
                {
                    commoditiesUs.Run();
                    await Task.Delay(new TimeSpan(0, 5, 0));
                    commoditiesEu.Run();
                    await Task.Delay(new TimeSpan(0, 5, 0));
                    y++;
                    LogMaker.Log($"Commodities scan {y} complete.");
                }
                
            }

            LogMaker.Log($"If you see this, something has gone terribly wrong.");

        }
    }
}