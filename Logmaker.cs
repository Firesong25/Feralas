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
                title = $"{appName} on Linux";
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

