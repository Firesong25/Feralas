using System.Text.Json;

namespace Feralas;
public class Listings
{
    static List<Auction> jsonAuctions = new();
    public List<WowAuction> LiveAuctions { get; private set; }
    public List<WowItem> ExtraItems { get; private set; }

    WowAuction extraAuction = new();
    WowAuction trialAuction = new();

    static List<int> craftedItemIds = new();

    public async Task CreateLists(PostgresContext context, WowRealm realm, string json, string tag)
    {
        LiveAuctions = new();
        ExtraItems = new();

        if (craftedItemIds.Count < 1)
        {
            craftedItemIds = context.CraftedItems.Select(l => l.Id).ToList();
        }

        if (json.Length > 0)
        {
            Root root = JsonSerializer.Deserialize<Root>(json);
            string connectedRealmString = string.Empty;

            if (root.auctions == null || root.auctions.Count == 0)
            {
                LogMaker.LogToTable($"{tag}", $"No auctions found.");
                File.WriteAllText(tag += ".json", json);
                return;
            }
            jsonAuctions = root.auctions.ToList();

        }

        //await GetExtraItemsAsync();
        await GetLiveAuctionsAsync(realm, tag);
    }

    public async Task GetLiveAuctionsAsync(WowRealm realm, string tag)
    {
        await Task.Delay(1); // happy now?
        foreach (Auction auction in jsonAuctions)
        {
            // I can't see a downside to this filter.
            if (FilterListing(realm.ConnectedRealmId, auction.item.id))
            {
                continue;
            }
            extraAuction.AuctionId = auction.id;
            extraAuction.ConnectedRealmId = realm.ConnectedRealmId;
            extraAuction.LastSeenTime = DateTime.UtcNow;
            extraAuction.LastSeenTime = DateTime.SpecifyKind(extraAuction.LastSeenTime, DateTimeKind.Utc);
            extraAuction.Quantity = auction.quantity;
            extraAuction.Buyout = auction.buyout;
            extraAuction.UnitPrice = auction.unit_price;

            if (extraAuction.UnitPrice == 0 && extraAuction.Buyout > 0)
            {
                extraAuction.UnitPrice = (long)extraAuction.Buyout;
            }
            extraAuction.ItemId = auction.item.id;

            if (auction.time_left.ToLower().Contains("very"))
            {
                extraAuction.TimeLeft = TimeLeft.VERY_LONG;
            }
            else if (auction.time_left.ToLower().Contains("medium"))
            {
                extraAuction.TimeLeft = TimeLeft.MEDIUM;
            }
            else if (auction.time_left.ToLower().Contains("short"))
            {
                extraAuction.TimeLeft = TimeLeft.SHORT;
            }
            else
            {
                extraAuction.TimeLeft = TimeLeft.LONG;
            }

            LiveAuctions.Add(extraAuction);

            extraAuction = new();
        }
    }

    bool FilterListing(int connectedRealmId, int itemId)
    {
        if (!connectedRealmId.Equals(12345) && !connectedRealmId.Equals(54321) && !craftedItemIds.Contains(itemId))
        {
            return true;
        }

        return false;
    }

    public async Task GetExtraItemsAsync()
    {
        await Task.Delay(1); // happy now?
        WowItem trialItem = new();
        foreach (Auction auction in jsonAuctions)
        {
            trialItem = new();
            if (ExtraItems.Count > 0)
                trialItem = ExtraItems.FirstOrDefault(l => l.ItemId == auction.item.id);
            else
                trialItem = null;

            if (trialItem == null)
            {
                WowItem newbie = new();
                newbie.ItemId = auction.item.id;
                newbie.Name = string.Empty;
                if (auction.item.bonus_lists != null)
                {
                    string bonus_list = string.Empty;
                    foreach (int i in auction.item.bonus_lists)
                    {
                        bonus_list += i.ToString();
                        bonus_list += '-';
                    }

                    bonus_list = bonus_list.Remove(bonus_list.Length - 1, 1);
                    newbie.BonusList = bonus_list;
                }
                else
                {
                    newbie.BonusList = string.Empty;
                }
                ExtraItems.Add(newbie);
            }
        }
    }
}

public class Auction
{
    public int id { get; set; }
    public Item item { get; set; }
    public long buyout { get; set; }
    public int quantity { get; set; }

    public long unit_price { get; set; }
    public string time_left { get; set; }
    public long? bid { get; set; }
}

public class Commodities
{
    public string href { get; set; }
}

public class ConnectedRealm
{
    public string href { get; set; }
}

public class Item
{
    public int id { get; set; }
    public int context { get; set; }
    public List<int> bonus_lists { get; set; }
    public List<Modifier> modifiers { get; set; }
    public int? pet_breed_id { get; set; }
    public int? pet_level { get; set; }
    public int? pet_quality_id { get; set; }
    public int? pet_species_id { get; set; }
}

public class Links
{
    public Self self { get; set; }
}

public class Modifier
{
    public int type { get; set; }
    public int value { get; set; }
}

public class Root
{
    public Links _links { get; set; }
    public ConnectedRealm connected_realm { get; set; }
    public List<Auction> auctions { get; set; }
    public Commodities commodities { get; set; }
}

public class Self
{
    public string href { get; set; }
}
