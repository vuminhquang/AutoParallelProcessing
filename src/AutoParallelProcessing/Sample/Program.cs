using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoParallelProcessing;

namespace ParallelProcessingApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<WorkItem> workItems = Enumerable.Range(0, 100).Select(i => new WorkItem
            {
                Id = i,
                ProcessDelay = RandomDelay(),
                Configuration = new ProcessConfig { Multiplier = i % 5 + 1 }
            }).ToList();

            Console.WriteLine("Starting parallel processing with ForEachAsync...");

            // Measure performance
            Stopwatch stopwatch = Stopwatch.StartNew();
            await workItems.ForEachAsync(10, ProcessWorkItem, "Additional Parameter 1", 42);
            stopwatch.Stop();

            Console.WriteLine($"Processing completed in {stopwatch.ElapsedMilliseconds} ms.");
        }

        private static async Task ProcessWorkItem(WorkItem item, object[] additionalParameters)
        {
            // Extract additional parameters
            string additionalParam1 = (string)additionalParameters[0];
            int additionalParam2 = (int)additionalParameters[1];

            // Simulate CPU-bound work
            double result = Math.Sqrt(item.Id) * item.Configuration.Multiplier;

            // Simulate I/O-bound work with random delay
            await Task.Delay(item.ProcessDelay);

            Console.WriteLine($"Processed Item {item.Id} with result {result:F2} in {item.ProcessDelay} ms. " +
                              $"Additional Params: {additionalParam1}, {additionalParam2}");
        }

        private static int RandomDelay()
        {
            Random random = new Random();
            return random.Next(100, 500); // Generates a random delay between 100 and 500 milliseconds
        }
    }

    class WorkItem
    {
        public int Id { get; set; }
        public int ProcessDelay { get; set; }
        public ProcessConfig Configuration { get; set; }
    }

    class ProcessConfig
    {
        public int Multiplier { get; set; }
    }
}