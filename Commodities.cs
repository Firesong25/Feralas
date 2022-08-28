using System.Text.Json;

namespace Feralas
{
    internal class Commodities
    {
        string fileName = "mini_commodities.json";
        string json = string.Empty;

        public async Task Init()
        {
            if (File.Exists(fileName))
            {
                json = File.ReadAllText(fileName);
            }

            if (json.Length > 0)
            {
                Root root = JsonSerializer.Deserialize<Root>(json);
                List<Auction> auctions = root.auctions.ToList();
                Auction first = auctions.FirstOrDefault();
            }

        }
    }
}
