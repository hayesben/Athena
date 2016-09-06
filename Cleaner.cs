﻿using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Athena
{
    public class Cleaner
    {
        private const string input_file = "corpus.txt";
        private const string output_file = "corpus_0.txt";

        public Cleaner()
        {
            Console.WriteLine("> Cleaning corpus [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(input_file).Length;
            using (var sr = new StreamReader(input_file))
            {
                using (var sw = new StreamWriter(output_file))
                {
                    var line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = ProcessLine(sw, line);
                        Console.Write("> Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                    }
                }
            }
            Console.WriteLine("\r\n");
        }

        private string ProcessLine(StreamWriter sw, string line)
        {
            line = Regex.Replace(line, @"(([.?!][\r\n ])|[\r\n])+", "\r\n");
            var array = line.Split(new char[] { (char)10, (char)13 }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < array.Length; i++)
            {
                var text = CleanText(array[i]);
                if (text.Length > 14) sw.WriteLine(text);
            }
            return line;
        }

        private string CleanText(string text)
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
    }
}