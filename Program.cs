using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Feralas
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Task.Delay(1);  // stop compiler warnings on Linux

            if (File.Exists("log.html"))
                File.Delete("log.html");

            await Configurations.Init();

            //PostgresContext context = new PostgresContext();
            //List<WowAuction> allAuctions = context.WowAuctions.Where(l => l.PartitionKey == "1393").ToList();

            //context.WowAuctions.RemoveRange(allAuctions);
            //context.SaveChanges();

            //allAuctions = context.WowAuctions.ToList();

            //foreach (WowAuction auction in allAuctions)
            //{
            //    auction.FirstSeenTime = DateTime.UtcNow - new TimeSpan(1, 30, 0);
            //    auction.FirstSeenTime = DateTime.SpecifyKind(auction.FirstSeenTime, DateTimeKind.Utc);
            //}

            //context.WowAuctions.UpdateRange(allAuctions);
            //context.SaveChanges();
            //return;


            LogMaker.Log($"Starting process.");

            
            RealmRunner IllidanUs = new("Illidan", "dynamic-us");
            RealmRunner kazzakEu = new("Kazzak", "dynamic-eu");
            RealmRunner nordrassilEu = new("Nordrassil","dynamic-eu");
            RealmRunner anvilmarUs = new("Anvilmar", "dynamic-us");

            try
            {
                int z = 0;
                while (true)
                {
                    await nordrassilEu.Run();
                    await anvilmarUs.Run();
                    await IllidanUs.Run();                    
                    await kazzakEu.Run();    

                    z++;
                    LogMaker.Log($"Auctions scan {z} complete.");
                    await Task.Delay(new TimeSpan(0, 15, 0));
                }
            }
            catch (Exception ex)
            {
                LogMaker.Log("___________________________________");
                LogMaker.Log(ex.Message);
                LogMaker.Log("___________________________________");
                LogMaker.Log(ex.StackTrace);
                LogMaker.Log("___________________________________");
                if (ex.InnerException != null)
                    LogMaker.Log($"{ex.InnerException}");
            }
            


            LogMaker.Log($"If you see this, something has gone terribly wrong.");

        }
    }
}