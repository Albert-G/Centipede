using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Centipede
{
    class Program
    {
        private static readonly string _prompt = "Centipede>";
        private static Centipede _centipedeInstance = null;

        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                PrintUsage();
                return;
            }

            var path = Path.GetFullPath(args.Length > 0 ? args[0].Replace("\"", "") : "./ ");

            Console.WriteLine($"Processing files under {path} ... ");
            _centipedeInstance = new Centipede(new ConsoleLogger(), path);

            Console.WriteLine("Done");

            if (args.Length == 1)
            {
                StartInteractiveMode();
            }
            else
            {
                StartBatchMode(args.Skip(1));
            }
        }

        private static void StartBatchMode(IEnumerable<string> fileNames)
        {
            var hostSolutions = new List<VsProjectFile>();
            foreach (var fileName in fileNames)
            {
                hostSolutions.AddRange(_centipedeInstance.GetHostSolutions(fileName));
            }
            hostSolutions = hostSolutions.Distinct().ToList();

            if (hostSolutions.Count() <= 0)
            {
                Console.WriteLine($"No result.");
            }
            else
            {
                Console.WriteLine("Solutions: ");
                foreach (var solution in hostSolutions)
                {
                    Console.WriteLine(solution.Path);
                }
            }
        }

        private static void StartInteractiveMode()
        {
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

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("centipede <folder path> [file1 full path] [file2 full path] ...");
            Console.WriteLine();
            Console.WriteLine("If no file given, Centipede will enable interactive mode, enter part of the file and use tab key to autocomplte the full path and press enter to get the result.");
            Console.WriteLine("Type 'exit' to quit Centipede.");
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
