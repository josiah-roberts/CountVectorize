using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CountVectorize
{
    class Options
    {
        public string DataFile { get; set; }
        public string LegendFile { get; set; }
        public string SourceFile { get; set; }
        public double CutoffPercent { get; set; } = 0.08;
        public bool Compress { get; set; } = true;
        public int[] Orders { get; set; } = new int[] { 1, 2, 3 };

        private static readonly string[] requiredArgs = new [] {
            "input",
            "datafile",
            "legendfile"
        };

        internal static Options FromArray(string[] args)
        {
            if (args.Any(x => x.ToLower() == "--help"))
            {
                PrintRequiredArgs();
                PrintOptionalArgs();
                Environment.Exit(0);
            }
            
            var ret = new Options();
            var iterator = args.GetEnumerator();
            while(iterator.MoveNext())
            {
                var argname = (string)iterator.Current;
                iterator.MoveNext();
                var argValue = (string)iterator.Current;

                switch (argname.ToLower())
                {
                    case "--input":
                        ret.SourceFile = argValue;
                        break;
                    case "--datafile":
                        ret.DataFile = argValue;
                        break;
                    case "--legendfile":
                        ret.LegendFile = argValue;
                        break;
                    case "--cutoffpercent":
                        ret.CutoffPercent = double.Parse(argValue);
                        break;
                    case "--compress":
                        ret.Compress = bool.Parse(argValue);
                        break;
                    case "--orders":
                        ret.Orders = argValue.Split(',').Select(int.Parse).ToArray();
                        break;
                }
            }

            return ret;
        }

        private static void PrintRequiredArgs()
        {
            Console.WriteLine("\nThe following arguments are required:");
            foreach (var required in requiredArgs)
            {
                Console.WriteLine($"--{required} <{required} path>");
            }
        }

        private static void PrintOptionalArgs()
        {
            Console.WriteLine("\nThe following arguments are optional:");
            Console.WriteLine("--cutoffpercent <percent>\n  Defaults to 0.08");
            Console.WriteLine("--compress <true/false>\n  Enables/disables gzip output compression. Defaults to true.");
            Console.WriteLine("--orders <1,2,3>\n  Specifies the n-gram sizes you want. Defaults to 1,2,3.");
        }

        internal void Validate()
        {
            if (LegendFile.ToLower() == DataFile.ToLower())
            {
                Console.WriteLine("Cannot have same legend and data file");
                Environment.Exit(-1);
            }

            if (File.Exists(LegendFile) || File.Exists(DataFile))
            {
                File.Delete(LegendFile);
                File.Delete(DataFile);
            }

            if (!File.Exists(SourceFile))
            {
                Console.WriteLine($"Source file {SourceFile} does not exist");
                Environment.Exit(-1);
            }

            if (Orders.Any(x => x > 3))
            {
                Console.WriteLine($"Cannot have ngrams greater than 3");
            }

            if (Orders.Distinct().Count() < Orders.Length)
            {
                Console.WriteLine($"Cannot repeat ngrams");
            }
        }
    }
}
