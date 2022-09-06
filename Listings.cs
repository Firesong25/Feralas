using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Feralas
{
    public class Listings
    {

        static string realmId = string.Empty;
        static List<Auction> jsonAuctions = new();
        public List<WowAuction> LiveAuctions { get; private set; }
        public List<WowItem> ExtraItems { get; private set; }

        WowAuction extraAuction = new();
        WowAuction trialAuction = new();

        public async Task CreateLists(string json, string tag)
        {
            await Task.Delay(1); // happy now?
            LiveAuctions = new();
            ExtraItems = new();

            if (json.Length > 0)
            {
                Root root = JsonSerializer.Deserialize<Root>(json);
                string connectedRealmString = string.Empty;

                if (root.connected_realm != null)
                {
                    connectedRealmString = root.connected_realm.href;
                    realmId = string.Concat(connectedRealmString.Where(char.IsNumber));
                }
                
                jsonAuctions = root.auctions.ToList();
                
            }

            await GetExtraItemsAsync();
            await GetLiveAuctionsAsync(tag);
        }

        public async Task GetLiveAuctionsAsync(string tag)
        {
            await Task.Delay(1); // happy now?
            foreach (Auction auction in jsonAuctions)
            {
                extraAuction.AuctionId = auction.id;
                if (tag.ToLower().Contains("us commodities"))
                {
                    extraAuction.PartitionKey = "12345";
                    extraAuction.ConnectedRealmId = 12345;
                }
                else if (tag.ToLower().Contains("eu commodities"))
                {
                    extraAuction.PartitionKey = "54321";
                    extraAuction.ConnectedRealmId = 54321;
                }
                else
                {
                    extraAuction.PartitionKey = realmId;
                    extraAuction.ConnectedRealmId = Convert.ToInt32(extraAuction.PartitionKey);
                }
                extraAuction.LastSeenTime = DateTime.UtcNow;                
                extraAuction.LastSeenTime = DateTime.SpecifyKind(extraAuction.LastSeenTime, DateTimeKind.Utc);
                extraAuction.Quantity = auction.quantity;
                extraAuction.Buyout = auction.buyout;
                double itemPrice = (long)extraAuction.Buyout / extraAuction.Quantity;
                extraAuction.UnitPrice = (long)Math.Floor(itemPrice);
                extraAuction.ItemId = auction.item.id;
                if (auction.time_left.ToLower().Contains("short"))
                    extraAuction.ShortTimeLeftSeen = true;
                LiveAuctions.Add(extraAuction);
                extraAuction = new();
            }
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

        // a lot of code here but may as well delete it if I can't link bonus lists to names for
        // in-game searches
        public async Task OldGettrialItemsAsync()
        {
            await Task.Delay(1); // happy now?
            WowItem trialItem = new();
            foreach (Auction auction in jsonAuctions)
            {
                trialItem.ItemId = auction.item.id;
                trialItem.Name = string.Empty;
                if (auction.item.bonus_lists != null)
                {
                    string bonus_list = string.Empty;
                    foreach (int i in auction.item.bonus_lists)
                    {
                        bonus_list += i.ToString();
                        bonus_list += '-';
                    }

                    bonus_list = bonus_list.Remove(bonus_list.Length - 1, 1);
                    trialItem.BonusList = bonus_list;
                }
                else
                    trialItem.BonusList = string.Empty;

                if (auction.item.pet_breed_id > 0)
                {
                    trialItem.PetSpeciesId = auction.item.pet_species_id;
                    trialItem.PetBreedId = auction.item.pet_breed_id;
                    trialItem.PetLevel = auction.item.pet_level;
                    trialItem.PetQualityId = auction.item.pet_quality_id;
                }
                else
                {
                    trialItem.PetSpeciesId = 0;
                    trialItem.PetBreedId = 0;
                    trialItem.PetLevel = 0;
                    trialItem.PetQualityId = 0;
                }

                if (trialItem.PetBreedId > 0)
                {
                    trialItem = ExtraItems.FirstOrDefault(l => l.ItemId == trialItem.ItemId &&
                        l.PetSpeciesId == trialItem.PetSpeciesId &&
                        l.PetBreedId == trialItem.PetBreedId &&
                        l.PetLevel == trialItem.PetLevel &&
                        trialItem.PetQualityId == trialItem.PetQualityId);
                }
                else
                {
                    trialItem = ExtraItems.FirstOrDefault(l => l.ItemId == trialItem.ItemId &&
                        l.BonusList == trialItem.BonusList);
                }


                if (trialItem == null)
                    ExtraItems.Add(trialItem);

                trialItem = new();
            }
        }
    }

    public class Auction
    {
        public int id { get; set; }
        public Item item { get; set; }
        public long buyout { get; set; }
        public int quantity { get; set; }
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
}
