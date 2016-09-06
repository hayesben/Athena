// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;

namespace Athena
{
    class Word2Vec
    {
        private readonly Random rnd = new Random();
        private const string input_file = "corpus_1.txt";        
        private const double alpha = 0.05;
        private const int window = 5;
        private const int negs = 5;
        private string[] roulette;
        private int rouletteLength;
        private Model model;

        public Word2Vec()
        {
            model = new Model();
            BuildRoulette();
            Train();
            model.Save();
        }

        private void BuildRoulette()
        {
            var tmp = new List<string>();
            var div = Math.Pow(Model.MinCount, 0.6);
            foreach (var word in model)
            {
                var count = (int)(Math.Pow(word.Value.Count, 0.6) / div);
                for (var i = 0; i < count; i++) tmp.Add(word.Key);
            }
            roulette = tmp.ToArray();
            rouletteLength = roulette.Length;
        }

        private void Train()
        {
            Console.WriteLine("> Training model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            var start = DateTime.Now;
            var last_word_count = 0;
            var word_count = 0;
            double length = new FileInfo(input_file).Length;
            using (var sr = new StreamReader(input_file))
            {
                var line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    var sentence = new List<string>();
                    foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!model.ContainsKey(word)) continue;
                        word_count++;
                        sentence.Add(word);
                    }

                    if (sentence.Count > 1) ProcessSentence(sentence);

                    if (word_count - last_word_count > 10000)
                    {
                        last_word_count = word_count;
                        var seconds = (DateTime.Now - start).TotalSeconds + 1;
                        var rate = word_count / seconds / 1000.0;
                        Console.Write("> Progress: {0:0.000%}  words/sec: {1:0.00}k  \r", sr.BaseStream.Position / length, rate);
                    }

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("\r\n");
                        return;
                    }
                }
            }
            Console.WriteLine("\r\n");
        }

        private void ProcessSentence(List<string> sentence)
        {
            var length = sentence.Count;
            for (var pos = 0; pos < length; pos++)
            {
                var word = sentence[pos];
                var hidden = new double[Model.Dims];
                var contextVectors = new List<double[]>();
                for (var w = 0; w < window * 2 + 1; w++)
                {
                    var p = pos - window + w;
                    if (p < 0 || p >= length) continue;
                    if (w != window)
                    {
                        var contextVector = model[sentence[p]].Location;
                        contextVectors.Add(contextVector);
                        for (var i = 0; i < Model.Dims; i++) hidden[i] += contextVector[i];
                    }
                }

                var count = contextVectors.Count;
                for (var i = 0; i < Model.Dims; i++) hidden[i] /= count;

                var error = new double[Model.Dims];
                for (var n = 0; n < negs + 1; n++)
                {
                    var target = word;
                    var label = 1;
                    if (n != 0)
                    {
                        while (target == word) target = roulette[rnd.Next(rouletteLength)];
                        label = 0;
                    }

                    double a = 0;
                    var targetVector = model[target].Context;
                    for (var i = 0; i < Model.Dims; i++) a += hidden[i] * targetVector[i];

                    double g = 0;
                    if (a > 5) g = (label - 1) * alpha;
                    else if (a < -5) g = (label - 0) * alpha;
                    else g = (label - (a + 5) / 10.0) * alpha;
                    if (g == 0) continue;

                    for (var i = 0; i < Model.Dims; i++)
                    {
                        error[i] += g * targetVector[i];
                        targetVector[i] += g * hidden[i];
                    }
                }

                foreach (var contextVector in contextVectors)
                    for (var i = 0; i < Model.Dims; i++)
                        contextVector[i] += error[i];
            }
        }
    }
}
