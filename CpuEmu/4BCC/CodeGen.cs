using System;
using System.Collections.Generic;

namespace _4BCC
{
    class Mnemonic
    {
        private static int _lineId = 0;

        public Mnemonic(string command, string param1 = "", string param2 = "")
        {
            Command = command;
            Param = param1;
            LineId = _lineId++;
        }

        public string Command { get; set; }

        public string Param { get; set; }

        public int LineId { get; }

        public string Line
        {
            get
            {
                if (Command.EndsWith(":") || Command.StartsWith(";"))
                    return Command;

                var result = "\t" + Command;
                if (!string.IsNullOrEmpty(Param))
                    result += " " + Param;
                return result;
            }
        }
    }

    class CondBranching
    {
        public List<Mnemonic> JumpIfTrue { get; } = new List<Mnemonic>();

        public List<Mnemonic> JumpIfFalse { get; } = new List<Mnemonic>();

        public Mnemonic AddTrueJump(string cmd)
        {
            var jmpCmd = new Mnemonic(cmd);
            JumpIfTrue.Add(jmpCmd);
            return jmpCmd;
        }

        public Mnemonic AddFalseJump(string cmd)
        {
            var jmpCmd = new Mnemonic(cmd);
            JumpIfFalse.Add(jmpCmd);
            return jmpCmd;
        }

        public void SetTrueJumpLabel(string labelName)
        {
            foreach (var mnemonic in JumpIfTrue)
                mnemonic.Param = labelName;
        }

        public void SetFalseJumpLabel(string labelName)
        {
            foreach (var mnemonic in JumpIfFalse)
                mnemonic.Param = labelName;
        }
    }

    class CodeGen
    {
        private List<Mnemonic> _mnemonics = new List<Mnemonic>();

        private VarProvider _varProvider;

        private int _labelNo;

        public CodeGen(List<Mnemonic> mnemonics, VarProvider varProvider)
        {
            _mnemonics = mnemonics;
            _varProvider = varProvider;
        }

        public string GenLabel()
        {
            var lbl = $"@label{_labelNo++}";
            return lbl;
        }

        public void GenAddition(Variable var1, Variable var2, Variable resultVar)
        {
            GenAluOp("ADD", var1, var2, resultVar, true);
        }

        public void GenSubstraction(Variable var1, Variable var2, Variable resultVar)
        {
            GenAluOp("SUB", var1, var2, resultVar, true);
        }

        public void GenLogicalAnd(Variable var1, Variable var2, Variable resultVar)
        {
            GenAluOp("AND", var1, var2, resultVar);
        }

        public void GenLogicalOr(Variable var1, Variable var2, Variable resultVar)
        {
            GenAluOp("OR", var1, var2, resultVar);
        }

        public void GenLogicalXor(Variable var1, Variable var2, Variable resultVar)
        {
            GenAluOp("XOR", var1, var2, resultVar);
        }

        public void GenIncrement(Variable var1)
        {
            _mnemonics.Add(new Mnemonic("STC", "1"));
            _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("LDBL"));
            GenAssignment(var1, var1, "ADD");
        }

