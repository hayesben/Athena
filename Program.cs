using System;

namespace Athena
{
    class Program
    {
        public static void Main(string[] args)
        {
            new Program();
        }

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
                    new Word2Vec();
                    break;
                }
                else if (key == ConsoleKey.T)
                {
                    new Word2Vec();
                    break;
                }
                else if (key == ConsoleKey.L) break;
            }

            var model = new Model();
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
    }
}