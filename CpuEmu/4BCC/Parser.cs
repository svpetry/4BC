using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace _4BCC
{
    enum DataType
    {
        None,
        Nibble, 
        Byte,
        Word
    }

    class ProcCall
    {
        public string ReturnLabel { get; set; }

        public int CallLineId { get; set; }
    }

    [DebuggerDisplay("Procedure {Name}")]
    class ProcInfo
    {
        public ProcInfo(string name, string label)
        {
            Name = name;
            Label = label;
        }

        public string Name { get; }

        public string Label { get; }

        public List<ProcCall> Calls { get; } = new List<ProcCall>();

        public Variable ReturnIdVar { get; set; }

        public int ProcEndLineId { get; set; }

        public ProcCall AddProcCall(int lineId, string returnLabel)
        {
            var procCall = new ProcCall { CallLineId = lineId, ReturnLabel = returnLabel };
            Calls.Add(procCall);
            return procCall;
        }
    }

    interface ILineNoProvider
    {
        int CurrentLineNo { get; }

        string CurrentFileName { get; }
    }

    class Parser : ILineNoProvider
    {
        private List<Token> _tokens;

        private int _position;

        private CodeGen _codeGen;

        private readonly VarProvider _varProvider;

        private List<Mnemonic> _mnemonics;

        private readonly Dictionary<string, ProcInfo> _procInfos = new Dictionary<string, ProcInfo>();

        public Parser()
        {
            _varProvider = new VarProvider(this);
        }

        public void ParseProgram(List<Token> tokens, List<Mnemonic> mnemonics)
        {
            _mnemonics = mnemonics;
            _codeGen = new CodeGen(_mnemonics, _varProvider);
            _tokens = tokens;
            ParseProgram();

            GenerateProcedureLogic();

            _varProvider.ExportVariables("varlist.txt");
        }

        public int CurrentLineNo
        {
            get
            {
                if (_position >= _tokens.Count)
                    return _tokens.Last().LineNo;
                return _tokens[_position].LineNo;
            }
        }

        public string CurrentFileName
        {
            get
            {
                if (_position >= _tokens.Count)
                    return _tokens.Last().FileName;
                return _tokens[_position].FileName;
            }
        }

        private int ParseNumber(string numstr)
        {
            // hex number
            if (numstr.StartsWith("$"))
                return int.Parse(numstr.Substring(1), System.Globalization.NumberStyles.HexNumber);

            // binary number
            if (numstr.StartsWith("%"))
            {
                var number = 0;
                numstr = numstr.Substring(1);
                for (var idx = 0; idx < numstr.Length; idx++)
                {
                    number <<= 1;
                    if (numstr[idx] == '1')
                        number++;
                }
                return number;
            }

            // character
            if (numstr.StartsWith("'") && numstr.EndsWith("'") && numstr.Length == 3)
            {
                return (byte)numstr[1];
            }

            // decimal number
            return int.Parse(numstr);
        }

        private bool IsOperator(TokenType token)
        {
            return token == TokenType.Plus || token == TokenType.Minus
                || token == TokenType.Multiply || token == TokenType.Divide || token == TokenType.Modulo
                || token == TokenType.LogicalAnd || token == TokenType.LogicalOr || token == TokenType.LogicalXor;
        }

        private Token CurrentToken => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.None, "", _tokens[0].FileName, _tokens.Count, false);

        private void MakeSameType(ref Variable var1, ref Variable var2)
        {
            if (var1.Type == var2.Type) return;

            if (var1.Type < var2.Type)
            {
                var newVar1 = _varProvider.GetSystemVariable(var2.Type);
                _codeGen.ChangeType(var1, newVar1);
                FreeSystemVar(var1);
                var1 = newVar1;
            }
            else
            {
                var newVar2 = _varProvider.GetSystemVariable(var1.Type);
                _codeGen.ChangeType(var2, newVar2);
                FreeSystemVar(var2);
                var2 = newVar2;
            }
        }

        private Variable ParseFactor(Dictionary<string, BaseVar> variables)
        {
            Variable resultVar;

            if (Accept(TokenType.Identifier, out var varToken))
            {
                if (!variables.TryGetValue(varToken.Value, out var baseVar))
                    throw new SyntaxErrorException(varToken, $"unknown variable {varToken.Value}");
                if (baseVar is ArrayVar arrayVar)
                {
                    resultVar = _varProvider.GetSystemVariable(arrayVar.Type);
                    Expect(TokenType.OpenBracket);
                    var indexVar = ParseExpression(variables);
                    Expect(TokenType.CloseBracket);
                    _codeGen.GenArrayRead(arrayVar, indexVar, resultVar);
                    FreeSystemVar(indexVar);
                }
                else
                    resultVar = (Variable)baseVar;
            }
            else if (Accept(TokenType.Number, out var numberToken))
            {
                var intNumber = ParseNumber(numberToken.Value);
                if (intNumber > 0xFFFF || intNumber < -32768)
                    throw new SyntaxErrorException(numberToken, $"number does not fit in 16 bits: {numberToken.Value}");
                var number = (ushort)intNumber;
                DataType dataType = DataType.Nibble;
                if (number > 0x00FF)
                    dataType = DataType.Word;
                else if (number > 0x000F)
                    dataType = DataType.Byte;
                resultVar = _varProvider.GetSystemVariable(dataType);

                _codeGen.GenAssignment(number, resultVar);
            }
            else if (Accept(TokenType.OpenParenthesis))
            {
                resultVar = ParseExpression(variables);
                Expect(TokenType.CloseParenthesis);
            }
            else if (Accept(TokenType.Lo))
            {
                Expect(TokenType.OpenParenthesis);
                var varTokenLo = Expect(TokenType.Identifier);
                var varName = varTokenLo.Value;
                if (!variables.TryGetValue(varName, out var baseVar))
                    throw new SyntaxErrorException(varTokenLo, $"unknown variable {varName}");
                Expect(TokenType.CloseParenthesis);

                if (baseVar is Variable var1)
                    resultVar = _codeGen.GenLo(var1);
                else
                    throw new SyntaxErrorException(varTokenLo, $"lo not allowed for arrays");
            }
            else if (Accept(TokenType.Hi))
            {
                Expect(TokenType.OpenParenthesis);
                var varTokenHi = Expect(TokenType.Identifier);
                var varName = varTokenHi.Value;
                if (!variables.TryGetValue(varName, out var baseVar))
                    throw new SyntaxErrorException(varTokenHi, $"unknown variable {varName}");
                Expect(TokenType.CloseParenthesis);

                if (baseVar is Variable var1)
                    resultVar = _codeGen.GenHi(var1);
                else
                    throw new SyntaxErrorException(varTokenHi, $"hi not allowed for arrays");
            }
            else
                throw new SyntaxErrorException(CurrentFileName, CurrentLineNo);

            return resultVar;
        }

        private Variable ParseTerm(Dictionary<string, BaseVar> variables)
        {
            var resultVar = ParseFactor(variables);
            while (CurrentTokenType(new[] { TokenType.Multiply, TokenType.Modulo, TokenType.Divide,
                TokenType.LogicalAnd, TokenType.LogicalOr, TokenType.LogicalXor }))
            {
                var opType = CurrentToken.Type;
                _position++;
                var var2 = ParseFactor(variables);
                var var1 = resultVar;

                MakeSameType(ref var1, ref var2);

                resultVar = _varProvider.GetSystemVariable(var1.Type);

                switch (opType)
                {
                    case TokenType.Multiply:
                        _codeGen.GenMultiplication(var1, var2, resultVar);
                        break;
                    case TokenType.Divide:
                        _codeGen.GenDivision(var1, var2, resultVar);
                        break;
                    case TokenType.Modulo:
                        _codeGen.GenDivision(var1, var2, resultVar, true);
                        break;
                    case TokenType.LogicalAnd:
                        _codeGen.GenLogicalAnd(var1, var2, resultVar);
                        break;
                    case TokenType.LogicalOr:
                        _codeGen.GenLogicalOr(var1, var2, resultVar);
                        break;
                    case TokenType.LogicalXor:
                        _codeGen.GenLogicalXor(var1, var2, resultVar);
                        break;
                }

                FreeSystemVar(var1);
                FreeSystemVar(var2);
            }

            return resultVar;
        }

        private Variable ParseExpression(Dictionary<string, BaseVar> variables)
        {
            var neg = false;
            if (CurrentTokenType( new[] { TokenType.Plus, TokenType.Minus }))
            {
                if (Accept(TokenType.Minus))
                    neg = true;
                else
                    _position++;
            }

            var resultVar = ParseTerm(variables);
            while (CurrentTokenType(new[] { TokenType.Plus, TokenType.Minus }))
            {
                var opType = CurrentToken.Type;
                _position++;
                var var2 = ParseTerm(variables);
                var var1 = resultVar;

                MakeSameType(ref var1, ref var2);

                resultVar = _varProvider.GetSystemVariable(var1.Type);

                if (opType == TokenType.Plus)
                    _codeGen.GenAddition(var1, var2, resultVar);
                else
                    _codeGen.GenSubstraction(var1, var2, resultVar);

                FreeSystemVar(var1);
                FreeSystemVar(var2);
            }

            if (neg)
            {
                // TODO
            }

            return resultVar;
        }

        private CondBranching ParseConditionGroup(Dictionary<string, BaseVar> variables)
        {
            CondBranching lastBranching;
            var lblTrue = _codeGen.GenLabel();
            var lblFalse = _codeGen.GenLabel();

            var branchings = new List<CondBranching>();
            branchings.Add(lastBranching = ParseConjunction(variables));

            while (Accept(TokenType.Or))
            {
                var condLbl = _codeGen.GenLabel();
                _mnemonics.Add(new Mnemonic(condLbl + ":"));

                var branching = ParseConjunction(variables);
                lastBranching.SetTrueJumpLabel(lblTrue);
                lastBranching.SetFalseJumpLabel(condLbl);

                branchings.Add(lastBranching = branching);
            }

            if (branchings.Count == 1)
                return branchings[0];

            lastBranching.SetTrueJumpLabel(lblTrue);
            lastBranching.SetFalseJumpLabel(lblFalse);

            var resultBranching = new CondBranching();
            _mnemonics.Add(new Mnemonic(lblTrue + ":"));
            _mnemonics.Add(resultBranching.AddTrueJump("JMP"));
            _mnemonics.Add(new Mnemonic(lblFalse + ":"));
            _mnemonics.Add(resultBranching.AddFalseJump("JMP"));

            return resultBranching;
        }

        private CondBranching ParseConjunction(Dictionary<string, BaseVar> variables)
        {
            CondBranching lastBranching;
            var lblTrue = _codeGen.GenLabel();
            var lblFalse = _codeGen.GenLabel();

            var branchings = new List<CondBranching>();
            branchings.Add(lastBranching = ParseCondition(variables));

            while (Accept(TokenType.And))
            {
                var condLbl = _codeGen.GenLabel();
                _mnemonics.Add(new Mnemonic(condLbl + ":"));

                var branching = ParseConjunction(variables);
                lastBranching.SetTrueJumpLabel(condLbl);
                lastBranching.SetFalseJumpLabel(lblFalse);

                branchings.Add(lastBranching = branching);
            }

            if (branchings.Count == 1)
                return branchings[0];
            else
            {
                lastBranching.SetTrueJumpLabel(lblTrue);
                lastBranching.SetFalseJumpLabel(lblFalse);

                var resultBranching = new CondBranching();
                _mnemonics.Add(new Mnemonic(lblTrue + ":"));
                _mnemonics.Add(resultBranching.AddTrueJump("JMP"));
                _mnemonics.Add(new Mnemonic(lblFalse + ":"));
                _mnemonics.Add(resultBranching.AddFalseJump("JMP"));
                return resultBranching;
            }
        }

        private CondBranching ParseCondition(Dictionary<string, BaseVar> variables)
        {
            if (Accept(TokenType.OpenBrace))
            {
                var resultBranching = ParseConditionGroup(variables);
                Expect(TokenType.CloseBrace);
                return resultBranching;
            }
            else if (Accept(TokenType.Odd))
            {
                var resultVar = ParseExpression(variables);
                var branching = _codeGen.CheckOdd(resultVar);
                FreeSystemVar(resultVar);
                return branching;
            }
            else
            {
                var var1 = ParseExpression(variables);
                var cond = CurrentToken.Type;
                if (cond == TokenType.Equal || cond == TokenType.NotEqual 
                    || cond == TokenType.Greater || cond == TokenType.GreaterEqual
                    || cond == TokenType.Lesser || cond == TokenType.LesserEqual)
                {
                    _position++;

                    // make checks for "> 0" and "# 0" faster
                    if ((cond == TokenType.Greater || cond == TokenType.NotEqual) 
                        && Accept(TokenType.Number, out var numToken))
                    {
                        if (ParseNumber(numToken.Value) == 0 && !IsOperator(CurrentToken.Type))
                            return _codeGen.CheckNotZero(var1);
                        else
                            _position--;
                    }

                    var var2 = ParseExpression(variables);
                    MakeSameType(ref var1, ref var2);
                    var branching = _codeGen.EvalCondition(var1, var2, cond);

                    FreeSystemVar(var1);
                    FreeSystemVar(var2);

                    return branching;
                }
                else
                    throw new SyntaxErrorException(CurrentToken, "condition: invalid operator");
            }
        }

        private void ParseStatement(Dictionary<string, BaseVar> variables)
        {
            if (Accept(TokenType.Identifier, out var varToken))
            {
                if (!variables.TryGetValue(varToken.Value, out var destBaseVar))
                    throw new SyntaxErrorException(varToken.FileName, varToken.LineNo, $"unknown variable {varToken.Value}");

                if (destBaseVar is ArrayVar arrayVar)
                {
                    if (Accept(TokenType.OpenBracket))
                    {
                        // assign value to array element
                        var indexVar = ParseExpression(variables);
                        Expect(TokenType.CloseBracket);
                        Expect(TokenType.Assignment);
                        var valueVar = ParseExpression(variables);
                        _codeGen.GenArrayWrite(arrayVar, indexVar, valueVar);

                        FreeSystemVar(indexVar);
                        FreeSystemVar(valueVar);
                    }
                    else
                    {
                        // string/array assignment
                        Expect(TokenType.Assignment);

                        if (Accept(TokenType.OpenParenthesis))
                        {
                            // constant array assignment x := (1, 3, 4)
                            var strValue = "";
                            while (strValue == "" || Accept(TokenType.Comma))
                            {
                                var numToken = Expect(TokenType.Number);
                                strValue += (char)ParseNumber(numToken.Value);
                            }
                            Expect(TokenType.CloseParenthesis);

                            if (strValue.Length > arrayVar.Length)
                                throw new SyntaxErrorException(varToken, $"array {arrayVar.Name} too short for array assignment");
                            _codeGen.GenStringAssignment(strValue, arrayVar);
                        }
                        else if (Accept(TokenType.Identifier, out var srcVarToken))
                        {
                            // array assignment x := y
                            if (!variables.TryGetValue(srcVarToken.Value, out var srcBaseVar))
                                throw new SyntaxErrorException(srcVarToken, $"unknown variable {srcVarToken.Value}");
                            if (!(srcBaseVar is ArrayVar srcVar))
                                throw new SyntaxErrorException(srcVarToken, $"variable {srcBaseVar.Name} has to be of array type.");
                            if (arrayVar.Type != srcVar.Type)
                                throw new SyntaxErrorException(srcVarToken, $"types of {arrayVar.Name} and {srcVar.Name} different.");
                            _codeGen.GenArrayAssignment(srcVar, arrayVar);
                        }
                        else
                        {
                            // string assignemt x := 'asdf'
                            var strToken = Expect(new[] { TokenType.String, TokenType.Number });
                            if (!strToken.Value.StartsWith("'") || !strToken.Value.EndsWith("'"))
                                throw new SyntaxErrorException(strToken, $"invalid string {strToken.Value}");
                            if (strToken.Value.Length - 2 > arrayVar.Length)
                                throw new SyntaxErrorException(strToken, $"array {arrayVar.Name} too short for string {strToken.Value}");
                            _codeGen.GenStringAssignment(strToken.Value.Substring(1, strToken.Value.Length - 2), arrayVar);
                        }
                    }
                }
                else if (destBaseVar is Variable destVar)
                {
                    Expect(TokenType.Assignment);

                    var valueVar = ParseExpression(variables);

                    if (valueVar.Type != destVar.Type)
                        _codeGen.ChangeType(valueVar, destVar);
                    else
                        _codeGen.GenAssignment(valueVar, destVar);

                    FreeSystemVar(valueVar);
                }
                return;
            }

            if (Accept(TokenType.Call))
            {
                var procToken = Expect(TokenType.Identifier);
                var procName = procToken.Value;
                if (_procInfos.TryGetValue(procName, out var proc))
                {
                    var returnLbl = _codeGen.GenLabel();
                    var callCommand = new Mnemonic("JMP", proc.Label);
                    _mnemonics.Add(callCommand);
                    _mnemonics.Add(new Mnemonic(returnLbl + ":"));
                    proc.AddProcCall(callCommand.LineId, returnLbl);
                }
                else
                    throw new SyntaxErrorException(procToken, "unknown procedure " + procName);
                return;
            }

            if (Accept(TokenType.Begin))
            {
                while (CurrentToken.Type != TokenType.End)
                {
                    ParseStatement(variables);
                    Expect(TokenType.Semicolon);
                }
                Expect(TokenType.End);
                return;
            }

            if (Accept(TokenType.Asm))
            {
                while (CurrentToken.Type != TokenType.End)
                {
                    ParseAsmLine(variables);
                }
                Expect(TokenType.End);
                return;
            }

            if (Accept(TokenType.If))
            {
                var lblThen = _codeGen.GenLabel();
                var lblElse = _codeGen.GenLabel();
                var lblExit = _codeGen.GenLabel();

                var branch = ParseConditionGroup(variables);
                branch.SetTrueJumpLabel(lblThen);
                Expect(TokenType.Then);
                _mnemonics.Add(new Mnemonic(lblThen + ":"));
                ParseStatement(variables);
                if (Accept(TokenType.Else))
                {
                    branch.SetFalseJumpLabel(lblElse);
                    _mnemonics.Add(new Mnemonic("JMP", lblExit));
                    _mnemonics.Add(new Mnemonic(lblElse + ":"));
                    ParseStatement(variables);
                }
                else
                    branch.SetFalseJumpLabel(lblExit);
                _mnemonics.Add(new Mnemonic(lblExit + ":"));
                return;
            }

            if (Accept(TokenType.While))
            {
                var lblLoopStart = _codeGen.GenLabel();
                var lblLoopBody = _codeGen.GenLabel();
                var lblLoopExit = _codeGen.GenLabel();

                _mnemonics.Add(new Mnemonic(lblLoopStart + ":"));
                var branch = ParseConditionGroup(variables);
                branch.SetTrueJumpLabel(lblLoopBody);
                branch.SetFalseJumpLabel(lblLoopExit);

                Expect(TokenType.Do);

                _mnemonics.Add(new Mnemonic(lblLoopBody + ":"));
                ParseStatement(variables);
                _mnemonics.Add(new Mnemonic("JMP", lblLoopStart));
                _mnemonics.Add(new Mnemonic(lblLoopExit + ":"));
                return;
            }

            if (Accept(TokenType.Shr))
            {
                var vToken = Expect(TokenType.Identifier);
                if (!variables.TryGetValue(vToken.Value, out var baseVar))
                    throw new SyntaxErrorException(vToken, "unknown variable");
                if (baseVar is Variable resultVar)
                    _codeGen.GenShr(resultVar);
                else
                    throw new SyntaxErrorException(vToken, "shr not allowed for arrays");
                return;
            }

            if (Accept(TokenType.Shl))
            {
                var vToken = Expect(TokenType.Identifier);
                if (!variables.TryGetValue(vToken.Value, out var baseVar))
                    throw new SyntaxErrorException(vToken, "unknown variable");
                if (baseVar is Variable resultVar)
                    _codeGen.GenShl(resultVar);
                else
                    throw new SyntaxErrorException(vToken, "shl not allowed for arrays");
                return;
            }

            if (Accept(TokenType.Inc))
            {
                var vToken = Expect(TokenType.Identifier);
                if (!variables.TryGetValue(vToken.Value, out var baseVar))
                    throw new SyntaxErrorException(vToken, "unknown variable");
                if (baseVar is Variable resultVar)
                    _codeGen.GenIncrement(resultVar);
                else
                    throw new SyntaxErrorException(vToken, "inc not allowed for arrays");
                return;
            }

            if (Accept(TokenType.Dec))
            {
                var vToken = Expect(TokenType.Identifier);
                if (!variables.TryGetValue(vToken.Value, out var baseVar))
                    throw new SyntaxErrorException(vToken, "unknown variable");
                if (baseVar is Variable resultVar)
                    _codeGen.GenDecrement(resultVar);
                else
                    throw new SyntaxErrorException(vToken, "inc not allowed for arrays");
                return;
            }

            if (Accept(TokenType.Nop))
            {
                _codeGen.GenNop();
                return;
            }

            if (Accept(TokenType.Outp))
            {
                var numberToken = Expect(TokenType.Number);
                Expect(TokenType.Comma);
                var valueVar = ParseExpression(variables);
                _codeGen.GenOut(int.Parse(numberToken.Value) & 0x0F, valueVar);

                FreeSystemVar(valueVar);
                return;
            }

            if (Accept(TokenType.Inp))
            {
                var ident = Expect(TokenType.Identifier);
                if (!variables.TryGetValue(ident.Value, out var baseVar))
                    throw new SyntaxErrorException(ident, "unknown variable");

                var resultVar = baseVar as Variable;
                if (resultVar == null)
                    throw new SyntaxErrorException(ident, "arrays not allowed for inp");

                Expect(TokenType.Comma);
                var numberToken = Expect(TokenType.Number);
                _codeGen.GenIn(int.Parse(numberToken.Value) & 0x0F, resultVar);

                return;
            }

            throw new SyntaxErrorException(CurrentToken, $"unknown statement: {CurrentToken.Value}");
        }

        private void ParseAsmLine(Dictionary<string, BaseVar> variables)
        {
            var regex = new Regex(@"\([^\)]+\)");
            while (Accept(TokenType.AsmLine, out var asmLine))
            {
                var parts = asmLine.Value.Split(new[] { ' ' }, 2);
                if (parts.Length == 1)
                {
                    _mnemonics.Add(new Mnemonic(parts[0]));
                }
                else if (parts.Length == 2)
                {
                    var cmd = parts[0].Trim();
                    var param = parts[1];
                    if (cmd != "")
                    {
                        var match = regex.Match(param);
                        if (match.Success)
                        {
                            var varParts = match.Value.Substring(1, match.Value.Length - 2).Split('+');
                            if (!variables.TryGetValue(varParts[0].Trim(), out var variable))
                                throw new SyntaxErrorException(CurrentToken, "unknown variable: " + varParts[0]);
                            var address = variable.Address;
                            if (varParts.Length == 2)
                                address += int.Parse(varParts[1].Trim());
                            param = param.Substring(0, match.Index) + $"0{address.ToString("X3")}h" + param.Substring(match.Index + match.Length);
                        }
                        _mnemonics.Add(new Mnemonic(cmd, param));
                    }
                }
            }
        }

        private void ParseBlock(Dictionary<string, BaseVar> variables, string lblStart, string blockName)
        {
            var localVars = new HashSet<string>();

            while (CurrentTokenType(new[] { TokenType.Const, TokenType.Var, TokenType.Procedure }))
            {
                // constants
                if (Accept(TokenType.Const))
                {
                    do
                    {
                        Expect(TokenType.Identifier);
                        Expect(TokenType.Equal);
                        Expect(TokenType.Number);
                    } while (Accept(TokenType.Comma));
                    Expect(TokenType.Semicolon);
                }

                // variables
                if (Accept(TokenType.Var))
                {
                    do
                    {
                        var varName = Expect(TokenType.Identifier).Value;
                        Expect(TokenType.Colon);
                        var dataType = Expect(TokenType.DataType).Value;
                        if (Accept(TokenType.OpenBracket))
                        {
                            // array variable
                            var length = ParseNumber(Expect(TokenType.Number).Value);
                            Expect(TokenType.CloseBracket);
                            variables.Add(varName, _varProvider.DefineArray(varName, dataType, length, blockName));
                            localVars.Add(varName);
                        }
                        else
                        {
                            // normal variable
                            variables.Add(varName, _varProvider.DefineVar(varName, dataType, blockName));
                            localVars.Add(varName);
                        }

                    } while (Accept(TokenType.Comma));
                    Expect(TokenType.Semicolon);
                }

                // procedure body
                if (Accept(TokenType.Procedure))
                {
                    var procName = Expect(TokenType.Identifier).Value;
                    Expect(TokenType.Semicolon);

                    var procLbl = "@proc_" + procName;
                    var proc = new ProcInfo(procName, procLbl)
                    {
                        ReturnIdVar = _varProvider.DefineVar("$procret_" + procName, DataType.Nibble)
                    };
                    _procInfos.Add(proc.Name, proc);

                    ParseBlock(variables, procLbl, procName);
                    Expect(TokenType.Semicolon);

                    var procEnd = new Mnemonic("NOP");
                    proc.ProcEndLineId = procEnd.LineId;
                    _mnemonics.Add(procEnd);
                    _mnemonics.Add(new Mnemonic("; endproc " + procName));
                }
            }

            _mnemonics.Add(new Mnemonic(lblStart + ":"));
            ParseStatement(variables);

            foreach (var varName in localVars)
                variables.Remove(varName);
        }

        private void ParseProgram()
        {
            _codeGen.GenInitializer();
            ParseBlock(new Dictionary<string, BaseVar>(), "@main", "main");
            Expect(TokenType.Dot);
        }

        private Token Expect(TokenType[] tokenTypes)
        {
            if (_position >= _tokens.Count)
                throw new SyntaxErrorException(_tokens[_tokens.Count - 1], "unexpected end of file");

            var token = _tokens[_position++];
            if (tokenTypes.Contains(token.Type))
                return token;
            throw new SyntaxErrorException(token, "unexpected symbol: " + token.Value);
        }

        private void FreeSystemVar(Variable variable)
        {
            if (_varProvider.FreeSystemVariable(variable))
                _codeGen.GenFreeVarInfo(variable);
        }

        private Token Expect(TokenType tokenType)
        {
            return Expect(new[] { tokenType });
        }

        private bool Accept(TokenType tokenType)
        {
            return Accept(tokenType, out var token);
        }

        private bool Accept(TokenType tokenType, out Token token)
        {
            token = null;
            if (_position >= _tokens.Count)
                return false;

            token = _tokens[_position];
            if (token.Type == tokenType)
            {
                _position++;
                return true;
            }
            return false;
        }

        private bool CurrentTokenType(TokenType[] tokenTypes)
        {
            foreach (var tokenType in tokenTypes)
            {
                if (CurrentToken.Type == tokenType)
                    return true;
            }
            return false;
        }

        private void GenerateProcedureLogic()
        {
            foreach (var proc in _procInfos.Values)
            {
                if (proc.Calls.Count == 0) continue;
                _codeGen.GenProcLogic(proc);
            }
        }
    }
}
