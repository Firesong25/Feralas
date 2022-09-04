// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
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

