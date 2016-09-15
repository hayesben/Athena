// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Athena
{
    internal class Word2Phrase
    {
        private readonly Dictionary<string, int> _vocab = new Dictionary<string, int>();
        private const string InputFile = "corpus_0.txt";
        private const string OutputFile = "corpus_1.txt";
        private const int Threshold = 100;      
        private long _trainWords;

        public Word2Phrase()
        {
            Learn();
            Save();
        }

        private void Learn()
        {
            Console.WriteLine("> Learning vocabulary [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(InputFile).Length;
            using (var sr = new StreamReader(InputFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string lastWord = null;
                    foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                    {
                        _trainWords++;
                        if (!_vocab.ContainsKey(word)) _vocab.Add(word, 1);
                        else _vocab[word]++;
                        if ((lastWord != null) && (lastWord != "NUMERIC_VALUE") && (word != "NUMERIC_VALUE"))
                        {
                            var bigram = lastWord + "_" + word;
                            if (!_vocab.ContainsKey(bigram)) _vocab.Add(bigram, 1);
                            else _vocab[bigram]++;
                        }
                        lastWord = word;
                    }

                    if (_vocab.Count > Model.MaxSize) Reduce();
                    Console.Write("> Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                }
            }

            Reduce();
            Console.WriteLine("\r\n");
            Console.WriteLine("> Vocab size: {0}k", _vocab.Count / 1000);
            Console.WriteLine();
        }

        private void Save()
        {
            Console.WriteLine("> Building phrases [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(InputFile).Length;
            using (var sr = new StreamReader(InputFile))
            {
                using (var sw = new StreamWriter(OutputFile, false))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        double wordCount = 0;
                        double lastWordCount = 0;
                        var bigramCount = 0;
                        string lastWord = null;

                        foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                        {
                            var oov = false;
                            if (!_vocab.ContainsKey(word)) oov = true;
                            else wordCount = _vocab[word];

                            if (lastWord != null)
                            {
                                var bigram = lastWord + "_" + word;
                                if (!_vocab.ContainsKey(bigram)) oov = true;
                                else bigramCount = _vocab[bigram];
                            }
                            else oov = true;

                            double score = 0;
                            if (!oov) score = (bigramCount - Model.MinCount) / lastWordCount / wordCount * _trainWords;

                            if (score > Threshold) sw.Write("_" + word);
                            else sw.Write(" " + word);

                            lastWord = word;
                            lastWordCount = wordCount;
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
            var keys = _vocab.Keys.ToList();
            foreach (var key in keys.Where(key => _vocab[key] < Model.MinCount))
                _vocab.Remove(key);
        }
    }
}