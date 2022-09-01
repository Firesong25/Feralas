using System.Runtime.InteropServices;

public static class LogMaker
{
    static string logSpam = string.Empty;
    static string title = string.Empty;
    static string path = @"log.html";

    static void GetTitle()
    {
        if (title == string.Empty)
        {
            string assembly = System.Reflection.Assembly.GetExecutingAssembly().ToString();
            string[] parts = assembly.Split(',');
            string appName = parts[0];
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isLinux)
            {
                string machineName = Environment.MachineName;
                char c = machineName[0];
                string niceName = c.ToString().ToUpper();

                for (int i = 1; i < machineName.Length; i++)
                {
                    niceName += machineName[i];
                }
                title = $"{appName} on {niceName}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string machineName = Environment.MachineName;
                char c = machineName[0];
                string niceName = c.ToString().ToUpper();

                for (int i = 1; i < machineName.Length; i++)
                {
                    string letter = machineName[i].ToString();
                    niceName += letter.ToLower();
                }
                title = $"{appName} on {niceName}";
            }
            else
            {
                title = $"{appName} on {Environment.MachineName}";
            }
            
        }
    }
    public static async void Log(string message)
    {
        if (message == logSpam)
            return;

        logSpam = message;

        //Console.WriteLine(message);

        GetTitle();

        // This text is added only once to the file.
        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine($"<title>{title} - Log</title>");
                sw.WriteLine("Log<br>");
            }
        }

        // Log each new message
        using (StreamWriter sw = File.AppendText(path))
        {
            await sw.WriteLineAsync($"{DateTime.UtcNow.ToLongTimeString()}:- {message} <br>");
        }
    }
}

