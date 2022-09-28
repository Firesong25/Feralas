namespace Feralas;
public static class CachedData
{

    public static List<MarginReport> MarginReports = new List<MarginReport>();

    public static List<WowRealm> Realms = new List<WowRealm>();

    public static List<Profession> Professions = new List<Profession>();

    public static List<CraftedItem> RealmItems = new List<CraftedItem>();

    public static List<CraftedItem> CommodityItems = new List<CraftedItem>();

    public static List<WowAuction> EuCommodities = new List<WowAuction>();

    public static List<WowAuction> UsCommodities = new List<WowAuction>();

    public static List<SkillTier> SkillTiers = new List<SkillTier>();

    public static readonly Dictionary<int, int> VendorItemCosts = new Dictionary<int, int>()
            {
                {180733, 90000},
                {178787, 1250000},
                {20815, 800},
                {6217, 124},
                {187812, 2500000},
                {3371, 400},
                {183950, 90000},
                {172056, 5000},
                {172057, 3750},
                {172058, 4500},
                {172059, 4250},
                {178786, 3500},
                {159, 5},
                {30817, 5},
                {2687, 2},
                {177062, 110000},
                {183955, 90000},
                {177061, 5000},
                {173060, 1000},
                {180732, 500},
                {175886, 1000},
                {183953, 90000},
                {39489, 5000},
                {39505, 750},
                {183952, 90000},
                {173168, 10000},
                {183954, 90000},
            };

    // iksit better to cache all prices once per run or just those I need 177 times per run
    public static Dictionary<int, long> EuCommodityPrices = new();

    public static Dictionary<int, long> UsCommodityPrices = new();

    public static List<Reagent> Reagents = new();




    public static async Task Init(PostgresContext context)
    {
        // populate the lists in the order they are used
        DateTime cutOffTime = DateTime.UtcNow - new TimeSpan(48, 0, 0);

        if (Realms.Count.Equals(0))
        {
            Realms = context.WowRealms.ToList();
        }

        if (EuCommodities.Count.Equals(0))
        {
            EuCommodities = context.WowAuctions.Where(l => l.ConnectedRealmId.Equals(54321) && 
                l.Sold.Equals(false) &&
                l.FirstSeenTime > cutOffTime).ToList();
        }

        if (MarginReports.Count.Equals(0))
        {
            MarginReports = context.MarginReports.ToList();
        }

        if (Professions.Count.Equals(0))
        {
            Professions = context.Professions.ToList();
        }

        if (RealmItems.Count.Equals(0))
        {
            RealmItems = context.CraftedItems.Where(l => l.IsCommodity.Equals(false)).ToList();
        }

        if (CommodityItems.Count.Equals(0))
        {
            CommodityItems = context.CraftedItems.Where(l => l.IsCommodity.Equals(true)).ToList();
        }

        if (SkillTiers.Count.Equals(0))
        {
            SkillTiers = context.SkillTiers.ToList();
        }

        if (Reagents.Count.Equals(0))
        {
            Reagents = context.Reagents.ToList();
        }

        if (UsCommodities.Count.Equals(0))
        {
            UsCommodities = context.WowAuctions.Where(l => l.ConnectedRealmId.Equals(12345) &&
                l.Sold.Equals(false) &&
                l.FirstSeenTime > cutOffTime).ToList();
        }

        await Task.Delay(1);
    }
}
