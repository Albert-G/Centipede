using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Centipede
{
    class Program
    {
        private static readonly string _prompt = "Centipede>";
        private static Centipede _centipedeInstance = null;

        static void Main(string[] args)
        {
            var path = Path.GetFullPath(args.Length > 0 ? args[0].Replace("\"", "") : "./ ");

            Console.WriteLine($"Processing project files under {path} ... ");
            _centipedeInstance = new Centipede(new ConsoleLogger(), path);

            Console.WriteLine("Done");
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler();

            while (true)
            {
                var input = ReadLine.Read(_prompt);
                if (input.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }

                var hostSolutions = _centipedeInstance.GetHostSolutions(input);
                if (hostSolutions.Count() <= 0)
                {
                    Console.WriteLine($"No result for {input}.");
                    continue;
                }
                else
                {
                    Console.WriteLine("This file is in the following solutions: ");
                    foreach (var solution in hostSolutions)
                    {
                        Console.WriteLine(solution.Path);
                    }
                }
            }
        }

        class AutoCompletionHandler : IAutoCompleteHandler
        {
            public char[] Separators { get; set; } = new char[] { ' ', '/' };

            public string[] GetSuggestions(string text, int index)
            {
                return _centipedeInstance.GetProjectFileNames(text).ToArray();
            }
        }
    }
}
