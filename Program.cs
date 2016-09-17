// Copyright (c) 2016 robosoup
// www.robosoup.com

using System;

namespace Athena
{
    internal class Program
    {
        public Program()
        {
            while (true)
            {
                Console.Write("> Clean [C], Train [T] or Load [L] ");
                var key = Console.ReadKey(true).Key;
                Console.WriteLine("\r\n");
                if (key == ConsoleKey.C)
                {
                    new Cleaner();
                    new Word2Phrase();
                    new Word2Vec(true);
                    break;
                }

                if (key == ConsoleKey.T)
                {
                    new Word2Vec(false);
                    break;
                }

                if (key == ConsoleKey.L) break;
            }

            var model = new Model(false);
            while (true)
            {
                Console.Write("? ");
                var results = model.Nearest(Console.ReadLine(), 10);
                Console.WriteLine("--------------");
                foreach (var item in results)
                    Console.WriteLine("{0:0.00}  {1}", item.Value, item.Key);

                Console.WriteLine();
            }
        }

        public static void Main(string[] args)
        {
            new Program();
        }
    }
}