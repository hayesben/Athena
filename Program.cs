// Copyright (c) 2016 robosoup
// www.robosoup.com

using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;
using System;

namespace Athena
{
    internal class Program
    {
        public const int DeviceID = 0;

        public const string Path_Bigrams = @".\Data\bigrams.txt";
        public const string Path_Corpus = @".\Data\corpus.txt";
        public const string Path_Corpus_0 = @".\Data\corpus_0.txt";
        public const string Path_Corpus_1 = @".\Data\corpus_1.txt";
        public const string Path_Model = @".\Data\model.bin";
        public const string Path_Test = @".\Data\test.csv";

        public static void Main(string[] args)
        {
            new Program();
        }

        public Program()
        {
            InitialiseGPU();

            while (true)
            {
                Console.Write("Load & Train [L], Retrain [R], Test [T] or Query [Q] ");
                var key = Console.ReadLine().ToUpper();
                Console.WriteLine("\r\n");
                if (key == "L")
                {
                    new Cleaner();
                    new Word2Phrase();
                    new Word2Vec(true);
                }

                if (key == "R") new Word2Vec(false);

                if (key == "T") new Test(new Model(false));

                if (key == "Q")
                {
                    var model = new Model(false);
                    model.Reduce(Model.QueryMin);
                    while (true)
                    {
                        Console.WriteLine("Type #exit to return to menu...");
                        Console.WriteLine();
                        Console.Write("? ");
                        var phrase = Console.ReadLine();
                        Console.WriteLine();
                        if (phrase == "#exit") break;
                        var neighbours = model.Nearest(phrase, 10);
                        Console.WriteLine("Nearest");
                        Console.WriteLine("-------------------");
                        for (var i = 0; i < neighbours.Length; i++)
                            Console.WriteLine("{0:0.00}  {1}", neighbours[i].Value, neighbours[i].Key);
                        Console.WriteLine();
                    }
                }
            }
        }

        private void InitialiseGPU()
        {
            try
            {
                CudafyModes.Target = eGPUType.Cuda;
                Console.WriteLine("Cuda devices");
                Console.WriteLine("------------");
                foreach (GPGPUProperties prop in CudafyHost.GetDeviceProperties(eGPUType.Cuda, false))
                    Console.WriteLine("Device {0} - {1} {2}",
                        prop.DeviceId,
                        prop.Name.Trim(),
                        prop.DeviceId == DeviceID ? "---Selected---" : "");
            }
            catch
            {
                Console.WriteLine("None found");
                CudafyModes.Target = eGPUType.OpenCL;
            }
            Console.WriteLine();

            Console.WriteLine("OpenCL devices");
            Console.WriteLine("--------------");
            foreach (GPGPUProperties prop in CudafyHost.GetDeviceProperties(eGPUType.OpenCL, false))
                Console.WriteLine("Device {0} - {1} {2}",
                    prop.DeviceId,
                    prop.Name.Trim(),
                    prop.DeviceId == DeviceID && CudafyModes.Target == eGPUType.OpenCL ? "---Selected---" : "");
            Console.WriteLine("\r\n");

            CudafyTranslator.Language = CudafyModes.Target == eGPUType.OpenCL ? eLanguage.OpenCL : eLanguage.Cuda;
        }
    }
}