using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

            PostgresConnectionString = String.Empty;

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

                if (BlizzardClientId == null && config.Contains("clientId="))
                {
                    string[] configStrings = config.Split('=');
                    BlizzardClientId = configStrings[1];
                }

                if (BlizzardClientPassword == null && config.Contains("clientSecret="))
                {
                    string[] configStrings = config.Split('=');
                    BlizzardClientPassword = configStrings[1];
                }
            }
        }
    }
}
