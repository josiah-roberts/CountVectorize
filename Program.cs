using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using static System.Console;

namespace CountVectorize
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = Options.FromArray(args);
            options.Validate();

            NgramResultData ngramData = BuildData(options);
            
            var serializer = new Serializer(ngramData, options);
            serializer.Process().Wait();
            WriteLine("Done.");
        }

        private static NgramResultData BuildData(Options options)
        {
            var articleArrays = FileOperations.LoadArticles(options.SourceFile);
            var ngramStore = new NgramProcessor(options);
            var ngramData = ngramStore.ProcessNgrams(articleArrays);
            WriteLine();
            return ngramData;
        }
    }

    public static class BullshitFuckery
    {
        public static void Write(this Stream fuckwit, byte[] shit)
        {
            fuckwit.Write(shit, 0, shit.Length);
        }

        public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            var dictionary = new Dictionary<TValue, TKey>();
            foreach (var entry in source)
            {
                if (!dictionary.ContainsKey(entry.Value))
                    dictionary.Add(entry.Value, entry.Key);
            }
            return dictionary;
        }

        public static byte[] Deflate(this byte[] raw)
        {
            using (var memory = new MemoryStream())
            using (var stream = new DeflateStream(memory, CompressionMode.Compress))
            {
                stream.Write(raw, 0, raw.Length);
                stream.Dispose();
                return memory.ToArray();
            }
        }

        public static byte[] Inflate(this byte[] img)
        {
            using (var to = new MemoryStream())
            using (var from = new MemoryStream(img))
            using (var compress = new DeflateStream(from, CompressionMode.Decompress))
            {
                compress.CopyTo(to);
                return to.ToArray();
            }
        }
    }
}