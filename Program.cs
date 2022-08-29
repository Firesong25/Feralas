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

            RealmRunner anvilmarUs = new("Anvilmar", "dynamic-us");
            RealmRunner IllidanUs = new("Illidan", "dynamic-us");
            RealmRunner kazzakEu = new("Kazzak", "dynamic-eu");
            try
            {
                anvilmarUs.Run();
                await Task.Delay(new TimeSpan(0, 10, 0));
                kazzakEu.Run();
                await Task.Delay(new TimeSpan(0, 10, 0));
                await IllidanUs.Run();
                await Task.Delay(new TimeSpan(0, 10, 0));
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