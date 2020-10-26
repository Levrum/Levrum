using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DailyXMLAggregator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load configuration
            ConfigurationManager.GetConfigurationPathFromUser();

            // Start polling
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var pollingTask = StartPolling(cancellationTokenSource);

            // Handle user input
            Console.WriteLine("Enter 'c' to view configuration path");
            Console.WriteLine("Enter 'exit' to stop");
            bool run = true;
            while (run)
            {
                string input = Console.ReadLine();
                if (input == "c")
                {
                    ConfigurationManager.GetConfigurationPathFromUser();
                }
                else if (input == "exit")
                {
                    cancellationTokenSource.Cancel();
                    pollingTask.Wait();
                    break;
                }
            }
        }

        public static async Task StartPolling(CancellationTokenSource cancellationTokenSource)
        {            
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var pollingTask = Task.Run(async () =>
            {
                while (true)
                {
                    DataManager.GetFilesToProcess();
                    DataManager.ArchiveData();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await Task.Delay(ConfigurationManager.PollFrequency);
                }
                Console.WriteLine("Polling stopped");
            }, cancellationToken);
            await pollingTask;
        }
    }
}
