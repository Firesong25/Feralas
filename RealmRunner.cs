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
            while (true)
            {
                string auctionsJson = await WowApi.GetRealmAuctions(realmName, wowNamespace);
                LogMaker.Log($"The realm data for {realmName} using {wowNamespace} namespace is downloaded.");
                DbUpdater db = new();
                await db.DoUpdatesAsync(context, auctionsJson);

                await Task.Delay(pollInterval);
            }
        }
    }
}
