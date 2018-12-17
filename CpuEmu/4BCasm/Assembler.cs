using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _4BCasm
{
    class VarDef
    {
        public string Name { get; set; }

        public int Address { get; set; }
    }

    class Label
    {
        public string Name { get; set; }

        public int? Address { get; set; }

        public List<int> Positions { get; } = new List<int>();
    }

    class AssemblerException : Exception
    {
        public AssemblerException(string message, int lineNo)
            : base(message)
        {
            LineNo = lineNo;
        }

        public int LineNo { get; }

        public override string ToString()
        {
            return Message + " at line no " + LineNo;
        }
    }

    class Assembler
    {
        private const int MaxVars = 4096;

        private string[] _lines;

        private int _lineNo;

        private List<byte> _data = new List<byte>();

        private Dictionary<string, VarDef> _variables = new Dictionary<string, VarDef>();

        private int _varPtr;

        private Dictionary<string, Label> _labels = new Dictionary<string, Label>();

        public Assembler(string[] lines)
        {
            _lines = lines;
        }

        public void AssembleTo(string fileName, out int bytes)
        {
            _data.Clear();
            _variables.Clear();
            _labels.Clear();
            _varPtr = 0;

            try
            {
                for (_lineNo = 0; _lineNo < _lines.Length; _lineNo++)
                {
                    var line = _lines[_lineNo].Trim();
                    if (line == "" || line.StartsWith(";")) continue;

                    var parts = line.Split(new[] { ' ', '\t' }).Select(_ => _.ToLowerInvariant()).ToArray();

                    if (parts.Length == 2 && parts[1].Length == 2 && parts[1].StartsWith("d"))
                        DefineVar(parts[0], parts[1]);
                    else if (parts.Length == 1 && parts[0].StartsWith("@") && parts[0].EndsWith(":"))
                        DefineLabel(parts[0].Substring(0, parts[0].Length - 1));
                    else
                        ParseInstruction(parts);
                }
                UpdateLabels();

                File.WriteAllBytes(fileName, _data.ToArray());
            }
            catch (AssemblerException ex)
            {
                Console.WriteLine(ex.Message);
            }
            bytes = _data.Count;
        }

        private void ParseInstruction(string[] parts)
        {
            var instruction = parts[0];
            var parameter = parts.Length > 1 ? parts[1] : null;

            switch (instruction)
            {
                case "nop":
                    _data.Add(0);
                    break;

                case "lda":
                    {
                        if (parameter == null)
                            throw new AssemblerException("parameter missing!", _lineNo + 1);
                        if (Char.IsDigit(parameter[0]))
                        {
                            // LDA imm
                            AddImmInstruction(0x03, Get4BValue(parameter));
                        }
                        else if (parameter.StartsWith("[b]"))
                        {
                            // LDA indirect
                            AddIndirectInstruction(0x02, GetValue(parameter.Substring(3)) & 0xFFF);
                        }
                        else
                        {
                            // LDA mem
                            var memAddr = GetMemAddr(parameter);
                            AddMemInstruction(0x01, memAddr);
                        }
                    }
                    break;

                case "sta":
                    {
                        if (parameter == null)
                            throw new AssemblerException("parameter missing!", _lineNo + 1);

                        else if (parameter.StartsWith("[b]"))
                        {
                            // STA indirect
                            AddIndirectInstruction(0x05, GetValue(parameter.Substring(3)) & 0xFFF);
                        }
                        else
                        {
                            // STA mem
                            var memAddr = GetMemAddr(parameter);
                            AddMemInstruction(0x04, memAddr);
                        }
                    }
                    break;

                case "ldbl":
                    _data.Add(0x06);
                    break;

                case "ldbh":
                    _data.Add(0x16);
                    break;

                case "not":
                    _data.Add(0x07);
                    break;

                case "add":
                    _data.Add(0x17);
                    break;

                case "sub":
                    _data.Add(0x27);
                    break;

                case "and":
                    _data.Add(0x37);
                    break;

                case "or":
                    _data.Add(0x47);
                    break;

                case "shl":
                    _data.Add(0x57);
                    break;

                case "shr":
                    _data.Add(0x67);
                    break;

                case "xor":
                    _data.Add(0x77);
                    break;

                case "jmp":
                    AddJumpInstruction(0x08, parameter);
                    break;

                case "ja":
                    AddJumpInstruction(0x18, parameter);
                    break;

                case "jb":
                    AddJumpInstruction(0x28, parameter);
                    break;

                case "jz":
                case "je":
                    AddJumpInstruction(0x38, parameter);
                    break;

                case "stc":
                    AddImmInstruction(0x09, Get4BValue(parameter));
                    break;

                case "bnk":
                    AddImmInstruction(0x0A, Get4BValue(parameter));
                    break;

                case "cmp":
                    _data.Add(0x2B);
                    break;

                case "out":
                    AddImmInstruction(0x0C, Get4BValue(parameter));
                    break;

                case "in":
                    AddImmInstruction(0x0D, Get4BValue(parameter));
                    break;

                case "hlt":
                    _data.Add(0x0E);
                    break;

                default:
                    throw new AssemblerException($"unknown command: {instruction}", _lineNo + 1);
            }
        }

        private void AddJumpInstruction(byte instCode, string labelName)
        {
            _data.Add(instCode);

            UpdateLabelUsage(labelName, _data.Count);
            _data.Add(0);
            _data.Add(0);
        }

        private int GetValue(string strValue)
        {
            if (strValue.EndsWith("h"))
            {
                var hex = strValue.Substring(0, strValue.Length - 1);
                return (byte)int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            }

            if (strValue.StartsWith("0x"))
            {
                var hex = strValue.Substring(2);
                return (byte)int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            }

            if (!int.TryParse(strValue, out int value))
                throw new AssemblerException("syntax error", _lineNo + 1);
            return value;
        }

        private byte Get4BValue(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                throw new AssemblerException("syntax error", _lineNo + 1);
            return (byte) (GetValue(strValue) & 0xF);
        }

        private void AddImmInstruction(int instCode, byte imm)
        {
            _data.Add((byte)(instCode + (imm << 4)));
        }

        private int GetMemAddr(string parameter)
        {
            if (parameter.StartsWith("["))
            {
                if (!parameter.EndsWith("]"))
                    throw new AssemblerException("syntax error", _lineNo + 1);
                var strValue = parameter.Substring(1, parameter.Length - 2);
                return GetValue(strValue) & 0xFFF;
            }

            if (parameter.Contains("[") || parameter.Contains("]"))
                throw new AssemblerException("syntax error", _lineNo + 1);
            return GetVar(parameter).Address;
        }

        private void AddMemInstruction(int instCode, int memAddr)
        {
            _data.Add((byte)(instCode + ((memAddr & 0xF00) >> 4)));
            _data.Add((byte)(memAddr & 0x0FF));
        }

        private void AddIndirectInstruction(int instCode, int offset)
        {
            if ((offset & 0x0FF) != 0)
                throw new AssemblerException("invalid offset", _lineNo + 1);
            _data.Add((byte)(instCode + ((offset & 0xF00) >> 4)));
        }

        private VarDef GetVar(string varName)
        {
            if (!_variables.TryGetValue(varName, out var varDef))
                throw new AssemblerException("unknown variable!", _lineNo + 1);
            return varDef;
        }

        private void DefineLabel(string labelName)
        {
            Label label;
            if (!_labels.TryGetValue(labelName, out label))
                _labels.Add(labelName, new Label { Name = labelName, Address = _data.Count });
            else
            {
                if (label.Address.HasValue)
                    throw new AssemblerException("label already defined!", _lineNo + 1);
                label.Address = _data.Count;
            }
        }

        private void UpdateLabelUsage(string labelName, int position)
        {
            Label label;
            if (!_labels.TryGetValue(labelName, out label))
                _labels.Add(labelName, label = new Label { Name = labelName});
            label.Positions.Add(position);
        }

        private void UpdateLabels()
        {
            foreach (var label in _labels.Values)
            {
                if (label.Address == null)
                    throw new AssemblerException($"label {label.Name} not defined!", 0);
                foreach (var pos in label.Positions)
                {
                    _data[pos] = (byte)(label.Address & 0x00FF);
                    _data[pos + 1] = (byte)((label.Address & 0xFF00) >> 8);
                }
            }
        }

        private void DefineVar(string name, string type)
        {
            if (_varPtr >= MaxVars)
                throw new AssemblerException("too many variables!", _lineNo + 1);
            _variables.Add(name, new VarDef { Name = name, Address = _varPtr++ });
        }
    }
}