        public void GenDecrement(Variable var1)
        {
            _mnemonics.Add(new Mnemonic("STC", "1"));
            _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("LDBL"));
            GenAssignment(var1, var1, "SUB");
        }

        public void GenComparison(Variable var1, Variable var2, string lblAbove, string lblBelow)
        {
            _mnemonics.Add(new Mnemonic("STC", "0"));
            if (var1.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address + 3)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 3)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(new Mnemonic("JA", lblAbove));
                _mnemonics.Add(new Mnemonic("JB", lblBelow));

                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address + 2)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 2)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(new Mnemonic("JA", lblAbove));
                _mnemonics.Add(new Mnemonic("JB", lblBelow));
            }
            if (var1.Type > DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address + 1)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(new Mnemonic("JA", lblAbove));
                _mnemonics.Add(new Mnemonic("JB", lblBelow));
            }
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address)));
            _mnemonics.Add(new Mnemonic("LDBL"));
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            _mnemonics.Add(new Mnemonic("CMP"));
            _mnemonics.Add(new Mnemonic("JA", lblAbove));
            _mnemonics.Add(new Mnemonic("JB", lblBelow));
        }

        public void GenArrayRead(ArrayVar arrayVar, Variable indexVar, Variable resultVar)
        {
            var dataSize = GetDataSize(arrayVar.Type);

            // 1. move index to temp variable
            var tempIndex = _varProvider.GetSystemVariable(DataType.Byte);
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(indexVar.Address)));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(tempIndex.Address)));
            if (indexVar.Type > DataType.Nibble)
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(indexVar.Address + 1)));
            else
                _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(tempIndex.Address + 1)));

            // 2. multiply index with 2 (byte array) or 4 (word array)
            if (dataSize > 1)
                GenShl(tempIndex);
            if (dataSize == 4)
                GenShl(tempIndex);

            // 3. fetch data
            for (var i = 0; i < dataSize; i++)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(tempIndex.Address)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(tempIndex.Address + 1)));
                _mnemonics.Add(new Mnemonic("LDBH"));

                _mnemonics.Add(new Mnemonic("BNK", arrayVar.Bank.ToString()));
                _mnemonics.Add(new Mnemonic("LDA", "[B]0" + arrayVar.Address.ToString("X3") + "h"));
                _mnemonics.Add(new Mnemonic("BNK", "0"));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + i)));

                GenIncrement(tempIndex);
            }

            _varProvider.FreeSystemVariable(tempIndex);
        }

        public void GenArrayWrite(ArrayVar arrayVar, Variable indexVar, Variable sourceVar)
        {
            var dataSize = GetDataSize(arrayVar.Type);

            // 1. move index to temp variable
            var tempIndex = _varProvider.GetSystemVariable(DataType.Byte);
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(indexVar.Address)));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(tempIndex.Address)));
            if (indexVar.Type > DataType.Nibble)
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(indexVar.Address + 1)));
            else
                _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(tempIndex.Address + 1)));

            // 2. multiply index with 2 (byte array) or 4 (word array)
            if (dataSize > 1)
                GenShl(tempIndex);
            if (dataSize == 4)
                GenShl(tempIndex);

            // 3. fetch data
            for (var i = 0; i < dataSize; i++)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(tempIndex.Address)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(tempIndex.Address + 1)));
                _mnemonics.Add(new Mnemonic("LDBH"));

                _mnemonics.Add(new Mnemonic("LDA", HexAddr(sourceVar.Address + i)));
                _mnemonics.Add(new Mnemonic("BNK", arrayVar.Bank.ToString()));
                _mnemonics.Add(new Mnemonic("STA", "[B]0" + arrayVar.Address.ToString("X3") + "h"));
                _mnemonics.Add(new Mnemonic("BNK", "0"));

                GenIncrement(tempIndex);
            }

            _varProvider.FreeSystemVariable(tempIndex);
        }

        private int GetDataSize(DataType type)
        {
            switch (type)
            {
                case DataType.Nibble:
                    return 1;
                case DataType.Byte:
                    return 2;
                case DataType.Word:
                    return 4;
            }
            return 0;
        }

        public void GenShl(Variable var1)
        {
            _mnemonics.Add(new Mnemonic("STC", "0"));
            GenAssignment(var1, var1, "SHL");
        }

        public void GenShr(Variable var1)
        {
            _mnemonics.Add(new Mnemonic("STC", "0"));
            if (var1.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 3)));
                _mnemonics.Add(new Mnemonic("SHR"));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address + 3)));

                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 2)));
                _mnemonics.Add(new Mnemonic("SHR"));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address + 2)));
            }

            if (var1.Type > DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("SHR"));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address + 1)));
            }

            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            _mnemonics.Add(new Mnemonic("SHR"));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address)));
        }

        public Variable GenLo(Variable var1)
        {
            if (var1.Type == DataType.Byte)
            {
                var resultVar = _varProvider.GetSystemVariable(DataType.Nibble);
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
                return resultVar;
            }
            else if (var1.Type == DataType.Word)
            {
                var resultVar = _varProvider.GetSystemVariable(DataType.Byte);
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
                return resultVar;
            }
            else
                return var1;
        }

        public Variable GenHi(Variable var1)
        {
            if (var1.Type == DataType.Byte)
            {
                var resultVar = _varProvider.GetSystemVariable(DataType.Nibble);
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
                return resultVar;
            }
            else if (var1.Type == DataType.Word)
            {
                var resultVar = _varProvider.GetSystemVariable(DataType.Byte);
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 2)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 3)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
                return resultVar;
            }
            else
                return var1;
        }

        public void GenAssignment(Variable var1, Variable resultVar, string cmd = "")
        {
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            if (cmd != "") _mnemonics.Add(new Mnemonic(cmd));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
            if (var1.Type > DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                if (cmd != "") _mnemonics.Add(new Mnemonic(cmd));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
            }
            if (var1.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 2)));
                if (cmd != "") _mnemonics.Add(new Mnemonic(cmd));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 2)));

                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 3)));
                if (cmd != "") _mnemonics.Add(new Mnemonic(cmd));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 3)));
            }
        }

        public void GenAssignment(int number, Variable resultVar)
        {
            _mnemonics.Add(new Mnemonic("LDA", HexNibble(number & 0x000F)));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
            if (resultVar.Type > DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexNibble((number & 0x00F0) >> 4)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
            }
            if (resultVar.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexNibble((number & 0x0F00) >> 8)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 2)));

                _mnemonics.Add(new Mnemonic("LDA", HexNibble((number & 0xF000) >> 12)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 3)));
            }
        }

        public void GenStringAssignment(string str, ArrayVar arrayVar)
        {
            _mnemonics.Add(new Mnemonic("BNK", arrayVar.Bank.ToString()));

            var addrStr = arrayVar.Address.ToString("X3");
            var memIdx = 0;
            for (var idx = 0; idx < str.Length; idx++)
            {
                var value = (int)str[idx];
                _mnemonics.Add(new Mnemonic("LDA", HexNibble(memIdx & 0x0F)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                if ((memIdx & 0xF0) != ((memIdx - 1) & 0xF0))
                {
                    _mnemonics.Add(new Mnemonic("LDA", HexNibble((memIdx & 0xF0) >> 4)));
                    _mnemonics.Add(new Mnemonic("LDBH"));
                }
                _mnemonics.Add(new Mnemonic("LDA", HexNibble(value & 0x0F)));
                _mnemonics.Add(new Mnemonic("STA", $"[B]0{addrStr}h"));
                memIdx++;

                _mnemonics.Add(new Mnemonic("LDA", HexNibble(memIdx & 0x0F)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                if ((memIdx & 0xF0) != ((memIdx - 1) & 0xF0))
                {
                    _mnemonics.Add(new Mnemonic("LDA", HexNibble((memIdx & 0xF0) >> 4)));
                    _mnemonics.Add(new Mnemonic("LDBH"));
                }
                _mnemonics.Add(new Mnemonic("LDA", HexNibble((value & 0xF0) >> 4)));
                _mnemonics.Add(new Mnemonic("STA", $"[B]0{addrStr}h"));
                memIdx++;
            }

            _mnemonics.Add(new Mnemonic("BNK", "0"));
        }

        public void ChangeType(Variable var1, Variable resultVar)
        {
            if (var1.Type == resultVar.Type)
                throw new Exception("conversion error");

            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));

            if (var1.Type > resultVar.Type)
            {
                if (resultVar.Type == DataType.Nibble) return;

                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
                return;
            }

            if (var1.Type == DataType.Byte)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
                _mnemonics.Add(new Mnemonic("LDA", "0"));
            }
            else 
            {
                _mnemonics.Add(new Mnemonic("LDA", "0"));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));
            }

            if (resultVar.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 2)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 3)));
            }
        }

        public void GenMultiplication(Variable varX, Variable varY, Variable resultVar)
        {
            _mnemonics.Add(new Mnemonic($"; {resultVar.Name} = {varX.Name} * {varY.Name}"));

            var dataType = varX.Type;

            // a := x;
            // b:= Y;
            // z:= 0;
            var varA = _varProvider.GetSystemVariable(dataType);
            var varB = _varProvider.GetSystemVariable(dataType);
            AddComment("a := x");
            GenAssignment(varX, varA);
            AddComment("b := y");
            GenAssignment(varY, varB);
            AddComment("z := 0");
            SetVarZero(resultVar);

            // WHILE b > 0 DO
            AddComment("WHILE b > 0 DO");

            var lblLoopStart = GenLabel();
            var lblLoopExit = GenLabel();
            var lblLoopBody = GenLabel();
            _mnemonics.Add(new Mnemonic(lblLoopStart + ":"));
            _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("LDBL"));

            _mnemonics.Add(new Mnemonic("STC", "0"));
            if (dataType == DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(varB.Address)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(new Mnemonic("JZ", lblLoopExit));
            }
            else
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(varB.Address)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(new Mnemonic("JA", lblLoopBody));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(varB.Address + 1)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(new Mnemonic("JA", lblLoopBody));

                if (dataType == DataType.Word)
                {
                    _mnemonics.Add(new Mnemonic("LDA", HexAddr(varB.Address + 2)));
                    _mnemonics.Add(new Mnemonic("CMP"));
                    _mnemonics.Add(new Mnemonic("JA", lblLoopBody));
                    _mnemonics.Add(new Mnemonic("LDA", HexAddr(varB.Address + 3)));
                    _mnemonics.Add(new Mnemonic("CMP"));
                    _mnemonics.Add(new Mnemonic("JA", lblLoopBody));
                }
                _mnemonics.Add(new Mnemonic("JMP", lblLoopExit));
                _mnemonics.Add(new Mnemonic(lblLoopBody + ":"));
            }

            // IF ODD b THEN z := z + a;
            AddComment("IF ODD b THEN");
            var lblIf = GenLabel();
            _mnemonics.Add(new Mnemonic("LDA", "1"));
            _mnemonics.Add(new Mnemonic("LDBL"));
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(varB.Address)));
            _mnemonics.Add(new Mnemonic("AND"));
            _mnemonics.Add(new Mnemonic("JZ", lblIf));

            AddComment("z := z + a;");
            GenAddition(resultVar, varA, resultVar);

            _mnemonics.Add(new Mnemonic(lblIf + ":"));

            // a := 2 * a;
            AddComment("a := 2 * a;");
            GenShl(varA);

            // b := b / 2;
            AddComment("b := b / 2;");
            GenShr(varB);

            _mnemonics.Add(new Mnemonic("JMP", lblLoopStart));
            _mnemonics.Add(new Mnemonic(lblLoopExit + ":"));

            _varProvider.FreeSystemVariable(varA);
            _varProvider.FreeSystemVariable(varB);
        }

        public void GenFreeVarInfo(Variable var1)
        {
            _mnemonics.Add(new Mnemonic("; free " + HexAddr(var1.Address)));
        }

        public void GenDivision(Variable varX, Variable varY, Variable resultVar, bool modulo = false)
        {
            _mnemonics.Add(new Mnemonic($"; {resultVar.Name} = {varX.Name} / {varY.Name}"));

            var dataType = varX.Type;

            // r := x;
            // q := 0;
            // w := y;
            var varR = _varProvider.GetSystemVariable(dataType);
            var varW = _varProvider.GetSystemVariable(dataType);
            GenAssignment(varX, varR);
            GenAssignment(varY, varW);
            SetVarZero(resultVar);

            //   WHILE w <= r DO w := 2 * w;
            AddComment("WHILE w <= r DO w := 2 * w;");
            var lblLoop1Start = GenLabel();
            var lblLoop1Body = GenLabel();
            var lblLoop1Exit = GenLabel();
            _mnemonics.Add(new Mnemonic(lblLoop1Start + ":"));
            GenComparison(varW, varR, lblLoop1Exit, lblLoop1Body);

            _mnemonics.Add(new Mnemonic(lblLoop1Body + ":"));

            GenShl(varW);

            _mnemonics.Add(new Mnemonic("JMP", lblLoop1Start));
            _mnemonics.Add(new Mnemonic(lblLoop1Exit + ":"));

            //  WHILE w > y DO
            AddComment("WHILE w > y DO");
            var lblLoop2Start = GenLabel();
            var lblLoop2Body = GenLabel();
            var lblLoop2Exit = GenLabel();
            _mnemonics.Add(new Mnemonic(lblLoop2Start + ":"));

            GenComparison(varW, varY, lblLoop2Body, lblLoop2Exit);
            _mnemonics.Add(new Mnemonic("JMP", lblLoop2Exit));

            _mnemonics.Add(new Mnemonic(lblLoop2Body + ":"));

            // q := 2 * q;
            AddComment("q := 2 * q;");
            GenShl(resultVar);

            // w := w / 2;
            AddComment("w := w / 2;");
            GenShr(varW);

            // IF w <= r THEN
            AddComment("IF w <= r THEN");
            var lblIfBody = GenLabel();
            GenComparison(varW, varR, lblLoop2Start, lblIfBody);
            _mnemonics.Add(new Mnemonic(lblIfBody + ":"));

            // r := r - w;
            AddComment("r := r - w");
            GenSubstraction(varR, varW, varR);

            // q := q + 1
            AddComment("q := q + 1");
            GenIncrement(resultVar);

            _mnemonics.Add(new Mnemonic("JMP", lblLoop2Start));
            _mnemonics.Add(new Mnemonic(lblLoop2Exit + ":"));

            if (modulo)
                GenAssignment(varR, resultVar);

            _varProvider.FreeSystemVariable(varR);
            _varProvider.FreeSystemVariable(varW);
        }

        public void GenOut(int portNo, Variable valueVar)
        {
            _mnemonics.Add(new Mnemonic("LDA", $"[{HexWord(valueVar.Address)}]"));
            _mnemonics.Add(new Mnemonic("OUT", HexNibble(portNo)));
        }

        public void GenIn(int portNo, Variable resultVar)
        {
            _mnemonics.Add(new Mnemonic("IN", HexNibble(portNo)));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address)));
            if (resultVar.Type != DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", "0"));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 1)));

                if (resultVar.Type == DataType.Word)
                {
                    _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 2)));
                    _mnemonics.Add(new Mnemonic("STA", HexAddr(resultVar.Address + 3)));
                }
            }
        }

        public CondBranching CheckOdd(Variable var1)
        {
            var condEval = new CondBranching();

            _mnemonics.Add(new Mnemonic("LDA", "1"));
            _mnemonics.Add(new Mnemonic("LDBL"));
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            _mnemonics.Add(new Mnemonic("AND"));

            _mnemonics.Add(condEval.AddFalseJump("JZ"));
            _mnemonics.Add(condEval.AddTrueJump("JMP"));

            return condEval;
        }

        public CondBranching CheckNotZero(Variable var1)
        {
            var condEval = new CondBranching();
            _mnemonics.Add(new Mnemonic("STC", "0"));
            _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("LDBL"));
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            _mnemonics.Add(new Mnemonic("CMP"));
            _mnemonics.Add(condEval.AddTrueJump("JA"));

            if (var1.Type > DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("CMP"));
                _mnemonics.Add(condEval.AddTrueJump("JA"));

                if (var1.Type == DataType.Word)
                {
                    _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 2)));
                    _mnemonics.Add(new Mnemonic("CMP"));
                    _mnemonics.Add(condEval.AddTrueJump("JA"));

                    _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 3)));
                    _mnemonics.Add(new Mnemonic("CMP"));
                    _mnemonics.Add(condEval.AddTrueJump("JA"));
                }
            }

            _mnemonics.Add(condEval.AddFalseJump("JMP"));

            return condEval;
        }

        public void GenProcLogic(ProcInfo proc)
        {
            var procEndIndex = FindMnemonicIndex(proc.ProcEndLineId);
            if (proc.Calls.Count == 1)
            {
                _mnemonics[procEndIndex] = new Mnemonic("JMP", proc.Calls[0].ReturnLabel);
            }
            else if (proc.Calls.Count <= 16)
            {
                var midx = procEndIndex;
                _mnemonics.RemoveAt(midx);

                _mnemonics.Insert(midx++, new Mnemonic("STC", "0"));
                _mnemonics.Insert(midx++, new Mnemonic("LDA", "1"));
                _mnemonics.Insert(midx++, new Mnemonic("LDBL"));
                _mnemonics.Insert(midx++, new Mnemonic("LDA", HexAddr(proc.ReturnIdVar.Address)));
                _mnemonics.Insert(midx++, new Mnemonic("SUB"));
                _mnemonics.Insert(midx++, new Mnemonic("JB", proc.Calls[0].ReturnLabel));

                GenSetCallId(FindMnemonicIndex(proc.Calls[0].CallLineId), 0, proc.Calls[0], proc);

                for (var callIdx = 1; callIdx < proc.Calls.Count; callIdx++)
                {
                    var call = proc.Calls[callIdx];
                    var callMnemonicIdx = FindMnemonicIndex(call.CallLineId);

                    GenSetCallId(callMnemonicIdx, callIdx, call, proc);

                    if (callIdx == proc.Calls.Count - 1)
                    {
                        _mnemonics.Insert(midx++, new Mnemonic("JMP", call.ReturnLabel));
                    }
                    else
                    {
                        _mnemonics.Insert(midx++, new Mnemonic("JZ", call.ReturnLabel));
                        if (callIdx < proc.Calls.Count - 2)
                            _mnemonics.Insert(midx++, new Mnemonic("SUB"));
                    }
                }
            }
            else
                throw new SyntaxErrorException("", 0, $"too many calls of procedure {proc.Name}");
        }

        public CondBranching EvalCondition(Variable var1, Variable var2, TokenType cond)
        {
            var condEval = new CondBranching();

            if (var1.Type != var2.Type) throw new Exception("types not equal in EvalCondition()");

            _mnemonics.Add(new Mnemonic("STC", "0"));

            if (var1.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address + 3)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 3)));
                _mnemonics.Add(new Mnemonic("CMP"));
                GenBranching(cond, condEval, false);

                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address + 2)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 2)));
                _mnemonics.Add(new Mnemonic("CMP"));
                GenBranching(cond, condEval, false);
            }

            if (var1.Type > DataType.Nibble)
            {
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address + 1)));
                _mnemonics.Add(new Mnemonic("LDBL"));
                _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address + 1)));
                _mnemonics.Add(new Mnemonic("CMP"));
                GenBranching(cond, condEval, false);
            }

            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var2.Address)));
            _mnemonics.Add(new Mnemonic("LDBL"));
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(var1.Address)));
            _mnemonics.Add(new Mnemonic("CMP"));
            GenBranching(cond, condEval, true);

            return condEval;
        }

        public void GenInitializer()
        {
            _mnemonics.Add(new Mnemonic("BNK", "0"));
            _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("OUT", "0"));
            _mnemonics.Add(new Mnemonic("OUT", "1"));
            _mnemonics.Add(new Mnemonic("OUT", "2"));
            _mnemonics.Add(new Mnemonic("OUT", "3"));
            _mnemonics.Add(new Mnemonic("LDA", "02h"));
            _mnemonics.Add(new Mnemonic("OUT", "4"));
            _mnemonics.Add(new Mnemonic("JMP", "@main"));
        }

        public void GenNop()
        {
            _mnemonics.Add(new Mnemonic("NOP"));
        }

        private void GenSetCallId(int callMnemonicIdx, int callIdx, ProcCall call, ProcInfo proc)
        {
            _mnemonics.Insert(callMnemonicIdx, new Mnemonic("LDA", HexNibble(callIdx)));
            _mnemonics.Insert(callMnemonicIdx + 1, new Mnemonic("STA", HexAddr(proc.ReturnIdVar.Address)));
        }

        private void GenBranching(TokenType cond, CondBranching condEval, bool lastNibble)
        {
            switch (cond)
            {
                case TokenType.Equal:
                    {
                        var lbl = GenLabel();
                        _mnemonics.Add(new Mnemonic("JE", lbl));
                        _mnemonics.Add(condEval.AddFalseJump("JMP"));
                        _mnemonics.Add(new Mnemonic(lbl + ":"));
                        if (lastNibble)
                            _mnemonics.Add(condEval.AddTrueJump("JMP"));
                    }
                    break;
                case TokenType.NotEqual:
                    {
                        var lbl = GenLabel();
                        _mnemonics.Add(new Mnemonic("JE", lbl));
                        _mnemonics.Add(condEval.AddTrueJump("JMP"));
                        _mnemonics.Add(new Mnemonic(lbl + ":"));
                        if (lastNibble)
                            _mnemonics.Add(condEval.AddFalseJump("JMP"));
                    }
                    break;
                case TokenType.Greater:
                    {
                        var lbl = GenLabel();
                        _mnemonics.Add(condEval.AddTrueJump("JA"));
                        _mnemonics.Add(condEval.AddFalseJump("JB"));
                        if (lastNibble)
                            _mnemonics.Add(condEval.AddFalseJump("JMP"));
                    }
                    break;
                case TokenType.GreaterEqual:
                    {
                        var lbl = GenLabel();
                        _mnemonics.Add(condEval.AddTrueJump("JA"));
                        _mnemonics.Add(condEval.AddFalseJump("JB"));
                        if (lastNibble)
                            _mnemonics.Add(condEval.AddTrueJump("JMP"));
                    }
                    break;
                case TokenType.Lesser:
                    {
                        var lbl = GenLabel();
                        _mnemonics.Add(condEval.AddTrueJump("JB"));
                        _mnemonics.Add(condEval.AddFalseJump("JA"));
                        if (lastNibble)
                            _mnemonics.Add(condEval.AddFalseJump("JMP"));
                    }
                    break;
                case TokenType.LesserEqual:
                    {
                        var lbl = GenLabel();
                        _mnemonics.Add(condEval.AddTrueJump("JB"));
                        _mnemonics.Add(condEval.AddFalseJump("JA"));
                        if (lastNibble)
                            _mnemonics.Add(condEval.AddTrueJump("JMP"));
                    }
                    break;
            }
        }

        private string HexAddr(int addr) => "[" + HexWord(addr) + "]";

        private string HexWord(int value) => $"0{value.ToString("X3")}h";

        private string HexByte(int value) => $"0{value.ToString("X2")}h";

        private string HexNibble(int value) => $"0{value.ToString("X1")}h";

        private void GenOp(string cmd, int address1, int address2, int? resultAddress = null, bool clearCarry = false)
        {
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(address2)));
            _mnemonics.Add(new Mnemonic("LDBL"));
            _mnemonics.Add(new Mnemonic("LDA", HexAddr(address1)));
            if (clearCarry)
                _mnemonics.Add(new Mnemonic("STC", "0"));
            _mnemonics.Add(new Mnemonic(cmd));

            if (resultAddress != null)
                _mnemonics.Add(new Mnemonic("STA", HexAddr(resultAddress.Value)));
        }

        private void GenAluOp(string cmd, Variable var1, Variable var2, Variable resultVar, bool useCarry = false)
        {
            _mnemonics.Add(new Mnemonic($"; {resultVar.Name} = {var1.Name} {cmd} {var2.Name}"));

            GenOp(cmd, var1.Address, var2.Address, resultVar?.Address, useCarry);

            if (var1.Type != DataType.Nibble)
                GenOp(cmd, var1.Address + 1, var2.Address + 1, resultVar.Address + 1);

            if (var1.Type == DataType.Word)
            {
                GenOp(cmd, var1.Address + 2, var2.Address + 2, resultVar.Address + 2);
                GenOp(cmd, var1.Address + 3, var2.Address + 3, resultVar.Address + 3);
            }
        }

        private void AddComment(string comment)
        {
            _mnemonics.Add(new Mnemonic("; " + comment));
        }

        private void SetVarZero(Variable var1)
        {
            _mnemonics.Add(new Mnemonic("LDA", "0"));
            _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address)));
            if (var1.Type != DataType.Nibble)
                _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address + 1)));
            if (var1.Type == DataType.Word)
            {
                _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address + 2)));
                _mnemonics.Add(new Mnemonic("STA", HexAddr(var1.Address + 3)));
            }
        }

        private int FindMnemonicIndex(int lineId)
        {
            for (var idx = 0; idx < _mnemonics.Count; idx++)
                if (_mnemonics[idx].LineId == lineId) return idx;
            throw new Exception($"line with id {lineId} not found.");
        }
    }
}
