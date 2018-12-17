using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4BCC
{
    abstract class CompilerException : Exception
    {
        protected CompilerException(string fileName, int lineNo, string message)
            : base(message)
        {
            LineNo = lineNo;
            FileName = fileName;
        }

        public int LineNo { get; }

        public string FileName { get; }
    }

    class SyntaxErrorException : CompilerException
    {
        public SyntaxErrorException(string fileName, int lineNo, string message = "")
            : base(fileName, lineNo, message)
        {
        }

        public SyntaxErrorException(Token token, string message = "")
            : base(token.FileName, token.LineNo, message)
        {
        }

        public override string ToString()
        {
            return $"Syntax error in file {FileName} line no {LineNo}: {Message}";
        }
    }

    class NoFreeMemoryException : CompilerException
    {
        public NoFreeMemoryException(string fileName, int lineNo, string message = "")
            : base(fileName, lineNo, message)
        {
        }

        public override string ToString()
        {
            return $"No free memory in file {FileName} line no {LineNo}: {Message}";
        }
    }
}
