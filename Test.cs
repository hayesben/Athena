// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Athena
{
    // The test file is formatted in the form of analogies - for example:
    // athens greece baghdad iraq
    // athens greece bangkok thailand
    // athens greece beijing china
    // athens greece berlin germany
    // ...
    // A great source can be found here:
    // http://download.tensorflow.org/data/questions-words.txt

    internal class Test
    {
        private Model model;
        private int threadCount = 0;
        private int correct;
        private int count;

        public Test(Model model)
        {
            this.model = model;
            this.model.Reduce(Model.QueryMin);

            Console.WriteLine("Starting test [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();

            string line;
            var items = new List<string>();
            using (StreamReader r = new StreamReader(Program.Path_Test))
                while ((line = r.ReadLine()) != null)
                    items.Add(line);
            var total = items.Count;

            var start = DateTime.Now;
            foreach (var item in items)
            {
                Task.Factory.StartNew(() => ProcessItem(item));
                Interlocked.Increment(ref threadCount);

                var seconds = (DateTime.Now - start).TotalSeconds + 1;
                Console.Write("Progress: {0:0.000%}  items/sec: {1:0.00}  \r", (double)count / total, count / seconds);

                while (threadCount > 99) Thread.Sleep(250);
            }

            while (threadCount > 0) Thread.Sleep(250);

            Console.WriteLine("\r\n");
            Console.WriteLine("Accuracy = {0:0.000%}", (double)correct / count);
            Console.WriteLine();
        }

        private void ProcessItem(string item)
        {
            var keys = item.Split(' ');
            var phrase = string.Format("{0}: {1} {2}", keys[0], keys[1], keys[2]);
            if (model.Nearest(phrase) == keys[3]) Interlocked.Increment(ref correct);
            Interlocked.Increment(ref count);
            Interlocked.Decrement(ref threadCount);
        }
    }
}