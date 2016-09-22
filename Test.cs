// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public Test(Model model)
        {
            Console.WriteLine("Performing test [{0:H:mm:ss}]", DateTime.Now);
            Console.WriteLine();
            Console.WriteLine("Hit 'Esc' to quit test early...");
            Console.WriteLine();

            double total = 0;
            using (StreamReader r = new StreamReader(TestFile))
                while (r.ReadLine() != null) { total++; }

            string line;
            double count = 0;
            var correct = 0;
            using (var sr = new StreamReader(TestFile))
                while ((line = sr.ReadLine()) != null)
                {
                    count++;
                    var keys = line.Split(',');
                    var phrase = string.Format("{0}: {1} {2}", keys[0], keys[1], keys[2]);
                    if (model.NearestWord(phrase) == keys[3]) correct++;
                    Console.Write("Progress: {0:0.000%}  \r", count / total);
                    if (Console.KeyAvailable && (Console.ReadKey(true).Key == ConsoleKey.Escape)) break;
                }

            Console.WriteLine("\r\n");
            Console.WriteLine("Accuracy = {0:0.000%}", correct / count);
            Console.WriteLine();
        }
    }
}