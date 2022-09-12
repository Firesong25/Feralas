using System;

namespace Feralas
{
    internal class RealmRunner
    {
        public RealmRunner(WowRealm realm)
        {
            Realm = realm;
            LastUpdate = DateTime.UtcNow;
        }

        public WowRealm Realm;


        public DateTime LastUpdate { get; private set; }

        public async Task Run()
        {
            System.Diagnostics.Stopwatch sw = new();

            try
            {
                sw.Start();
                string tag = $"{Realm.Name} US";
                if (Realm.WowNamespace.Contains("-eu"))
                    tag = $"{Realm.Name} EU";

                string results = string.Empty;

                //LogMaker.LogToTable($"RealmRunner",$"{tag} Auction house scan.");
                string auctionsJson = await WowApi.GetRealmAuctions(Realm, tag);
                if (auctionsJson != string.Empty)
                {
                    //LogMaker.LogToTable($"RealmRunner", $"The realm data for {tag} namespace is downloaded.");
                    DbUpdater db = new();
                    results = await db.DoUpdatesAsync(auctionsJson, tag);
                    LastUpdate = DateTime.UtcNow;
                }
                else
                    LogMaker.LogToTable($"RealmRunner", $"Failed to get realm data for {tag} namespace.");

                if (results != string.Empty)
                {
                    LogMaker.LogToTable($"{tag}", $"{GetReadableTimeByMs(sw.ElapsedMilliseconds)} for {results}.");
                }
                else
                {
                    LogMaker.LogToTable($"{tag}", $"{GetReadableTimeByMs(sw.ElapsedMilliseconds)} for failed run.");
                }
                
            }
            catch (Exception ex)
            {
                LogMaker.LogToTable($"RealmRunner",$"________________{Realm.Name} Run Failed___________________");
                LogMaker.LogToTable($"RealmRunner",ex.Message);
                LogMaker.LogToTable($"RealmRunner","___________________________________");
                LogMaker.LogToTable($"RealmRunner",ex.StackTrace);
                LogMaker.LogToTable($"RealmRunner","___________________________________");
                if (ex.InnerException != null)
                    LogMaker.LogToTable($"RealmRunner",$"{ex.InnerException}");
                LogMaker.LogToTable($"RealmRunner",$"________________{Realm.Name} Run Failed___________________");
            }
        }


        static string GetReadableTimeByMs(long ms)
        {
            // Based on answers https://stackoverflow.com/questions/9993883/convert-milliseconds-to-human-readable-time-lapse
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            if (t.Hours > 0) return $"{t.Hours} hours {t.Minutes} minutes {t.Seconds} seconds {t.Milliseconds} milliseconds";
            else if (t.Minutes > 0) return $"{t.Minutes} minutes {t.Seconds} seconds {t.Milliseconds} milliseconds";
            else if (t.Seconds > 0) return $"{t.Seconds} seconds {t.Milliseconds} milliseconds";
            else return $"{t.Milliseconds} milliseconds";
        }
    }
}

