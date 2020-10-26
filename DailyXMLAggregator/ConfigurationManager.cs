using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DailyXMLAggregator
{
    public static class ConfigurationManager
    {
        public static FileInfo ConfigurationFile { get; set; } = new FileInfo(@"./configuration.txt");
        public static DirectoryInfo SourceDirectory { get; set; }
        public static DirectoryInfo ArchiveDirectory { get; set; }
        public static int PollFrequency { get; set; }
        public static bool ConfigurationLoaded { get; set; } = false;

        public static void LoadConfiguration()
        {
            if (!ConfigurationFile.Exists)
            {
                throw new FileNotFoundException("The configuration path is invalid.");
            }

            using (StreamReader reader = ConfigurationFile.OpenText())
            {
                string sourcePath;
                string archivePath;
                string pollFrequencyString;

                try
                {
                    sourcePath = reader.ReadLine();
                    archivePath = reader.ReadLine();
                    pollFrequencyString = reader.ReadLine();
                }
                catch (Exception ex)
                {
                    ConfigurationLoaded = false;
                    throw ex;
                }

                if (!Directory.Exists(sourcePath) || !Directory.Exists(archivePath) || !int.TryParse(pollFrequencyString, out int pollFrequency))
                {
                    throw new Exception("Configuration file invalid.");
                }
                else
                {
                    SourceDirectory = new DirectoryInfo(sourcePath);
                    ArchiveDirectory = new DirectoryInfo(archivePath);
                    PollFrequency = pollFrequency;
                    ConfigurationLoaded = true;
                }
            }
        }

        public static void GetConfigurationPathFromUser()
        {
            Console.WriteLine($"Current configuration path: {ConfigurationFile.FullName}");
            Console.Write("Would you like to change the configuration path? y/n: ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key == ConsoleKey.Y)
            {
                string path = Console.ReadLine();
                ConfigurationFile = new FileInfo(path);
                Console.WriteLine($"Configuration path successfully changed to {ConfigurationFile.FullName}");
            }

            LoadConfiguration();
        }
    }
}
