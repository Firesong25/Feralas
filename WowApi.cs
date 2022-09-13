using Newtonsoft.Json;
using System.Diagnostics;

namespace Feralas
{
    public static class WowApi
    {
        static Token holdToken = new();
        static string AccessToken = string.Empty; //"USYhT4u8tJEZOB87HD43PofR4gZ9r5iTGq";
        static Stopwatch TokenTimer = new();
        static Dictionary<string, int> CachedConnectedRealmIds = new();
        public static async Task<string> GetRealmAuctions(WowRealm realm, string tag)
        {
            string auctionsJson = string.Empty;

            Token tok = await GetElibilityToken();

            if (tok.AccessToken == null)
            {
                //LogMaker.LogToTable($"WowApi", $"Access token refused.  Use the old one...");
                AccessToken = "UScrm608hoZJq8hso4zGW8caqUeXGhSoJE";
            }
            else
            {
                AccessToken = tok.AccessToken.ToString();
            }

            List<Auction> auctions = new();


            string url = $"https://us.api.blizzard.com/data/wow/connected-realm/{realm.ConnectedRealmId}/auctions?namespace={realm.WowNamespace}&locale=en_US&access_token={AccessToken}";

            if (realm.WowNamespace.Contains("-eu"))
            {
                url = $"https://eu.api.blizzard.com/data/wow/connected-realm/{realm.ConnectedRealmId}/auctions?namespace=dynamic-eu&locale=en_US&access_token={AccessToken}";
            }

            if (realm.Name.ToLower().Contains("commodities") && realm.WowNamespace.Contains("-us"))
            {
                url = $"https://us.api.blizzard.com/data/wow/auctions/commodities?namespace=dynamic-us&access_token={AccessToken}";
            }

            if (realm.Name.ToLower().Contains("commodities") && realm.WowNamespace.Contains("-eu"))
            {
                url = $"https://eu.api.blizzard.com/data/wow/auctions/commodities?namespace=dynamic-eu&access_token={AccessToken}";
            }



            try
            {
                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = null;
                hch.UseProxy = false;

                HttpClient client = new HttpClient(hch);
                client.Timeout = TimeSpan.FromMinutes(1);
                HttpResponseMessage response = await client.GetAsync(url);
                HttpContent content = response.Content;
                auctionsJson = await content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                LogMaker.LogToTable($"{tag}", "WowApi problem.");
                LogMaker.LogToTable($"{tag}", ex.Message);
            }

            if (auctionsJson == string.Empty)
            {
                await Task.Delay(new TimeSpan(0, 1, 0));
                try
                {
                    HttpClient client = new();
                    client.Timeout = TimeSpan.FromMinutes(1);
                    HttpResponseMessage response = await client.GetAsync(url);
                    HttpContent content = response.Content;
                    auctionsJson = await content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    LogMaker.LogToTable($"{tag}", "WowApi crash found again after 2 minutes.");
                    LogMaker.LogToTable($"{tag}", ex.Message);
                }
            }
            if (auctionsJson == string.Empty)
            {

                LogMaker.LogToTable($"WowApi", $"Blizzard sent an empty string for {tag}");
            }

            if (auctionsJson.Substring(0, 30).Contains("404"))
            {
                LogMaker.LogToTable($"WowApi", $"404 for {tag}");

                LogMaker.LogToTable($"WowApi", $"{url}");
            }

            return auctionsJson;
        }

        public static async Task<int> GetConnectedRealmId(string wowNamespace, string realmName)
        {
            await Task.Delay(1);
            string url = $"https://us.api.blizzard.com/data/wow/search/connected-realm?namespace=dynamic-us&realms.name.en_US={realmName}&access_token={AccessToken}";

            if (wowNamespace.Contains("-eu"))
                url = $"https://eu.api.blizzard.com/data/wow/search/connected-realm?namespace=dynamic-eu&realms.name.en_US={realmName}&access_token={AccessToken}";

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
                        if (pair.Value.ToString().Contains("connected-realm"))
                        {
                            int realmspot = pair.Value.ToString().IndexOf("connected-realm");
                            string numString = pair.Value.ToString().Substring(realmspot, 20);

                            connectedRealmId = Convert.ToInt32(string.Concat(numString.Where(char.IsNumber)));
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

            if (itemName.Length < 2)
            {
                itemName = "Test Bunny";
            }

            return itemName;

            /*
                // Why are these names held back? Keeping here for reference
                Dictionary<int, string> mappedNames = new();
                mappedNames.Add(123868, "Relic of Shakama");
                mappedNames.Add(123869, "Relic of Elune");
                mappedNames.Add(56054, "Gleaming Flipper");
                mappedNames.Add(54629, "Prickly Thorn");
                mappedNames.Add(60405, "Stubby Bear Tail");
                mappedNames.Add(158078, "Cracked Overlord's Scepter");
                mappedNames.Add(123865, "Relic of Ursol");
                mappedNames.Add(60406, "Blood-Caked Incisors");
                mappedNames.Add(60390, "Reticulated Tissue");
                mappedNames.Add(178149, "Centurion Anima Core");
            */
        }


        private static async Task<Token> GetElibilityToken()
        {
            Token tok = new();

            if (Configurations.BlizzardClientId == null)
            {

                LogMaker.LogToTable($"WowApi", $"Loading of credentials file has failed!");
            }

            if (AccessToken == string.Empty || TokenTimer.ElapsedMilliseconds / 1000 > 3600 || !TokenTimer.IsRunning)
            {
                TokenTimer.Start();
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

                try
                {
                    HttpResponseMessage tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form));
                    var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
                    tok = JsonConvert.DeserializeObject<Token>(jsonContent);
                    holdToken = tok;
                }
                catch (Exception ex)
                {

                    LogMaker.LogToTable($"WowApi", $"------------------- Error getting access token------------------");
                    LogMaker.LogToTable($"WowApi", $"{ex.Message}");
                    return null;
                }

            }
            else
            {
                tok = holdToken;
            }



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
