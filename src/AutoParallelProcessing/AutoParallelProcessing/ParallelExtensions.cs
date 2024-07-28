namespace AutoParallelProcessing
{
    public static class ParallelExtensions
    {
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, int? degreeOfParallelism, Func<T, object[], Task> body, params object[] additionalParameters)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            // Set degree of parallelism to number of logical processors if not specified
            var actualDegreeOfParallelism = degreeOfParallelism ?? Environment.ProcessorCount;

            if (actualDegreeOfParallelism <= 0)
                throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism),
                    "Degree of parallelism must be greater than zero.");

            var chunks = SplitCollection(source, actualDegreeOfParallelism);

            await Parallel.ForEachAsync(chunks, new ParallelOptions { MaxDegreeOfParallelism = actualDegreeOfParallelism },
                async (chunk, _) => { 
                    await Task.WhenAll(chunk.Select(item => body(item, additionalParameters))).ConfigureAwait(false); 
                });
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, int? degreeOfParallelism, Func<T, Task> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            await source.ForEachAsync(degreeOfParallelism, (item, _) => body(item));
        }

        public static void ForEach<T>(this IEnumerable<T> source, int? degreeOfParallelism, Action<T, object[]> body, params object[] additionalParameters)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            // Set degree of parallelism to number of logical processors if not specified
            var actualDegreeOfParallelism = degreeOfParallelism ?? Environment.ProcessorCount;
        
            var chunks = SplitCollection(source, actualDegreeOfParallelism);

            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = actualDegreeOfParallelism }, chunk =>
            {
                foreach (var item in chunk)
                {
                    body(item, additionalParameters);
                }
            });
        }

        private static List<List<T>> SplitCollection<T>(IEnumerable<T> source, int degreeOfParallelism)
        {
            var sourceList = source.ToList();
            var totalItems = sourceList.Count;

            // Base chunk size (can be adjusted based on your specific use case)
            var baseChunkSize = 1000;

            // Adjust the chunk size dynamically
            var targetChunkSize = CalculateTargetChunkSize(totalItems, degreeOfParallelism, baseChunkSize);

            // Calculate the number of chunks needed
            var numberOfChunks = Math.Max(degreeOfParallelism, (totalItems + targetChunkSize - 1) / targetChunkSize);

            return sourceList
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index % numberOfChunks)
                .Select(g => g.Select(x => x.Item).ToList())
                .ToList();
        }

        private static int CalculateTargetChunkSize(int totalItems, int degreeOfParallelism, int baseChunkSize)
        {
            // Example logic to adjust the chunk size based on totalItems and degreeOfParallelism
            // This can be customized further based on the nature of the workload

            // Increase the chunk size if the total number of items is large
            if (totalItems > 100000)
            {
                baseChunkSize *= 2;
            }

            // Decrease the chunk size if the degree of parallelism is high
            if (degreeOfParallelism > 8)
            {
                baseChunkSize /= 2;
            }

            // Ensure the chunk size is not too small or too large
            baseChunkSize = Math.Max(baseChunkSize, 500); // Minimum chunk size
            baseChunkSize = Math.Min(baseChunkSize, 10000); // Maximum chunk size

            return baseChunkSize;
        }
    }
}