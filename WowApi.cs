﻿using Newtonsoft.Json;
using System.Diagnostics;

namespace Feralas
{
    public static class WowApi
    {
        static Token holdToken = new();
        static string AccessToken = string.Empty;
        static Stopwatch TokenTimer = new();
        static Dictionary<string, int> CachedConnectedRealmIds = new();
        public static async Task<string> GetRealmAuctions(string realmName, string wowNamespace, string tag)
        {
            string auctionsJson = string.Empty;

            Token tok = await GetElibilityToken();

            if (tok.AccessToken == null)
            {
                //LogMaker.LogToTable($"WowApi", $"Access token refused.  Use the old one...");
                AccessToken = "USJTaraEEIsuGXHXvMnCOvDeJMVDh7ZSJg";
            }
            else
            {
                AccessToken = tok.AccessToken.ToString();
            }

            int connectedRealmId = 0;

            List<Auction> auctions = new List<Auction>();
            //CachedConnectedRealmIds.Add("First one", 1);

            if (!realmName.ToLower().Contains("commodities"))
            {
                try
                {
                    if (CachedConnectedRealmIds.ContainsKey(tag))
                    { 
                        connectedRealmId = CachedConnectedRealmIds[tag];
                    }
                    else
                    {
                        connectedRealmId = await GetConnectedRealmId(wowNamespace, realmName);
                        CachedConnectedRealmIds.Add(tag, connectedRealmId);
                    }
                    
                }
                catch (Exception ex)
                {
                    LogMaker.LogToTable($"WowApi", $"WowApi->GetConnectedRealmId ------------- {ex.Message}");
                }

                if (connectedRealmId == 0)
                {
                    LogMaker.LogToTable($"WowApi", $"WowApi->GetConnectedRealmId -------------Failed to get connected realm id for {tag}.");
                    return string.Empty;
                }
                else
                {
                    //LogMaker.LogToTable($"WowApi", $"Connected realm id for {tag} is {connectedRealmId}.");
                }
            }


            string url = $"https://us.api.blizzard.com/data/wow/connected-realm/{connectedRealmId}/auctions?namespace={wowNamespace}&locale=en_US&access_token={AccessToken}";

            if (wowNamespace.Contains("-eu"))
            {
                url = $"https://eu.api.blizzard.com/data/wow/connected-realm/{connectedRealmId}/auctions?namespace=dynamic-eu&locale=en_US&access_token={AccessToken}";
            }

            if (realmName.ToLower().Contains("commodities") && wowNamespace.Contains("-us"))
            {
                url = $"https://us.api.blizzard.com/data/wow/auctions/commodities?namespace=dynamic-us&access_token={AccessToken}";
            }

            if (realmName.ToLower().Contains("commodities") && wowNamespace.Contains("-eu"))
            {
                url = $"https://eu.api.blizzard.com/data/wow/auctions/commodities?namespace=dynamic-eu&access_token={AccessToken}";
            }

        

            try
            {
                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = null;
                hch.UseProxy = false;

                HttpClient client = new HttpClient(hch);
                client.Timeout = TimeSpan.FromMinutes(10);
                HttpResponseMessage response = await client.GetAsync(url);
                HttpContent content = response.Content;
                auctionsJson = await content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                LogMaker.LogToTable($"WowApi", "WowApi crash found.");
                LogMaker.LogToTable($"WowApi", ex.Message);
            }

            if (auctionsJson == string.Empty)
            {
                await Task.Delay(new TimeSpan(0, 1, 0));
                try
                {
                    HttpClient client = new();
                    client.Timeout = TimeSpan.FromMinutes(10);
                    HttpResponseMessage response = await client.GetAsync(url);
                    HttpContent content = response.Content;
                    auctionsJson = await content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    LogMaker.LogToTable($"WowApi", "WowApi crash found again after 60 second delay.");
                    LogMaker.LogToTable($"WowApi", ex.Message);
                }
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
