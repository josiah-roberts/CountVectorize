using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace CountVectorize
{
    static class FileOperations
    {
        static bool started = false;
        public static IEnumerable<string[]> LoadArticles(string filePath)
        {
            if (!started)
            {
                started = true;
                Console.WriteLine("Loading data...");
            }
            using (var reader = new StreamReader(File.OpenRead(filePath)))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    line = line.Substring(line.IndexOf(',') + 4);
                    line = line.Substring(0, line.Length - 1);
                    yield return line.Split(' ');                
                }
            }                
        }
    }
}
