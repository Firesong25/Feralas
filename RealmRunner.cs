namespace Feralas
{
    internal class RealmRunner
    {
        public RealmRunner(string slug, string blizzardNamespace, LocalContext dbContext)
        {
            realmName = slug;
            wowNamespace = blizzardNamespace;
            context = dbContext;
        }

        string realmName;
        string wowNamespace;
        LocalContext context;

        TimeSpan pollInterval = new TimeSpan(0, 20, 0);

        public async Task Run()
        {
            int runCount = 0;
            while (true)
            {
                runCount++;

                LogMaker.Log($"Auction run {runCount} for {realmName} on {wowNamespace}.");
                string auctionsJson = await WowApi.GetRealmAuctions(realmName, wowNamespace);
                LogMaker.Log($"The realm data for {realmName} using {wowNamespace} namespace is downloaded.");
                DbUpdater db = new();
                string tag = $"{realmName} US";
                if (wowNamespace.Contains("-eu"))
                    tag = $"{realmName} EU";
                await db.DoUpdatesAsync(context, auctionsJson, tag);

                await Task.Delay(pollInterval);
            }
        }
    }
}
