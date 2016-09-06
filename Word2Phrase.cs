// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Athena
{
    class Word2Phrase
    {
        private const string input_file = "corpus_0.txt";
        private const string output_file = "corpus_1.txt";
        private const int threshold = 100;
        private Dictionary<string, int> vocab = new Dictionary<string, int>();
        private long train_words = 0;

        public Word2Phrase()
        {
            Learn();
            Save();
        }

        private void Learn()
        {
            Console.WriteLine("> Learning vocabulary [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(input_file).Length;
            using (var sr = new StreamReader(input_file))
            {
                var line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    string last_word = null;
                    foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                    {
                        train_words++;
                        if (!vocab.ContainsKey(word)) vocab.Add(word, 1);
                        else vocab[word]++;
                        if (last_word != null && last_word != "NUMERIC_VALUE" && word != "NUMERIC_VALUE")
                        {
                            string bigram = last_word + "_" + word;
                            if (!vocab.ContainsKey(bigram)) vocab.Add(bigram, 1);
                            else vocab[bigram]++;
                        }
                        last_word = word;
                    }
                    if (vocab.Count > Model.MaxSize) Reduce();
                    Console.Write("> Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                }
            }
            Reduce();
            Console.WriteLine("\r\n");
            Console.WriteLine("> Vocab size: {0}k", vocab.Count / 1000);
            Console.WriteLine();
        }

        private void Save()
        {
            Console.WriteLine("> Building phrases [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(input_file).Length;
            using (var sr = new StreamReader(input_file))
            {
                using (var sw = new StreamWriter(output_file, false))
                {
                    var line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        double word_count = 0;
                        double last_word_count = 0;
                        var bigram_count = 0;
                        string last_word = null;

                        foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                        {
                            var oov = false;
                            if (!vocab.ContainsKey(word)) oov = true;
                            else word_count = vocab[word];

                            if (last_word != null)
                            {
                                var bigram = last_word + "_" + word;
                                if (!vocab.ContainsKey(bigram)) oov = true;
                                else bigram_count = vocab[bigram];
                            }
                            else oov = true;

                            double score = 0;
                            if (!oov) score = ((bigram_count - Model.MinCount) / last_word_count / word_count) * train_words;

                            if (score > threshold) sw.Write("_" + word);
                            else sw.Write(" " + word);

                            last_word = word;
                            last_word_count = word_count;
                        }
                        sw.WriteLine();
                        sw.Flush();
                        Console.Write("> Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                    }
                }
            }
            Console.WriteLine("\r\n");
        }

        private void Reduce()
        {
            var keys = vocab.Keys.ToList();
            foreach (var key in keys)
                if (vocab[key] < Model.MinCount) vocab.Remove(key);
        }
    }
}
