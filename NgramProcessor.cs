using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.IO;
using static System.Console;

namespace CountVectorize
{
    public class NgramProcessor
    {
        private Options Options;

        public Dictionary<long, int> NgramIndexes = new Dictionary<long, int>();
        public Dictionary<int, int> NgramCounts = new Dictionary<int, int>();

        private int lastNgramIndex = 0;
        private object ngramLock = new object();

        public Dictionary<string, uint> Words;
        private uint lastWordIndex;
        private object wordLock = new object();

        int total;
        int complete;

        internal NgramProcessor(Options options)
        {
            Options = options;
        }

        public NgramResultData ProcessNgrams(IEnumerable<IEnumerable<string>> articles)
        {
            WriteLine("Building word table...");
            Words = new Dictionary<string, uint>();
            var words = new ConcurrentQueue<Tuple<uint[], int>>(articles.Select((x, i) => new Tuple<uint[], int>(GetWords(x), i)));
            total = words.Count;

            var output = new ConcurrentQueue<byte[]>();

            var tasks = new List<Task>();

            WriteLine($"Spinning up {Environment.ProcessorCount} threads to process ngrams...");
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (words.TryDequeue(out Tuple<uint[], int> article))
                        output.Enqueue(GetNgrams(article.Item1, article.Item2));
                }));
            }

            Task.WaitAll(tasks.ToArray());

            WriteLine($"Finished vectorizing {NgramCounts.Count.ToString("N0")} n-grams for {output.Count.ToString("N0")} articles.");

            return new NgramResultData
            {
                Articles = output,
                NgramCounts = NgramCounts,
                NgramLookup = NgramIndexes.Reverse(),
                Words = Words
            };
        }

        uint[] GetWords(IEnumerable<string> strings)
        {
            var output = new LinkedList<uint>();

            foreach (var item in strings)
            {
                uint itemId;
                if (!Words.TryGetValue(item, out itemId))
                {
                    Words[item] = itemId = ++lastWordIndex;
                }
                output.AddLast(itemId);
            }

            return output.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        long Ngram(params uint[] words)
        {
            long ngram = 0;
            for (int i = 0; i < words.Length; i++)
            {
                ngram = ngram << 18;
                var word = words[i];
                ngram = ngram | word;
            }

            return ngram;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte[] GetNgrams(uint[] article, int articleNumber)
        {
            Dictionary<long, Ngram> ngrams = new Dictionary<long, Ngram>();

            foreach (var size in Options.Orders)
            {
                var length = article.Length;
                for (int i = 0; i < length - size; i++)
                {
                    uint[] words = new uint[size];
                    for (int offset = 0; offset < size; offset++)
                    {
                        words[offset] = article[i + offset];
                    }
                    var ngramId = Ngram(words);

                    if (ngrams.ContainsKey(ngramId))
                    {
                        ngrams[ngramId].Count++;
                    }
                    else
                    {
                        int ngramIndex = 0;
                        lock (ngramLock)
                        {
                            if (!NgramIndexes.TryGetValue(ngramId, out ngramIndex))
                            {
                                NgramIndexes[ngramId] = ngramIndex = lastNgramIndex++;
                            }
                        }

                        ngrams[ngramId] = new Ngram { Count = 1, Index = ngramIndex };
                        lock (NgramCounts)
                        {
                            NgramCounts.TryGetValue(ngramIndex, out int ngramCount);
                            NgramCounts[ngramIndex] = ++ngramCount;
                        }
                    }
                }
            }

            var raw = new byte[32 + 48 * ngrams.Count];
            BitConverter.GetBytes(articleNumber).CopyTo(raw, 0);
            int byteOffset = 32;
            foreach (var item in ngrams)
            {
                item.Value.ToByteArray().CopyTo(raw, byteOffset);
                byteOffset += 48;
            }

            if (++complete % 1000 == 0)
                Console.WriteLine($"Finished n-gramming {complete} of {total}");

            var ret = raw.Deflate();
            return ret;
        }
    }
}
