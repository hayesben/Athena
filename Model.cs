// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Athena
{
    internal class Model : Dictionary<string, Model.Item>
    {
        // Start hyperparameters.
        public const int Dims = 128;
        public const int MaxSize = (int)1e6;
        public const int MinCount = 25;
        // End hyperparameters.

        private readonly Dictionary<string, string> _bigrams = new Dictionary<string, string>();

        public Model(bool learnVocab)
        {
            if (learnVocab) LearnVocab();
            LoadModel(learnVocab);
            Reduce();

            var keys = from k in Keys where k.Contains('_') select k;
            foreach (var key in keys)
                _bigrams.Add(key.Replace('_', ' '), key);
        }

        public void FindText(string phrase)
        {
            string line;
            using (var sr = new StreamReader(Program.Path_Corpus_1))
                while ((line = sr.ReadLine()) != null)
                    if (line.Contains(phrase)) Console.WriteLine(line);
            Console.WriteLine();
        }

        public KeyValuePair<string, double>[] Nearest(string phrase, int count, bool context)
        {
            double sim;
            var vec = Vector(phrase);
            var bestd = new double[count];
            var bestw = new string[count];
            for (var n = 0; n < count; n++) bestd[n] = -1;

            foreach (var key in Keys)
            {
                var tmp = this[key];
                if (!context) sim = Similarity(vec, tmp.Location);
                else sim = Similarity(vec, tmp.Context);
                for (var c = 0; c < count; c++)
                    if (sim > bestd[c])
                    {
                        for (var i = count - 1; i > c; i--)
                        {
                            bestd[i] = bestd[i - 1];
                            bestw[i] = bestw[i - 1];
                        }

                        bestd[c] = sim;
                        bestw[c] = key;
                        break;
                    }
            }

            var result = new KeyValuePair<string, double>[count];
            for (var i = 0; i < count; i++) result[i] = new KeyValuePair<string, double>(bestw[i], bestd[i]);

            return result;
        }

        public string NearestWord(string phrase)
        {
            var results = Nearest(phrase, 1, false);
            return results.First().Key;
        }

        public void Save()
        {
            Console.WriteLine("Saving model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            var back = string.Format("{0}_{1:yyyyMMddHHmm}.bin", Program.Path_Model.Remove(Program.Path_Model.Length - 4), DateTime.Now);
            if (File.Exists(Program.Path_Model)) File.Move(Program.Path_Model, back);
            using (var bw = new BinaryWriter(File.Open(Program.Path_Model, FileMode.Create)))
            {
                bw.Write(Count);
                bw.Write(Dims);
                foreach (var item in this)
                {
                    bw.Write(item.Key);
                    item.Value.Save(bw);
                }
            }
        }

        private void LearnVocab()
        {
            Console.WriteLine("Learning vocabulary [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            double length = new FileInfo(Program.Path_Corpus_1).Length;
            using (var sr = new StreamReader(Program.Path_Corpus_1))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                        if (!ContainsKey(word)) Add(word, new Item { Count = 1 });
                        else this[word].Count++;

                    if (Count > MaxSize) Reduce();
                    Console.Write("Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                }
            }

            Reduce();
            foreach (var item in this) item.Value.Seed();

            Console.WriteLine("\r\n");
            Console.WriteLine("Vocab size: {0}k", Count / 1000);
            Console.WriteLine();
        }

        private void LoadModel(bool learnVocab)
        {
            if (!File.Exists(Program.Path_Model)) return;
            Console.WriteLine("Loading model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            using (var br = new BinaryReader(File.Open(Program.Path_Model, FileMode.Open)))
            {
                var words = br.ReadInt32();
                var dims = br.ReadInt32();
                if (dims != Dims)
                {
                    Console.WriteLine("Dimensions don't match!");
                    Console.WriteLine();
                    return;
                }

                for (var w = 0; w < words; w++)
                {
                    var key = br.ReadString();
                    if (!ContainsKey(key)) Add(key, new Item());
                    this[key].Load(br, learnVocab);
                }
            }
        }

        private static float Similarity(float[] vec1, float[] vec2)
        {
            float sim = 0;
            float len1 = 0;
            float len2 = 0;
            var dims = vec1.Length;
            for (var i = 0; i < dims; i++)
            {
                sim += vec1[i] * vec2[i];
                len1 += vec1[i] * vec1[i];
                len2 += vec2[i] * vec2[i];
            }

            if ((len1 == 0) || (len2 == 0)) return 0;

            return sim / (float)(Math.Sqrt(len1) * Math.Sqrt(len2));
        }

        public string[] Tokenise(string phrase)
        {
            phrase = _bigrams.Aggregate(phrase, (current, token) => current.Replace(token.Key, token.Value));
            return phrase.Split(null as string[], StringSplitOptions.RemoveEmptyEntries);
        }

        private float[] Vector(string phrase)
        {
            var vec = new float[Dims];
            var keys = Tokenise(phrase);
            foreach (var k in keys)
            {
                var sgn = 1;
                var key = k;
                if (key.EndsWith(":"))
                {
                    sgn = -1;
                    key = key.TrimEnd(':');
                }

                if (!ContainsKey(key)) continue;

                var tmp = this[key].Normal;
                for (var i = 0; i < Dims; i++)
                    vec[i] += tmp[i] * sgn;
            }

            return vec;
        }

        private void Reduce()
        {
            var keys = Keys.ToList();
            foreach (var key in from key in keys let item = this[key] where item.Count < MinCount select key)
                Remove(key);
        }

        public class Item
        {
            private static readonly Random Rnd = new Random();
            public float[] Context = new float[Dims];
            public float[] Location = new float[Dims];
            public int Count;
            public int ID;

            public float[] Normal
            {
                get
                {
                    float len = 0;
                    for (var i = 0; i < Dims; i++) len += Location[i] * Location[i];
                    len = (float)Math.Sqrt(len);
                    var normal = new float[Dims];
                    for (var i = 0; i < Dims; i++) normal[i] = Location[i] / len;
                    return normal;
                }
            }

            public void Load(BinaryReader br, bool learnVocab)
            {
                if (learnVocab) br.ReadInt32();
                else Count = br.ReadInt32();
                for (var i = 0; i < Dims; i++) Location[i] =br.ReadSingle();
                for (var i = 0; i < Dims; i++) Context[i] = br.ReadSingle();
            }

            public void Save(BinaryWriter bw)
            {
                bw.Write(Count);
                for (var i = 0; i < Dims; i++) bw.Write(Location[i]);
                for (var i = 0; i < Dims; i++) bw.Write(Context[i]);
            }

            public void Seed()
            {
                for (var i = 0; i < Dims; i++)
                {
                    Context[i] = (float)(0.5 - Rnd.NextDouble());
                    Location[i] = (float)(0.5 - Rnd.NextDouble());
                }
            }
        }
    }
}