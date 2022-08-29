namespace Feralas
{
    internal class RealmRunner
    {
        public RealmRunner(string slug, string blizzardNamespace)
        {
            realmName = slug;
            wowNamespace = blizzardNamespace;
        }
        // https://docs.microsoft.com/en-gb/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues
        //Using one context per thread to avoid collisions.
        string realmName;
        string wowNamespace;

        TimeSpan pollInterval = new TimeSpan(0, 20, 0);

        public async Task Run()
        {
            LocalContext context = new();
            int runCount = 0;
            while (true)
            {
                runCount++;

                LogMaker.Log($"Auction run {runCount} for {realmName} on {wowNamespace}.");
                string auctionsJson = await WowApi.GetRealmAuctions(realmName, wowNamespace);
                if (auctionsJson != string.Empty)
                {
                    LogMaker.Log($"The realm data for {realmName} using {wowNamespace} namespace is downloaded.");
                    DbUpdater db = new();
                    string tag = $"{realmName} US";
                    if (wowNamespace.Contains("-eu"))
                        tag = $"{realmName} EU";
                    await db.DoUpdatesAsync(context, auctionsJson, tag);
                }
                else
                    LogMaker.Log($"Failed to get realm data for {realmName} using {wowNamespace} namespace.");

                await Task.Delay(pollInterval);
            }
        }
    }
}
