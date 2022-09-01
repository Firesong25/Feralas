﻿using Microsoft.EntityFrameworkCore;
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

            LogMaker.Log($"Starting process.");

            int z = 0;
            RealmRunner IllidanUs = new("Illidan", "dynamic-us");
            RealmRunner kazzakEu = new("Kazzak", "dynamic-eu");
            RealmRunner nordrassilEu = new("Nordrassil", "dynamic-eu");
            RealmRunner anvilmarUs = new("Anvilmar", "dynamic-us");
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
            }

            LogMaker.Log($"If you see this, something has gone terribly wrong.");

        }
    }
}