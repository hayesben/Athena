// Copyright (c) 2016 robosoup
// www.robosoup.com

using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Athena
{
    internal class Word2Vec
    {
        // Start hyperparameters.
        private const float BaseLearningRate = 0.05f;
        private const double SubSample = 1e-8;
        private const int Iterations = 5;
        private const int NegativeSamples = 5;
        private const int SentenceBatches = 10;
        private const int SentencePositions = 64;
        private const int Window = 5;
        // End hyperparameters.

        private readonly Model _model;
        private readonly Random _rnd = new Random();
        private int[,] _tokens = new int[SentenceBatches, 1 + SentencePositions];
        private float _learningRate = BaseLearningRate;
        private float[,] _gpuContext;
        private float[,] _gpuLocation;
        private int[] _gpuRoulette;
        private int _rouletteLength;
        private int _sentence;
        private GPGPU _gpu;

        public Word2Vec(bool learnVocab)
        {
            _model = new Model(learnVocab);
            _gpu = CudafyHost.GetDevice(CudafyModes.Target, Program.DeviceID);
            _gpu.LoadModule(CudafyTranslator.Cudafy(_gpu.GetArchitecture()));
            _gpu.FreeAll();
            CopyToGPU();

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                Train(iteration);
                CopyFromGPU();
                _model.Save();
            }

            _gpu.FreeAll();
        }

        private void CopyToGPU()
        {
            var arrayContext = new float[_model.Count, Model.Dims];
            var arrayLocation = new float[_model.Count, Model.Dims];

            int id = 0;
            foreach (var item in _model)
            {
                item.Value.ID = id;
                var location = item.Value.Location;
                var context = item.Value.Context;
                for (var i = 0; i < Model.Dims; i++)
                {
                    arrayContext[id, i] = context[i];
                    arrayLocation[id, i] = location[i];
                }
                id++;
            }

            var tmp = new List<int>();
            var div = Math.Pow(Model.MinCount, 0.6);
            foreach (var word in _model)
            {
                var count = (int)(Math.Pow(word.Value.Count, 0.6) / div);
                for (var i = 0; i < count; i++) tmp.Add(word.Value.ID);
            }
            var arrayRoulette = tmp.ToArray();
            _rouletteLength = arrayRoulette.Length;

            _gpuContext = _gpu.Allocate<float>(arrayContext);
            _gpuLocation = _gpu.Allocate<float>(arrayLocation);
            _gpuRoulette = _gpu.Allocate<int>(arrayRoulette);

            _gpu.CopyToDevice(arrayContext, _gpuContext);
            _gpu.CopyToDevice(arrayLocation, _gpuLocation);
            _gpu.CopyToDevice(arrayRoulette, _gpuRoulette);
        }

        private void CopyFromGPU()
        {
            var arrayContext = new float[_model.Count, Model.Dims];
            var arrayLocation = new float[_model.Count, Model.Dims];

            _gpu.CopyFromDevice(_gpuContext, arrayContext);
            _gpu.CopyFromDevice(_gpuLocation, arrayLocation);

            foreach (var item in _model)
            {
                var id = item.Value.ID;
                var location = item.Value.Location;
                var context = item.Value.Context;
                for (var i = 0; i < Model.Dims; i++)
                {
                    context[i] = arrayContext[id, i];
                    location[i] = arrayLocation[id, i];
                }
            }
        }

        private void Train(int iteration)
        {
            Console.WriteLine("Training model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            Console.WriteLine("Hit 'Esc' to quit training early...");
            Console.WriteLine();
            var start = DateTime.Now;
            var checkpoint = DateTime.Now;
            var wordCount = 0;
            double length = new FileInfo(Program.Path_Corpus_1).Length;
            using (var sr = new StreamReader(Program.Path_Corpus_1))
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

                    if (sentence.Count > 1) ProcessSentence(sentence, _learningRate);

                    if (checkpoint < DateTime.Now)
                    {
                        var progress = (float)(sr.BaseStream.Position / length);
                        var seconds = (DateTime.Now - start).TotalSeconds + 1;
                        var rate = wordCount / seconds / 1000.0;
                        _learningRate = BaseLearningRate - (iteration * BaseLearningRate / Iterations) - (progress * BaseLearningRate / Iterations);
                        Console.Write("Progress: {0:0.000%}  words/sec: {1:0.000}k  learning rate: {2:0.000}  \r", progress, rate, _learningRate);
                        checkpoint = DateTime.Now.AddSeconds(1);
                    }

                    if (Console.KeyAvailable && (Console.ReadKey(true).Key == ConsoleKey.Escape)) break;
                }
            }
            Console.WriteLine("\r\n");
        }

        private void ProcessSentence(IReadOnlyList<string> sentence, float learningRate)
        {
            var position = 1;
            foreach (var word in sentence)
            {
                var tmp = _model[word];
                if (_rnd.NextDouble() > 1 - Math.Sqrt(SubSample * tmp.Count)) continue;
                _tokens[_sentence, position] = tmp.ID;
                position++;
                if (position == SentencePositions) break;
            }
            _tokens[_sentence, 0] = position - 1;

            _sentence++;
            if (_sentence == SentenceBatches)
            {
                _sentence = 0;
                _gpu.CopyToConstantMemory(_tokens, Tokens);
                _gpu.Launch(SentenceBatches * SentencePositions, Model.Dims).Execute(_gpuContext, _gpuLocation, _gpuRoulette, _rouletteLength, _rnd.Next(99999), learningRate);
                _tokens = new int[SentenceBatches, 1 + SentencePositions];
            }
        }

        [Cudafy]
        public static int[,] Tokens = new int[SentenceBatches, 1 + SentencePositions];

        [Cudafy]
        public static void Execute(GThread thread, float[,] context, float[,] location, int[] roulette, int rouletteLength, int seed, float learningRate)
        {
            float[] activation = thread.AllocateShared<float>("activation", Model.Dims);
            float[] error = thread.AllocateShared<float>("error", Model.Dims);
            float[] hidden = thread.AllocateShared<float>("hidden", Model.Dims);
            float[] g = thread.AllocateShared<float>("g", 1);
            int[] crop = thread.AllocateShared<int>("crop", 1);
            int[] negID = thread.AllocateShared<int>("negID", 1);

            int sentence = thread.blockIdx.x / SentencePositions;
            int position = thread.blockIdx.x - sentence * SentencePositions;
            int dim = thread.threadIdx.x;

            int len = Tokens[sentence, 0];
            if (position <= len)
            {
                uint random = (uint)(seed + position);
                if (dim == 0)
                {
                    random = random * 1664525u + 1013904223u;
                    crop[0] = (int)(random % Window);
                }
                thread.SyncThreads();

                int c = 0;
                hidden[dim] = 0;
                for (var w = crop[0]; w < Window * 2 + 1 - crop[0]; w++)
                {
                    var p = position - Window + w;
                    if ((p < 0) || (p >= len)) continue;

                    if (w == Window) continue;

                    hidden[dim] += location[Tokens[sentence, p + 1], dim];
                    c++;
                }
                hidden[dim] /= c;

                error[dim] = 0;

                for (int n = 0; n <= NegativeSamples; n++)
                {
                    if (dim == 0)
                    {
                        random = random * 1664525u + 1013904223u;
                        negID[0] = roulette[random % rouletteLength];
                    }
                    thread.SyncThreads();

                    int targetID = negID[0];
                    if (n == 0) targetID = Tokens[sentence, position + 1];

                    activation[dim] = hidden[dim] * context[targetID, dim];
                    thread.SyncThreads();

                    int j = Model.Dims / 2;
                    while (j != 0)
                    {
                        if (dim < j) activation[dim] += activation[dim + j];
                        thread.SyncThreads();
                        j /= 2;
                    }

                    if ((n != 0 || activation[0] <= 5f) && (n == 0 || activation[0] >= -5f))
                    {
                        if (dim == 0)
                        {
                            int label = 0;
                            if (n == 0) label = 1;
                            g[0] = (label - 1 / (1 + GMath.Exp(-activation[0]))) * learningRate;
                        }
                        thread.SyncThreads();

                        error[dim] += g[0] * context[targetID, dim];
                        context[targetID, dim] += g[0] * hidden[dim];
                    }
                    thread.SyncThreads();
                }

                for (var w = crop[0]; w < Window * 2 + 1 - crop[0]; w++)
                {
                    var p = position - Window + w;
                    if ((p < 0) || (p >= len)) continue;

                    if (w == Window) continue;

                    location[Tokens[sentence, p + 1], dim] += error[dim];
                }
            }
        }
    }
}
