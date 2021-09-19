using System;
using System.IO;

namespace MindustryCompiler
{
    class Program
    {
        private static string inputPath = "E:\\Documents\\temp\\mindustry.test";

        private static void Main(string[] args)
        {
            ParseArguments(args);

            string compiled = Compile(inputPath);

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(inputPath), "outp.logic"), compiled);

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (i == 0)
                {
                    inputPath = args[0];
                }
            }

            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentNullException("Input path must be set!");
        }

        private static string Compile(string path)
        {
            string source = File.ReadAllText(path);

            Compiler comp = new Compiler();

            return comp.Compile(source);
        }
    }
}
