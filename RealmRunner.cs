﻿using System;

namespace Feralas
{
    internal class RealmRunner
    {
        public RealmRunner(string slug, string blizzardNamespace)
        {
            realmName = slug;
            wowNamespace = blizzardNamespace;
            LastUpdate = DateTime.UtcNow;
        }
        // https://docs.microsoft.com/en-gb/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues
        //Using one context per thread to avoid collisions.
        string realmName;
        string wowNamespace;

        public DateTime LastUpdate { get; private set; }

        public async Task Run()
        {
            System.Diagnostics.Stopwatch sw = new();

            try
            {
                sw.Start();
                string tag = $"{realmName} US";
                if (wowNamespace.Contains("-eu"))
                    tag = $"{realmName} EU";

                if (realmName.ToLower().Contains("commodities") && wowNamespace.Contains("-us"))
                {
                    tag = "US commodities";
                }

                if (realmName.ToLower().Contains("commodities") && wowNamespace.Contains("-eu"))
                {
                    tag = "EU commodities";
                }                

                int taglength = tag.Length;
                int idealLength = 25;
                tag += ":";
                for (int i = taglength; i <= idealLength; ++i)
                {
                    tag += " ";
                }



                LogMaker.Log($"{tag} Auction house scan.");
                string auctionsJson = await WowApi.GetRealmAuctions(realmName, wowNamespace, tag);
                if (auctionsJson != string.Empty)
                {
                    //LogMaker.Log($"The realm data for {tag} namespace is downloaded.");
                    DbUpdater db = new();
                    await db.DoUpdatesAsync(auctionsJson, tag);
                    LastUpdate = DateTime.UtcNow;
                }
                else
                    LogMaker.Log($"Failed to get realm data for {tag} namespace.");


                LogMaker.Log($"{tag} scan and database update took {GetReadableTimeByMs(sw.ElapsedMilliseconds)}.");
            }
            catch (Exception ex)
            {
                LogMaker.Log($"________________{realmName} Run Failed___________________");
                LogMaker.Log(ex.Message);
                LogMaker.Log("___________________________________");
                LogMaker.Log(ex.StackTrace);
                LogMaker.Log("___________________________________");
                if (ex.InnerException != null)
                    LogMaker.Log($"{ex.InnerException}");
                LogMaker.Log($"________________{realmName} Run Failed___________________");
            }
        }


        static string GetReadableTimeByMs(long ms)
        {
            // Based on answers https://stackoverflow.com/questions/9993883/convert-milliseconds-to-human-readable-time-lapse
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            if (t.Hours > 0) return $"{t.Hours} hours {t.Minutes} minutes {t.Seconds} seconds";
            else if (t.Minutes > 0) return $"{t.Minutes}minutes {t.Seconds} seconds";
            else if (t.Seconds > 0) return $"{t.Seconds} seconds";
            else return $"{t.Milliseconds} milliseconds";
        }
    }
}

