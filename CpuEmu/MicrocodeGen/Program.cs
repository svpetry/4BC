using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrocodeGen
{
    class Program
    {
        private const string FileName = "InstructionMatrix.csv";

        private static byte[] _rom1 = new byte[128];
        private static byte[] _rom2 = new byte[128];

        private const int Rom1Offset = 10;
        private const int Rom2Offset = 2;

        static void Main(string[] args)
        {
            if (!File.Exists(FileName))
            {
                Console.WriteLine($"{FileName} not found.");
                return;
            }

            var lines = File.ReadAllLines(FileName);

            // set default values
            var lineNo = 0;
            while (!lines[lineNo].StartsWith("Default"))
                lineNo++;
            var defaults = lines[lineNo].Split(';');
            byte defaultRom1 = 0;
            byte defaultRom2 = 0;
            for (var idx = 0; idx < 8; idx++)
            {
                if (defaults[idx + Rom1Offset] == "1")
                    defaultRom1 = (byte)(defaultRom1 | (1 << idx));
            }
            for (var idx = 0; idx < 8; idx++)
            {
                if (defaults[idx + Rom2Offset] == "1")
                    defaultRom2 = (byte)(defaultRom2 | (1 << idx));
            }
            for (var idx = 0; idx < _rom2.Length; idx++)
            {
                _rom1[idx] = defaultRom1;
                _rom2[idx] = defaultRom2;
            }

            while (true)
            {
                while (lineNo < lines.Length && !lines[lineNo].StartsWith("0x"))
                    lineNo++;
                if (lineNo == lines.Length) break;

                var cmdCode = Convert.ToUInt32(lines[lineNo].Substring(2, 2), 16);
                for (var cmdLine = 0; cmdLine < 8; cmdLine++)
                {
                    var romPos = cmdLine + (cmdCode << 3);
                    var value1 = defaultRom1;
                    var value2 = defaultRom2;

                    var values = lines[lineNo].Split(';');
                    for (var idx = 0; idx < 8; idx++)
                    {
                        if (values[idx + Rom1Offset] == "1")
                            value1 = (byte)(value1 | (1 << idx));
                        else if (values[idx + Rom1Offset] == "0")
                            value1 = (byte)(value1 & ~(1 << idx));
                    }
                    for (var idx = 0; idx < 8; idx++)
                    {
                        if (values[idx + Rom2Offset] == "1")
                            value2 = (byte)(value2 | (1 << idx));
                        else if (values[idx + Rom2Offset] == "0")
                            value2 = (byte)(value2 & ~(1 << idx));
                    }
                    _rom1[romPos] = value1;
                    _rom2[romPos] = value2;

                    lineNo++;
                }
            }

            File.WriteAllBytes("rom1.bin", _rom1);
            File.WriteAllBytes("rom2.bin", _rom2);
        }
    }
}
