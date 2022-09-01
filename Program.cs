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
                    // in order of size
                    await kazzakEu.Run();
                    await IllidanUs.Run();  
                    await anvilmarUs.Run();
                    await nordrassilEu.Run();


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