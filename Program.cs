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
                var phrase = Console.ReadLine();
                var neighbours = model.Nearest(phrase, 10, false);
                var context = model.Nearest(phrase, 10, true);
                Console.WriteLine();
                Console.Write("Neighbours");
                Console.CursorLeft = 40;
                Console.WriteLine("Context");
                Console.Write("-------------------");
                Console.CursorLeft = 40;
                Console.WriteLine("-------------------");
                for (var i = 0; i < neighbours.Length; i++)
                {
                    Console.Write("{0:0.00}  {1}", neighbours[i].Value, neighbours[i].Key);
                    Console.CursorLeft = 40;
                    Console.WriteLine("{0:0.00}  {1}", context[i].Value, context[i].Key);
                }
                Console.WriteLine();
            }
        }

        public static void Main(string[] args)
        {
            new Program();
        }
    }
}