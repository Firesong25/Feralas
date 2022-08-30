using Microsoft.EntityFrameworkCore;
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

            //LocalContext context = new();
            //List<WowAuction> auctions = context.WowAuctions.ToList();
            //List<WowItem> items = context.WowItems.ToList();



            //foreach (WowAuction auction in auctions)
            //{
            //    if (auction.Id == Guid.Empty)
            //        auction.Id = Guid.NewGuid();
            //    auction.PartitionKey = auction.ConnectedRealmId.ToString();

            //}

            //context.WowAuctions.UpdateRange(auctions);
            //context.WowItems.RemoveRange(items);
            //context.SaveChanges();
            //Console.WriteLine("Done");
            //return;



            LogMaker.Log($"Starting process.");
            await Configurations.Init();

            RealmRunner anvilmarUs = new("Anvilmar", "dynamic-us");
            RealmRunner IllidanUs = new("Illidan", "dynamic-us");
            RealmRunner kazzakEu = new("Kazzak", "dynamic-eu");
            RealmRunner nordrassilEu = new("Nordrassil","dynamic-eu");

            try
            {
                nordrassilEu.Run();
                await Task.Delay(new TimeSpan(0, 7, 0));
                anvilmarUs.Run();
                await Task.Delay(new TimeSpan(0, 7, 0));
                kazzakEu.Run();
                await Task.Delay(new TimeSpan(0, 7, 0));
                await IllidanUs.Run();
                await Task.Delay(new TimeSpan(0, 7, 0));
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