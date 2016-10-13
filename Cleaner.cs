// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Athena
{
    internal class Cleaner
    {
        private const string InputFile = "corpus.txt";
        private const string OutputFile = "corpus_0.txt";
        private const string FinalFile = "corpus_1.txt";
        private const string IngestFile = "ingest.txt";

        public Cleaner()
        {
            Console.WriteLine("Cleaning corpus [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(InputFile).Length;
            using (var sr = new StreamReader(InputFile))
            {
                using (var sw = new StreamWriter(OutputFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        ProcessLine(sw, line);
                        Console.Write("Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                    }
                }
            }

            Console.WriteLine("\r\n");
        }

        private static void ProcessLine(TextWriter sw, string line)
        {
            line = Regex.Replace(line, @"(([.?!][\r\n ])|[\r\n])+", "\r\n");
            var array = line.Split(new[] { (char)10, (char)13 }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var text in array.Select(CleanText).Where(text => text.Length > 14))
                sw.WriteLine(text);
        }

        private static string CleanText(string text)
        {
            // Pad and convert to lower case.
            text = " " + text.ToLower(CultureInfo.InvariantCulture) + " ";

            // Standardise end of document tags.
            text = Regex.Replace(text, "===eod===", "END_OF_DOCUMENT");

            // Remove apostrophes.
            text = Regex.Replace(text, "[’']", "");

            // Standardise diacritics.
            text = Regex.Replace(text, "[àáâãäå]", "a");
            text = Regex.Replace(text, "[ç]", "c");
            text = Regex.Replace(text, "[èéêë]", "e");
            text = Regex.Replace(text, "[ìíîï]", "i");
            text = Regex.Replace(text, "[ñ]", "n");
            text = Regex.Replace(text, "[òóôõöø]", "o");
            text = Regex.Replace(text, "[ùúûü]", "u");
            text = Regex.Replace(text, "[ýÿ]", "y");

            // Remove none alpha-numerics and spaces.
            text = Regex.Replace(text, @"[^a-zA-Z0-9 _]", " ");

            // Remove free standing numbers (not 4 in length).
            text = Regex.Replace(text, @"\s[0-9]{1,3}(?=\s)|\s[0-9]{5,}(?=\s)", " NUMERIC_VALUE ");

            // Remove 4 digit numbers that are not years 1000 - 2999.
            text = Regex.Replace(text, @"\s[03-9][0-9]{3}(?=\s)", " NUMERIC_VALUE ");

            // Remove single free standing letters (not 'a' or 'i').
            text = Regex.Replace(text, @"\s[^ai](?=\s)", " ");

            // Remove multiple numeric values.
            text = Regex.Replace(text, @"(\bNUMERIC_VALUE\b)\s+(\1(\s+|$))+", "$1 ");

            // Remove multiple spaces.
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }

        public static void Ingest(Model model)
        {
            using (var sr = new StreamReader(IngestFile))
            {
                using (var sw = new StreamWriter(InputFile, true))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        sw.WriteLine(line);
                    sw.WriteLine("===eod===");
                }
            }

            var start = new FileInfo(OutputFile).Length;
            using (var sr = new StreamReader(IngestFile))
            {
                using (var sw = new StreamWriter(OutputFile, true))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        ProcessLine(sw, line);
                    sw.WriteLine("END_OF_DOCUMENT");
                }
            }

            var stream = new FileStream(OutputFile, FileMode.Open);
            stream.Seek(start, SeekOrigin.Begin);
            using (var sr = new StreamReader(stream))
            {
                using (var sw = new StreamWriter(FinalFile, true))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string last_word = null;
                        foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                        {
                            string bigram = last_word + "_" + word;
                            if (model.ContainsKey(bigram)) sw.Write("_" + word);
                            else sw.Write(" " + word);

                            last_word = word;
                        }
                        sw.WriteLine();
                    }
                }
            }
            File.Delete(IngestFile);
        }

        public static void ReplaceInFile(string pattern)
        {
            var checkpoint = DateTime.Now;
            double length = new FileInfo(FinalFile).Length;
            byte[] find = Encoding.ASCII.GetBytes(pattern);
            byte[] replace = Encoding.ASCII.GetBytes(pattern.Replace(' ', '_'));
            byte[] buffer = new byte[find.Length];

            Console.WriteLine("Replacing text [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();

            using (Stream stream = File.Open(FinalFile, FileMode.Open))
            {
                using (BufferedStream bs = new BufferedStream(stream, find.Length))
                {
                    int i;
                    while ((i = bs.Read(buffer, 0, find.Length)) == find.Length)
                    {
                        if (find.SequenceEqual(buffer))
                        {
                            bs.Position -= find.Length;
                            bs.Write(replace, 0, find.Length);
                        }
                        else bs.Position -= find.Length - Pad(buffer, find);

                        if (checkpoint < DateTime.Now)
                        {
                            Console.Write("Progress: {0:0.000%}  \r", bs.Position / length);
                            checkpoint = DateTime.Now.AddSeconds(1);
                        }
                    }
                }
            }
            Console.WriteLine("Finish [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
        }

        private static int Pad(byte[] buffer, byte[] find)
        {
            int i = 1;
            while (i < buffer.Length)
            {
                int n = buffer.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(buffer, i, aux1, 0, n);
                Array.Copy(find, aux2, n);
                if (aux1.SequenceEqual(aux2)) return i;
                i++;
            }
            return i;
        }
    }
}