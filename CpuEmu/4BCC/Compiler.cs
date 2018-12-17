using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _4BCC
{
    class Compiler
    {
        private List<string> _asmFile = new List<string>();

        private Tokenizer _tokenizer = new Tokenizer();

        private Parser _parser = new Parser();

        public Compiler()
        {
        }

        public void Compile(string fileName)
        {
            var tokens = _tokenizer.Tokenize(fileName).Where(_ => !_.CanIgnore).ToList();

            try
            {
                var mnemonics = new List<Mnemonic>();
                _parser.ParseProgram(tokens, mnemonics);
                Optimizer.Optimize(mnemonics);
                _asmFile.AddRange(mnemonics.Select(_ => _.Line));
                _asmFile.Add("\r\n\tHLT");
            }
            catch (SyntaxErrorException ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        public void SaveAsm(string fileName)
        {
            File.WriteAllLines(fileName, _asmFile);
        }
       
    }
}
