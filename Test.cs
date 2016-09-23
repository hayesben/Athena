// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Athena
{
    // The test file is formatted in the form of comma seperated analogies - for example:
    // athens,greece,baghdad,iraq
    // athens,greece,bangkok,thailand
    // athens,greece,beijing,china
    // athens,greece,berlin,germany
    // ...
    // A great source can be found here:
    // http://download.tensorflow.org/data/questions-words.txt

    internal class Test
    {
        private const string TestFile = "test.csv";
        private Model model;
        private int threadCount = 0;
        private int correct;
        private int count;

        public Test(Model model)
        {
            this.model = model;
            Console.WriteLine("Starting test [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            Console.WriteLine("Hit 'Esc' to quit test early...");
            Console.WriteLine();

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            string line;
            var items = new List<string>();
            using (StreamReader r = new StreamReader(TestFile))
                while ((line = r.ReadLine()) != null)
                    items.Add(line);
            var total = items.Count;

            var start = DateTime.Now;
            foreach (var item in items)
            {
                Task.Factory.StartNew(() => ProcessItem(item, token));
                Interlocked.Increment(ref threadCount);

                var seconds = (DateTime.Now - start).TotalSeconds + 1;
                Console.Write("Progress: {0:0.000%}  items/sec: {1:0.00}  \r", (double)count / total, count / seconds);

                if (Console.KeyAvailable && (Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    tokenSource.Cancel();
                    break;
                }

                while (threadCount > 99) Thread.Sleep(250);
            }

            while (threadCount > 0) Thread.Sleep(250);

            Console.WriteLine("\r\n");
            Console.WriteLine("Accuracy = {0:0.000%}", (double)correct / count);
            Console.WriteLine();
        }

        private void ProcessItem(string item, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                Interlocked.Decrement(ref threadCount);
                return;
            }
            var keys = item.Split(',');
            var phrase = string.Format("{0}: {1} {2}", keys[0], keys[1], keys[2]);
            if (model.NearestWord(phrase) == keys[3]) Interlocked.Increment(ref correct);
            Interlocked.Increment(ref count);
            Interlocked.Decrement(ref threadCount);
        }
    }
}