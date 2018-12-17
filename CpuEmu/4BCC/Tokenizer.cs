using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace _4BCC
{
    enum TokenType
    {
        None,

        Begin,
        End,
        Asm,
        Var,
        Const,
        For,
        To,
        Downto,
        While,
        If,
        Then,
        Else,
        Repeat,
        Until,
        Odd,
        Procedure,
        Do,
        Call,
        Outp,
        Inp,
        Hi,
        Lo,
        Inc,
        Dec,
        Nop,
        
        OpenParenthesis,
        CloseParenthesis,
        OpenBracket,
        CloseBracket,
        OpenBrace,
        CloseBrace,
        DataType,
        Identifier,
        Number,
        String,
        AsmLine,

        Semicolon,
        Comma,
        Colon,

        Assignment,
        Lesser,
        Greater,
        LesserEqual,
        GreaterEqual,
        Equal,
        NotEqual,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        LogicalAnd,
        LogicalOr,
        LogicalXor,
        And,
        Or,
        Shr,
        Shl,

        Comment,
        LineBreak,
        Dot,
        WhiteSpace
    }

    class TokenMatch
    {
        public bool IsMatch { get; set; }
        public string Value { get; set; }
        public string RemainingText { get; set; }
        public TokenType TokenType { get; set; }
        public bool CanIgnore { get; set; }
    }

    class TokenDef
    {
        private Regex _regex;

        public TokenDef(TokenType type, string regexPattern, bool canIgnore = false)
        {
            Type = type;
            _regex = new Regex(regexPattern);
            CanIgnore = canIgnore;
        }

        public TokenType Type { get; }

        public bool CanIgnore { get; }

        public string RegExpr { get; }

        public TokenMatch Match(string inputString)
        {
            var match = _regex.Match(inputString);
            if (match.Success)
            {
                string remainingText = string.Empty;
                if (match.Length != inputString.Length)
                    remainingText = inputString.Substring(match.Length);

                return new TokenMatch
                {
                    IsMatch = true,
                    RemainingText = remainingText,
                    Value = match.Value,
                    TokenType = Type,
                    CanIgnore = CanIgnore
                };
            }
            return new TokenMatch { IsMatch = false };
        }
    }

    [DebuggerDisplay("{Type} {Value}")]
    class Token
    {
        public Token(TokenType type, string value, string fileName, int lineNo, bool canIgnore)
        {
            Type = type;
            Value = value;
            FileName = fileName;
            LineNo = lineNo;
            CanIgnore = canIgnore;
        }

        public TokenType Type { get; }

        public string Value { get; }

        public string FileName { get; }

        public int LineNo { get; }

        public bool CanIgnore { get; }
    }

    class Tokenizer
    {
        private List<TokenDef> _tokenDefs;

        private bool _inAsmBlock;

        public Tokenizer()
        {
            InitTokenDefs();
        }

        private void InitTokenDefs()
        {
            _tokenDefs = new List<TokenDef>() {
                new TokenDef(TokenType.Comment, @"^//([^\n])*", true),
                new TokenDef(TokenType.Begin, @"^begin"),
                new TokenDef(TokenType.End, @"^end"),
                new TokenDef(TokenType.Asm, @"^asm"),
                new TokenDef(TokenType.Var, @"^var"),
                new TokenDef(TokenType.Const, @"^const"),
                new TokenDef(TokenType.For, @"^for"),
                new TokenDef(TokenType.To, @"^to"),
                new TokenDef(TokenType.Downto, @"^downto"),
                new TokenDef(TokenType.While, @"^while"),
                new TokenDef(TokenType.If, @"^if"),
                new TokenDef(TokenType.Then, @"^then"),
                new TokenDef(TokenType.Else, @"^else"),
                new TokenDef(TokenType.Repeat, @"^repeat"),
                new TokenDef(TokenType.Until, @"^until"),
                new TokenDef(TokenType.Odd, @"^odd"),
                new TokenDef(TokenType.Procedure, @"^procedure"),
                new TokenDef(TokenType.Do, @"^do"),
                new TokenDef(TokenType.Call, @"^call"),
                new TokenDef(TokenType.Inp, @"^inp"),
                new TokenDef(TokenType.Outp, @"^outp"),
                new TokenDef(TokenType.Hi, @"^hi"),
                new TokenDef(TokenType.Lo, @"^lo"),
                new TokenDef(TokenType.Inc, @"^inc"),
                new TokenDef(TokenType.Dec, @"^dec"),
                new TokenDef(TokenType.And, @"^and"),
                new TokenDef(TokenType.Or, @"^or"),
                new TokenDef(TokenType.Nop, @"^nop"),

                new TokenDef(TokenType.OpenParenthesis, @"^\("),
                new TokenDef(TokenType.CloseParenthesis, @"^\)"),
                new TokenDef(TokenType.OpenBracket, @"^\["),
                new TokenDef(TokenType.CloseBracket, @"^\]"),
                new TokenDef(TokenType.OpenBrace, @"^\{"),
                new TokenDef(TokenType.CloseBrace, @"^\}"),
                
                new TokenDef(TokenType.DataType, @"^(nibble|byte|word)"),
                new TokenDef(TokenType.Number, @"^(\d+|\$(\d|[a-f]|[A-F])+|\%[0|1]+|'[^']')"),
                new TokenDef(TokenType.String, @"^'[^']*'"),
                new TokenDef(TokenType.Semicolon, @"^;"),
                new TokenDef(TokenType.Comma, @"^,"),
                new TokenDef(TokenType.Dot, @"^\."),

                new TokenDef(TokenType.Assignment, @"^:="),
                new TokenDef(TokenType.Colon, @"^:"),
                new TokenDef(TokenType.LesserEqual, @"^<="),
                new TokenDef(TokenType.GreaterEqual, @"^>="),
                new TokenDef(TokenType.Lesser, @"^<"),
                new TokenDef(TokenType.Greater, @"^>"),
                new TokenDef(TokenType.Equal, @"^="),
                new TokenDef(TokenType.NotEqual, @"^#"),
                new TokenDef(TokenType.Plus, @"^\+"),
                new TokenDef(TokenType.Minus, @"^-"),
                new TokenDef(TokenType.Multiply, @"^\*"),
                new TokenDef(TokenType.Divide, @"^/"),
                new TokenDef(TokenType.Modulo, @"^%"),
                new TokenDef(TokenType.LogicalAnd, @"^&"),
                new TokenDef(TokenType.LogicalOr, @"^\|"),
                new TokenDef(TokenType.LogicalXor, @"^\^"),
                new TokenDef(TokenType.Shr, @"^shr"),
                new TokenDef(TokenType.Shl, @"^shl"),

                new TokenDef(TokenType.Identifier, @"^(_|[a-z]|[A-Z])\w*"),

                new TokenDef(TokenType.LineBreak, @"^(\r\n|\n)", true),
                new TokenDef(TokenType.WhiteSpace, @"^[\t ]+", true)
            };
        }

        public List<Token> Tokenize(string fileName)
        {
            var lineNo = 1;
            var tokens = new List<Token>();

            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Include file {fileName} not found");

            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                if (line.StartsWith("#include"))
                {
                    var parts = line.Split(new[] { ' ' }, 2);
                    if (parts.Length == 2)
                        tokens.AddRange(Tokenize(parts[1]));

                    lineNo++;
                    continue;
                }

                if (TokenizeAsmLine(line, fileName, lineNo, tokens))
                {
                    lineNo++;
                    continue;
                }

                var remainingText = line;
                while (!string.IsNullOrWhiteSpace(remainingText))
                {
                    var match = FindMatch(remainingText);
                    if (match.IsMatch)
                    {
                        tokens.Add(new Token(match.TokenType, match.Value, fileName, lineNo, match.CanIgnore));
                        remainingText = match.RemainingText;
                    }
                    else
                        remainingText = "";
                }

                lineNo++;
            }

            if (_inAsmBlock)
                throw new SyntaxErrorException(fileName, lineNo, "asm block has no end");

            return tokens;
        }

        private bool TokenizeAsmLine(string line, string fileName, int lineNo, List<Token> tokens)
        {
            if (!_inAsmBlock)
            {
                if (line.Trim() == "asm")
                {
                    tokens.Add(new Token(TokenType.Asm, "asm", fileName, lineNo, false));
                    _inAsmBlock = true;
                    return true;
                }
                return false;
            }

            if (line.Trim() == "end;")
            {
                tokens.Add(new Token(TokenType.End, "end", fileName, lineNo, false));
                tokens.Add(new Token(TokenType.Semicolon, ";", fileName, lineNo, false));
                _inAsmBlock = false;
                return true;
            }

            var regex = new Regex("^[^\n]*");
            var regexMatch = regex.Match(line);
            if (!regexMatch.Success) throw new SyntaxErrorException(fileName, lineNo, "error in asm block");

            tokens.Add(new Token(TokenType.AsmLine, regexMatch.Value.Trim(), fileName, lineNo, false));
            return true;
        }

        private TokenMatch FindMatch(string inputText)
        {
            foreach (var tokenDefinition in _tokenDefs)
            {
                var match = tokenDefinition.Match(inputText);
                if (match.IsMatch)
                    return match;
            }

            return new TokenMatch() { IsMatch = false };
        }
    }
}
