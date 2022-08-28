using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace Feralas
{
    public static class WowApi
    {
        static string AccessToken = string.Empty;
        static Stopwatch TokenTimer = new();
        public static async Task<string> GetRealmAuctions(string realmName, string wowNamespace)
        {
            string auctionsJson = string.Empty;

            if (AccessToken == string.Empty || TokenTimer.ElapsedMilliseconds / 1000 > 3600 || !TokenTimer.IsRunning)
            {
                TokenTimer.Start();
                Token tok = await GetElibilityToken();
                AccessToken = tok.AccessToken.ToString();
            }

            List<Auction> auctions = new List<Auction>();
            int connectedRealmId = await GetConnectedRealmId(wowNamespace, realmName);
            //string url = $"https://us.api.blizzard.com/data/wow/realm/{realmName}?namespace={wowNamespace}&locale=en_US&access_token={AccessToken}";
            string url = $"https://us.api.blizzard.com/data/wow/connected-realm/{connectedRealmId}/auctions?namespace={wowNamespace}&locale=en_US&access_token={AccessToken}";
            string commoditiesUrl = $"https://us.api.blizzard.com/data/wow/auctions/commodities?namespace={wowNamespace}&access_token={AccessToken}";

            HttpClient client = new();

            using (HttpContent content = client.GetAsync(url).Result.Content)
            {
                auctionsJson = content.ReadAsStringAsync().Result;
                await Task.Delay(1);
            }

            return auctionsJson;
        }

        public static async Task<int> GetConnectedRealmId(string wowNamespace, string realmName)
        {
            string url = $"https://us.api.blizzard.com/data/wow/realm/{realmName}?namespace={wowNamespace}&locale=en_US&access_token={AccessToken}";

            HttpClient client = new();
            int connectedRealmId = 0;

            using (HttpResponseMessage response = client.GetAsync(url).Result)
            {
                using (HttpContent content = response.Content)
                {
                    var json = content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    foreach (KeyValuePair<string, object> pair in result)
                    {
                        ////[3] = {[connected_realm, {{"href": "https://us.api.blizzard.com/data/wow/connected-realm/71?namespace=dynamic-us"}]}
                        if (pair.Key == "connected_realm")
                        {
                            connectedRealmId = Convert.ToInt32(string.Concat(pair.Value.ToString().Where(char.IsNumber)));
                            break;
                        }
                    }
                }
            }
            return connectedRealmId;
        }
        public static async Task<string> GetItemName(int itemId)
        {
            await Task.Delay(1);
            string itemName = string.Empty;
            string wowNamespace = "static-us";
            string uri = $"https://us.api.blizzard.com/data/wow/item/{itemId}?namespace={wowNamespace}&locale=en_US&access_token={AccessToken}";

            HttpClient client = new();

            using (HttpResponseMessage response = client.GetAsync(uri).Result)
            {
                using (HttpContent content = response.Content)
                {
                    var json = content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    foreach (KeyValuePair<string, object> pair in result)
                    {
                        if (pair.Key == "name")
                        {
                            itemName = pair.Value.ToString();
                            break;
                        }
                    }
                }
            }

            return itemName;
        }

        private static async Task<Token> GetElibilityToken()
        {
            string baseAddress = @"https://us.battle.net/oauth/token";

            if (Configurations.BlizzardClientId == string.Empty)
                await Configurations.Init();

            HttpClient client = new();

            string grant_type = "client_credentials";

            var form = new Dictionary<string, string>
                {
                    {"grant_type", grant_type},
                    {"client_id", Configurations.BlizzardClientId},
                    {"client_secret", Configurations.BlizzardClientPassword},
                };

            HttpResponseMessage tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            Token tok = JsonConvert.DeserializeObject<Token>(jsonContent);
            return tok;
        }
    }

    public class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
