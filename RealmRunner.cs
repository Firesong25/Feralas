using System.Text.Json;

namespace Feralas;

internal class RealmRunner
{
    public RealmRunner(WowRealm wowRealm)
    {
        realm = wowRealm;
        LastUpdate = DateTime.UtcNow;
    }

    WowRealm realm;

    public DateTime LastUpdate { get; private set; }

    public async Task Run()
    {
        System.Diagnostics.Stopwatch sw = new();

        sw.Start();
        string tag = $"{realm.Name} US";
        if (realm.WowNamespace.Contains("-eu"))
        {
            tag = $"{realm.Name} EU";
        }

        string results = string.Empty;

        string auctionsJson = await WowApi.GetRealmAuctions(realm, tag);
        if (auctionsJson != string.Empty && IsJsonValid(auctionsJson))
        {
            DbUpdater db = new();
            results = await db.DoUpdatesAsync(realm, auctionsJson, tag);
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
            //LogMaker.LogToTable($"{tag}", $"{GetReadableTimeByMs(sw.ElapsedMilliseconds)} for {results}.");
            sw.Restart();
        }
    }

    public static bool IsJsonValid(string txt)
    {
        try { return JsonDocument.Parse(txt) != null; } catch { }

        return false;
    }


    public static string GetReadableTimeByMs(long ms)
    {
        // Based on answers https://stackoverflow.com/questions/9993883/convert-milliseconds-to-human-readable-time-lapse
        TimeSpan t = TimeSpan.FromMilliseconds(ms);
        if (t.Hours > 0) return $"{t.Hours} hours {t.Minutes} minutes {t.Seconds} seconds";
        else if (t.Minutes > 0) return $"{t.Minutes} minutes {t.Seconds} seconds";
        else if (t.Seconds > 0) return $"{t.Seconds} seconds";
        else return $"{t.Milliseconds} milliseconds";
    }
}

