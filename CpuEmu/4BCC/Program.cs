using System;
using System.IO;

namespace _4BCC
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("usage: 4BCC [filename]");
                return;
            }

            var fileName = args[0];
            if (!File.Exists(fileName))
            {
                Console.WriteLine("file not found!");
                return;
            }

            var compiler = new Compiler();
            compiler.Compile(fileName);
            var outputFile = Path.ChangeExtension(fileName, "asm");
            compiler.SaveAsm(outputFile);
            Console.WriteLine($"Success! File {outputFile} written.");
        }
    }
}
