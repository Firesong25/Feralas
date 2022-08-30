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

        TimeSpan pollInterval = new TimeSpan(0, 20, 0);

        public async Task Run()
        {
            LocalContext context = new();

            try
            {
                LogMaker.Log($"Auction run for {realmName} on {wowNamespace}.");
                string auctionsJson = await WowApi.GetRealmAuctions(realmName, wowNamespace);
                if (auctionsJson != string.Empty)
                {
                    LogMaker.Log($"The realm data for {realmName} using {wowNamespace} namespace is downloaded.");
                    DbUpdater db = new();
                    string tag = $"{realmName} US";
                    if (wowNamespace.Contains("-eu"))
                        tag = $"{realmName} EU";
                    await db.DoUpdatesAsync(context, auctionsJson, tag);
                    LastUpdate = DateTime.UtcNow;
                }
                else
                    LogMaker.Log($"Failed to get realm data for {realmName} using {wowNamespace} namespace.");
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

            await Task.Delay(pollInterval);
        }
    }
}

