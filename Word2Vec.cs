// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Athena
{
    internal class Word2Vec
    {
        private readonly Model _model;
        private readonly Random _rnd = new Random();
        private const string InputFile = "corpus_1.txt";
        private const double Alpha = 0.05;
        private const int Window = 5;
        private const int Negs = 5;
        private string[] _roulette;
        private int _rouletteLength;

        public Word2Vec(bool learnVocab)
        {
            _model = new Model(learnVocab);
            BuildRoulette();
            Train();
            _model.Save();
        }

        private void BuildRoulette()
        {
            var tmp = new List<string>();
            var div = Math.Pow(Model.MinCount, 0.6);
            foreach (var word in _model)
            {
                var count = (int)(Math.Pow(word.Value.Count, 0.6) / div);
                for (var i = 0; i < count; i++) tmp.Add(word.Key);
            }

            _roulette = tmp.ToArray();
            _rouletteLength = _roulette.Length;
        }

        private void Train()
        {
            Console.WriteLine("> Training model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            var start = DateTime.Now;
            var lastWordCount = 0;
            var wordCount = 0;
            double length = new FileInfo(InputFile).Length;
            using (var sr = new StreamReader(InputFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var sentence = new List<string>();
                    foreach (
                        var word in
                        line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries)
                            .Where(word => _model.ContainsKey(word)))
                    {
                        wordCount++;
                        sentence.Add(word);
                    }

                    if (sentence.Count > 1) ProcessSentence(sentence);

                    if (wordCount - lastWordCount > 10000)
                    {
                        lastWordCount = wordCount;
                        var seconds = (DateTime.Now - start).TotalSeconds + 1;
                        var rate = wordCount / seconds / 1000.0;
                        Console.Write("> Progress: {0:0.000%}  words/sec: {1:0.00}k  \r", sr.BaseStream.Position / length, rate);
                    }

                    if (Console.KeyAvailable && (Console.ReadKey(true).Key == ConsoleKey.Escape))
                    {
                        Console.WriteLine("\r\n");
                        return;
                    }
                }
            }

            Console.WriteLine("\r\n");
        }

        private void ProcessSentence(IReadOnlyList<string> sentence)
        {
            var length = sentence.Count;
            for (var pos = 0; pos < length; pos++)
            {
                var word = sentence[pos];
                var hidden = new double[Model.Dims];
                var contextVectors = new List<double[]>();
                for (var w = 0; w < Window * 2 + 1; w++)
                {
                    var p = pos - Window + w;
                    if ((p < 0) || (p >= length)) continue;

                    if (w == Window) continue;

                    var contextVector = _model[sentence[p]].Location;
                    contextVectors.Add(contextVector);
                    for (var i = 0; i < Model.Dims; i++) hidden[i] += contextVector[i];
                }

                var count = contextVectors.Count;
                for (var i = 0; i < Model.Dims; i++) hidden[i] /= count;

                var error = new double[Model.Dims];
                for (var n = 0; n < Negs + 1; n++)
                {
                    var target = word;
                    var label = 1;
                    if (n != 0)
                    {
                        while (target == word) target = _roulette[_rnd.Next(_rouletteLength)];
                        label = 0;
                    }

                    double a = 0;
                    var targetVector = _model[target].Context;
                    for (var i = 0; i < Model.Dims; i++) a += hidden[i] * targetVector[i];
                    if ((n == 0 && a > 5) || (n != 0 && a < -5)) continue;

                    var g = (label - 1 / (1 + Math.Exp(-a))) * Alpha;
                    for (var i = 0; i < Model.Dims; i++)
                    {
                        error[i] += g * targetVector[i];
                        targetVector[i] += g * hidden[i];
                    }
                }

                foreach (var contextVector in contextVectors)
                    for (var i = 0; i < Model.Dims; i++) contextVector[i] += error[i];
            }
        }
    }
}