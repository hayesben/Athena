// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Athena
{
    public class Model : Dictionary<string, Model.Item>
    {
        public const int Dims = 64;
        public const int MaxSize = (int)1e6;
        public const int MinCount = 16;

        private const string input_file = "corpus_1.txt";
        private const string model_file = "model.bin";
        private Dictionary<string, string> tokens = new Dictionary<string, string>();

        public class Item
        {
            private static Random rnd = new Random();
            public double[] Context = new double[Model.Dims];
            public double[] Location = new double[Model.Dims];
            public int Count;

            public void Load(BinaryReader br)
            {
                Count = br.ReadInt32();
                for (var i = 0; i < Model.Dims; i++) Location[i] = br.ReadDouble();
                for (var i = 0; i < Model.Dims; i++) Context[i] = br.ReadDouble();
            }

            public void Save(BinaryWriter bw)
            {
                bw.Write(Count);
                for (var i = 0; i < Model.Dims; i++) bw.Write(Location[i]);
                for (var i = 0; i < Model.Dims; i++) bw.Write(Context[i]);
            }

            public void Seed()
            {
                for (var i = 0; i < Model.Dims; i++)
                {
                    Context[i] = 0.5 - rnd.NextDouble();
                    Location[i] = 0.5 - rnd.NextDouble();
                }
            }
        }

        public Model()
        {
            if (File.Exists(model_file)) Load();
            else Learn();

            var keys = from k in Keys where k.Contains('_') select k;
            foreach (var key in keys)
                tokens.Add(key.Replace('_', ' '), key);
        }

        public Dictionary<string, double> Nearest(string phrase, int count)
        {
            var vec = Vector(phrase);
            var bestd = new double[count];
            var bestw = new string[count];
            for (var n = 0; n < count; n++) bestd[n] = -1;

            foreach (var key in Keys)
            {
                var tmp = this[key];
                var sim = Similarity(vec, tmp.Location);
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
            var result = new Dictionary<string, double>();
            for (var i = 0; i < count; i++) result.Add(bestw[i], bestd[i]);
            return result;
        }

        public void Save()
        {
            Console.WriteLine("> Saving model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            var back = string.Format("model_{0}.bak", DateTime.Now.ToString("yyyyMMddHHmm"));
            if (File.Exists(model_file)) File.Move(model_file, back);
            using (var bw = new BinaryWriter(File.Open(model_file, FileMode.Create)))
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
                    foreach (var word in line.Split(null as string[], StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!ContainsKey(word)) Add(word, new Item() { Count = 1 });
                        else this[word].Count++;
                    }
                    if (Count > MaxSize) Reduce(MinCount);
                    Console.Write("> Progress: {0:0.000%}  \r", sr.BaseStream.Position / length);
                }
            }
            Reduce(MinCount);
            foreach (var item in this) item.Value.Seed();
            Console.WriteLine("\r\n");
            Console.WriteLine("> Vocab size: {0}k", Count / 1000);
            Console.WriteLine();
        }

        private void Load()
        {
            Console.WriteLine("> Loading model [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            using (var br = new BinaryReader(File.Open(model_file, FileMode.Open)))
            {
                var words = br.ReadInt32();
                var dims = br.ReadInt32();
                if (dims != Dims)
                {
                    Console.WriteLine("> Dimensions don't match!");
                    Console.WriteLine();
                    return;
                }
                for (var w = 0; w < words; w++)
                {
                    var key = br.ReadString();
                    if (!ContainsKey(key)) Add(key, new Item());
                    this[key].Load(br);
                }
            }
        }

        private static double Similarity(double[] vec1, double[] vec2)
        {
            double sim = 0;
            double len1 = 0;
            double len2 = 0;
            var dims = vec1.Length;
            for (var i = 0; i < dims; i++)
            {
                sim += vec1[i] * vec2[i];
                len1 += vec1[i] * vec1[i];
                len2 += vec2[i] * vec2[i];
            }
            if (len1 == 0 || len2 == 0) return 0;
            else return sim / (Math.Sqrt(len1) * Math.Sqrt(len2));
        }

        public string[] Tokenise(string phrase)
        {
            foreach (var token in tokens)
                phrase = phrase.Replace(token.Key, token.Value);
            return phrase.Split(null as string[], StringSplitOptions.RemoveEmptyEntries);
        }

        private double[] Vector(string phrase)
        {
            var count = 0;
            var vec = new double[Dims];
            var keys = Tokenise(phrase);
            for (var w = 0; w < keys.Length; w++)
            {
                var sgn = 1;
                var key = keys[w];
                if (key.EndsWith(":"))
                {
                    sgn = -1;
                    key = key.TrimEnd(':');
                }
                if (ContainsKey(key))
                {
                    count++;
                    var tmp = this[key].Location;
                    for (var i = 0; i < Dims; i++)
                        vec[i] += tmp[i] * sgn;
                }
            }
            if (count > 1)
                for (var i = 0; i < Dims; i++) vec[i] /= count;

            return vec;
        }

        private void Reduce(int threshold)
        {
            var keys = Keys.ToList();
            foreach (var key in keys)
            {
                var item = this[key];
                if (item.Count < threshold) base.Remove(key);
            }
        }
    }
}
