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

            sw.Start();
            string tag = $"{Realm.Name} US";
            if (Realm.WowNamespace.Contains("-eu"))
            {
                tag = $"{Realm.Name} EU";
            }

            string results = string.Empty;

            string auctionsJson = await WowApi.GetRealmAuctions(Realm, tag);
            if (auctionsJson != string.Empty)
            {
                DbUpdater db = new();
                results = await db.DoUpdatesAsync(Realm, auctionsJson, tag);
                LastUpdate = DateTime.UtcNow;
            }
            else
                LogMaker.LogToTable($"RealmRunner", $"Failed to get realm data for {tag} namespace.");

            if (results.Equals(string.Empty))
            {
                LogMaker.LogToTable($"{tag}", $"{GetReadableTimeByMs(sw.ElapsedMilliseconds)} for failed run.");
            }
            else
            {
                LogMaker.LogToTable($"{tag}", $"{GetReadableTimeByMs(sw.ElapsedMilliseconds)} for {results}.");
                sw.Restart();
            }
        }


        public static string GetReadableTimeByMs(long ms)
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

