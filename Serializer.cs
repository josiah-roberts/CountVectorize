using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace CountVectorize
{
    class Serializer
    {
        private int ArticleCount;
        private NgramResultData NgramStore;
        private ConcurrentQueue<byte[]> OutputStrings;
        private Dictionary<int, int> OutputIndexMappings;

        private List<Task> ProcessTasks;
        private Options Options;

        public Serializer(NgramResultData ngramStore, Options options)
        {
            NgramStore = ngramStore;
            Options = options;
            ArticleCount = ngramStore.Articles.Count;
        }

        public async Task Process()
        {
            OutputStrings = new ConcurrentQueue<byte[]>();
            
            WriteLegend();

            WriteLine($"Spinning up {Environment.ProcessorCount} threads to serialize data...");
            ProcessTasks = Enumerable.Range(0, Environment.ProcessorCount).Select(x => Task.Run(() => ProcessNgrams())).ToList();
            ProcessTasks.Add(Task.Run(() => WriteData()));

            await Task.WhenAll(ProcessTasks.ToArray());
        }

        private void WriteLegend()
        {
            WriteLine("Writing legend...");
            var wordTable = NgramStore.Words.Reverse();
            OutputIndexMappings = new Dictionary<int, int>();
            int featureCount = 0;
            
            using (var file = File.OpenWrite(Options.LegendFile))
            using (var deflate = GetOutStream(file))
            {
                foreach (var ngramIndex in NgramStore.NgramLookup.Keys)
                {
                    if (NgramStore.NgramCounts[ngramIndex] >= Options.CutoffPercent * (double)ArticleCount)
                    {
                        var mapping = ++featureCount;
                        OutputIndexMappings[ngramIndex] = mapping;
                        long ngramLong = NgramStore.NgramLookup[ngramIndex];
                        string ngramString = NgramToWords(ngramLong, wordTable);
                        deflate.Write(Encoding.ASCII.GetBytes(mapping + "=" + ngramString + "\n"));
                    }
                }
            }
            WriteLine($"Wrote legend of {featureCount} n-grams above the {Options.CutoffPercent} cutoff");
        }

        private Stream GetOutStream(Stream file)
        {
            if (Options.Compress)
                return new GZipStream(file, CompressionMode.Compress);
            else
                return file;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string NgramToWords(long ngram, Dictionary<uint, string> words)
        {
            var output = "";
            while (ngram != 0)
            {
                uint word = (uint)ngram & 0x3FFFF;
                ngram = ngram >> 18;
                output += words[word] + " ";
            }
            return output.TrimEnd();
        }

        private void WriteData()
        {
            var ascii = Encoding.ASCII;
            using (var file = File.OpenWrite(Options.DataFile))
            {
                List<byte> bytes = new List<byte>();
                while (ProcessTasks.Count(x => x.Status == TaskStatus.RanToCompletion) < ProcessTasks.Count - 1 || OutputStrings.Count > 0)
                {
                    if (OutputStrings.TryDequeue(out byte[] toWrite))
                    {
                        bytes.AddRange(toWrite);
                        if (bytes.Count > 268435456)
                        {
                            var array = bytes.ToArray();
                            bytes.Clear();
                            file.Write(array, 0, array.Length);
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                var bArray = bytes.ToArray();
                file.Write(bArray, 0, bArray.Length);
            }
        }

        private void ProcessNgrams()
        {
            while (NgramStore.Articles.TryDequeue(out byte[] compressed))
            {
                SerializeBytes(compressed);
            }
        }

        private void SerializeBytes(byte[] compressed)
        {
            using (var stream = new MemoryStream())
            using (var deflate = GetOutStream(stream))
            {
                var lastIndex = 1;

                var items = compressed.Inflate();
                var articleNumber = BitConverter.ToInt32(items, 0);

                deflate.Write(Encoding.ASCII.GetBytes(articleNumber.ToString()));

                for (int i = 32; i < items.Length; i += 48)
                {
                    var item = Ngram.FromByteArray(items, i);

                    if (NgramStore.NgramCounts[item.Index] < Options.CutoffPercent * (double)ArticleCount)
                        continue;

                    if (i != 0)
                        deflate.Write(Encoding.ASCII.GetBytes(" "));

                    var mapping = OutputIndexMappings[item.Index];
                    deflate.Write(Encoding.ASCII.GetBytes(mapping + "=" + item.Count));
                    lastIndex = item.Index;
                }

                deflate.Write(Encoding.ASCII.GetBytes("\n"));
                deflate.Dispose();

                if (NgramStore.Articles.Count != 0 && NgramStore.Articles.Count % 1000 == 0)
                    WriteLine($"Writing an item. {NgramStore.Articles.Count} remaining.");

                OutputStrings.Enqueue(stream.ToArray());
            }
        }

        private static byte[] RepeatBytes(string input, int count)
        {
            return Encoding.ASCII.GetBytes(new StringBuilder().Insert(0, input, count).ToString());
        }
    }
}
