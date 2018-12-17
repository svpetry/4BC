using System;
using System.IO;

namespace _4BCasm
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("no file given.");
                return;
            }

            var fileName = args[0];
            if (Path.GetExtension(fileName) == "")
                fileName += ".asm";

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("file not found.");
                return;
            }

            var lines = File.ReadAllLines(fileName);

            var asm = new Assembler(lines);
            var outputFileName = Path.ChangeExtension(fileName, "bin");
            asm.AssembleTo(outputFileName, out var bytes);

            Console.WriteLine($"Read {lines.Length} lines. {bytes} bytes written to output file {outputFileName}.");
        }
    }
}
