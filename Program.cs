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


            LogMaker.Log($"Starting process.");
            await Configurations.Init();
            LocalContext context = new();

            //To fix mistakes
            //List<WowItem> deleteMe = context.WowItems.AsNoTracking().ToList();
            //context.WowItems.RemoveRange(deleteMe);
            //List<WowAuction> deleteMeToo = context.WowAuctions.AsNoTracking().ToList();
            //context.WowAuctions.RemoveRange(deleteMeToo);
            //context.SaveChanges();

            RealmRunner anvilmarUs = new("anvilmar", "dynamic-us", context);
            await anvilmarUs.Run();


            LogMaker.Log($"If you see this, something has gone terribly wrong.");

        }
    }
}