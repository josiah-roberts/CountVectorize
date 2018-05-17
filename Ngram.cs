using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CountVectorize
{
    public class NgramResultData
    {
        public ConcurrentQueue<byte[]> Articles { get; set; }
        public Dictionary<int, int> NgramCounts { get; set; }
        public Dictionary<int, long> NgramLookup { get; set; }
        public Dictionary<string, uint> Words { get; set; }
    }

    public class Ngram
    {
        public int Index { get; set; }
        public short Count { get; set; }

        public byte[] ToByteArray()
        {
            byte[] ret = new byte[48];
            BitConverter.GetBytes(Count).CopyTo(ret, 0);
            BitConverter.GetBytes(Index).CopyTo(ret, 16);
            return ret;
        }

        public static Ngram FromByteArray(byte[] input, int offset)
        {
            return new Ngram
            {
                Count = BitConverter.ToInt16(input, offset + 0),
                Index = BitConverter.ToInt32(input, offset + 16)
            };
        }
    }
}
