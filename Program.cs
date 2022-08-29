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

            RealmRunner anvilmarUs = new("Anvilmar", "dynamic-us", context);
            RealmRunner kazzakEu = new("Kazzak", "dynamic-eu", context);
            try
            {
                kazzakEu.Run();
                await Task.Delay(new TimeSpan(0, 10, 0));
                await anvilmarUs.Run();
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