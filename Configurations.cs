using System.Runtime.InteropServices;

namespace Feralas
{
    public static class Configurations
    {
        public static string BlizzardClientId { get; private set; }
        public static string BlizzardClientPassword { get; private set; }

        public static string DigitalOceanConnectionString { get; private set; }

        public static string OVHConnectionString { get; private set; }
        public static string CosmosConnectionString { get; private set; }

        public static async Task Init()
        {
            string[] paths = { Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Data", "Configurations.txt" };
            string configurationFile = Path.Combine(paths);

            DigitalOceanConnectionString = string.Empty;
            OVHConnectionString = string.Empty;

            string serverName = "cleardragon.com";
            string ovhServerName = "328e252d";

            string[] configs = File.ReadAllLines(configurationFile);
            foreach (string config in configs)
            {
                if (DigitalOceanConnectionString == string.Empty && config.Contains(serverName))
                {
                    DigitalOceanConnectionString = config;
                }

                if (OVHConnectionString == string.Empty && config.Contains(ovhServerName))
                {
                    OVHConnectionString = config;
                }

                if (CosmosConnectionString == string.Empty && config.Contains(serverName))
                {
                    CosmosConnectionString = config;
                }

                bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                if (isLinux)
                {
                    if (BlizzardClientId == null && config.Contains("linux_client_id"))
                    {
                        string[] configStrings = config.Split('=');
                        BlizzardClientId = configStrings[1].Trim();
                    }

                    if (BlizzardClientPassword == null && config.Contains("linux_client_secret="))
                    {
                        string[] configStrings = config.Split('=');
                        BlizzardClientPassword = configStrings[1].Trim();
                    }
                }
                else
                {
                    if (BlizzardClientId == null && config.Contains("clientId="))
                    {
                        string[] configStrings = config.Split('=');
                        BlizzardClientId = configStrings[1].Trim();
                    }

                    if (BlizzardClientPassword == null && config.Contains("clientSecret="))
                    {
                        string[] configStrings = config.Split('=');
                        BlizzardClientPassword = configStrings[1].Trim();
                    }
                }
            }
            await Task.Delay(1); // stop Linux warnings
        }
    }
}
