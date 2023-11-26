using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Benchmarking
{
    public static class Benchmark
    {
        private class Average
        {
            public int current { private set; get; }
            private int n;

            public bool dirty;

            public void Add(int sample)
            {
                if (n >= 60)
                {
                    current = 0;
                    n = 0;
                }
                current = (n * current + sample) / (n + 1);
                n += 1;
                dirty = true;
            }

            public void Read()
            {
                dirty = false;
            }
        }

        private static Dictionary<string, Stopwatch> stopwatches = new();
        private static Dictionary<string, Average> averages = new();

        public static void Start(string id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            stopwatches.Add(id, stopwatch);
        }

        public static void Stop(string id)
        {
            var stopwatch = stopwatches[id];
            stopwatch.Stop();

            TimeSpan timeSpan = stopwatch.Elapsed;
            var ns = timeSpan.Nanoseconds;

            averages.TryAdd(id, new Average());
            averages[id].Add(ns);

            stopwatches.Remove(id);
        }

        public static List<(string, int)> GetNanosecondReport()
        {
            var newAverages = averages.Where(average => !average.Value.dirty);
            foreach (var average in averages)
            {
                average.Value.Read();
            }
            return newAverages.Select(average => (average.Key, average.Value.current)).ToList();
        }
    }
}