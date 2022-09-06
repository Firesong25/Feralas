using System.Runtime.InteropServices;

namespace Feralas
{
    public static class Configurations
    {
        public static string BlizzardClientId { get; private set; }
        public static string BlizzardClientPassword { get; private set; }

        public static string PostgresConnectionString { get; private set; }
        public static string CosmosConnectionString { get; private set; }

        public static async Task Init()
        {
            string[] paths = { Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Data", "Configurations.txt" };
            string configurationFile = Path.Combine(paths);

            PostgresConnectionString = string.Empty;

            string serverName = "cleardragon.com";

            string[] configs = File.ReadAllLines(configurationFile);
            foreach (string config in configs)
            {
                if (PostgresConnectionString == string.Empty && config.Contains(serverName))
                {
                    PostgresConnectionString = config;
                }

                if (CosmosConnectionString == string.Empty && config.Contains(serverName))
                {
                    CosmosConnectionString = config;
                }

                bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                if (isLinux)
                {
                    if (BlizzardClientId == null && config.Contains("linux_clientId="))
                    {
                        string[] configStrings = config.Split('=');
                        BlizzardClientId = configStrings[1].Trim();
                    }

                    if (BlizzardClientPassword == null && config.Contains("linux_clientSecret="))
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
