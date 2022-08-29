using System;
using System.IO;

namespace Feralas
{
    public static class LogMaker
    {
        static string logSpam;
        static string path = @"log.html";
        public static async void Log(string message)
        {
            if (message == logSpam)
                return;

            logSpam = message;

            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("<title>Feralas - Log</title>");
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
}

