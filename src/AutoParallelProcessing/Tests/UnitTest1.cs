using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using AutoParallelProcessing;
using Xunit.Abstractions;

namespace ParallelProcessingApp.Tests
{
    public class ParallelProcessingTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static readonly Random Random = new Random();
        private const int TotalIterations = 10000; // Reduced for testing purposes
        private const int NumberOfRuns = 10; // Reduced for testing purposes

        private readonly HashSet<int> inputSet = new HashSet<int>(Enumerable.Range(0, TotalIterations));

        public ParallelProcessingTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task CompareProcessingMethodsPerformance()
        {
            var results = new List<(string MethodName, double Mean, double Error, double StdDev)>
            {
                // await MeasureExecutionTime("Sequential Processing", ExecuteInSequence),
                // await MeasureExecutionTime("Parallel.ForAsync Processing", ExecuteInParallelAsync),
                // await MeasureExecutionTime("Parallel.For with Chunking Processing", ExecuteInParallelWithChunking),
                await MeasureExecutionTime("Pure Task.WhenAll Processing", ExecuteWithTaskWhenAll),
                await MeasureExecutionTime("Parallel.ForEachAsync Processing", 
                    () => inputSet.ForEachAsync(null, ProcessTask))
            };

            _testOutputHelper.WriteLine("Performance Comparison:");
            foreach (var result in results)
            {
                _testOutputHelper.WriteLine($"{result.MethodName}: Mean = {result.Mean:F2} ms, Error = {result.Error:F2} ms, StdDev = {result.StdDev:F2} ms");
            }

            var bestMethod = results.OrderBy(r => r.Mean).First();
            _testOutputHelper.WriteLine($"Best Method: {bestMethod.MethodName} with Mean Time = {bestMethod.Mean:F2} ms");

            // Assert that the custom Parallel.ForEachAsync is the best method
            Assert.Equal("Parallel.ForEachAsync Processing", bestMethod.MethodName);
        }

        private static async Task<(string MethodName, double Mean, double Error, double StdDev)> MeasureExecutionTime(string methodName, Func<Task> method)
        {
            long[] runTimes = new long[NumberOfRuns];

            for (int i = 0; i < NumberOfRuns; i++)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                await method();
                stopwatch.Stop();
                runTimes[i] = stopwatch.ElapsedMilliseconds;
            }

            double mean = runTimes.Average();
            double stdDev = Math.Sqrt(runTimes.Average(v => Math.Pow(v - mean, 2)));
            double error = stdDev / Math.Sqrt(NumberOfRuns);

            return (methodName, mean, error, stdDev);
        }

        private static async Task ExecuteInSequence()
        {
            for (int i = 0; i < TotalIterations; i++)
            {
                await ProcessTask(i);
            }
        }

        private static async Task ExecuteInParallelAsync()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < TotalIterations; i++)
            {
                tasks.Add(ProcessTask(i));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task ExecuteInParallelWithChunking()
        {
            int chunkSize = (TotalIterations + Environment.ProcessorCount - 1) / Environment.ProcessorCount;
            var chunkTasks = new Task[(TotalIterations + chunkSize - 1) / chunkSize]; // Adjust for total iterations

            Parallel.For(0, chunkTasks.Length, chunk =>
            {
                chunkTasks[chunk] = Task.Run(async () =>
                {
                    for (int i = chunk * chunkSize; i < Math.Min((chunk + 1) * chunkSize, TotalIterations); i++)
                    {
                        await ProcessTask(i);
                    }
                });
            });

            await Task.WhenAll(chunkTasks);
        }

        private static async Task ExecuteWithTaskWhenAll()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < TotalIterations; i++)
            {
                tasks.Add(ProcessTask(i));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task ProcessTask(int i)
        {
            // Simulate CPU-bound work
            double result = Math.Sqrt(i);

            // Simulate I/O-bound work with random delay
            await Task.Delay(RandomDelay());
        }

        private static int RandomDelay()
        {
            return (int)(1000 + Random.NextDouble() * 500) / 1000; // Generates a random delay between 1000 and 1500 microseconds (1 to 1.5 milliseconds)
        }
    }
}