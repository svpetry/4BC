using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace _4BCC
{
    abstract class BaseVar
    {
        public DataType Type { get; set; }

        public string Name { get; set; }

        public int Address { get; set; }

        public string BlockName { get; set; }
    }

    [DebuggerDisplay("Variable {Name} ({Type})")]
    class Variable : BaseVar
    {
    }

    [DebuggerDisplay("Array {Name}[{Length}] ({Type})")]
    class ArrayVar : BaseVar
    {
        public int Length { get; set; }

        public int Bank { get; set; }
    }

    class VarProvider
    {
        private const string SysvarPrefix = "$SYS";

        private const int MemSize = 4096;

        private int _freeArrayPtr;

        private int _freeBank = 1;

        private int _freeMemPtr;

        private Dictionary<DataType, Stack<Variable>> _systemVars = new Dictionary<DataType, Stack<Variable>>();

        private List<Variable> _allVariables = new List<Variable>();

        private List<ArrayVar> _arrays = new List<ArrayVar>();

        private int _sysVarCounter;

        private ILineNoProvider _lineNoProvider;

        public VarProvider(ILineNoProvider lineNoProvider)
        {
            _lineNoProvider = lineNoProvider;

            _systemVars.Add(DataType.Nibble, new Stack<Variable>());
            _systemVars.Add(DataType.Byte, new Stack<Variable>());
            _systemVars.Add(DataType.Word, new Stack<Variable>());
        }

        public Variable GetSystemVariable(DataType dataType)
        {
            Variable sysVar;
            var stack = _systemVars[dataType];
            if (stack.Count > 0)
                sysVar = stack.Pop();
            else
                sysVar = DefineVar(SysvarPrefix + _sysVarCounter++, dataType);
            return sysVar;
        }

        public bool FreeSystemVariable(Variable variable)
        {
            if (!variable.Name.StartsWith(SysvarPrefix)) return false;
            var stack = _systemVars[variable.Type];
            stack.Push(variable);
            return true;
        }

        public Variable DefineVar(string varName, string typeName, string blockName = "")
        {
            return DefineVar(varName, ParseDataType(typeName), blockName);
        }

        public Variable DefineVar(string varName, DataType dataType, string blockName = "")
        {
            if (_freeMemPtr >= MemSize)
                throw new NoFreeMemoryException(_lineNoProvider.CurrentFileName, _lineNoProvider.CurrentLineNo);

            var address = _freeMemPtr;
            switch (dataType)
            {
                case DataType.Nibble:
                    _freeMemPtr++;
                    break;
                case DataType.Byte:
                    _freeMemPtr += 2;
                    break;
                case DataType.Word:
                    _freeMemPtr += 4;
                    break;
            }

            var newVar = new Variable
            {
                Name = varName,
                Type = dataType,
                Address = address,
                BlockName = blockName
            };

            _allVariables.Add(newVar);
            return newVar;
        }

        public ArrayVar DefineArray(string varName, string typeName, int length, string blockName = "")
        {
            return DefineArray(varName, ParseDataType(typeName), length, blockName);
        }

        public ArrayVar DefineArray(string varName, DataType dataType, int length, string blockName)
        {
            if (_freeBank > 15)
                throw new NoFreeMemoryException(_lineNoProvider.CurrentFileName, _lineNoProvider.CurrentLineNo);

            var newArray = new ArrayVar
            {
                Name = varName,
                Type = dataType,
                Address = _freeArrayPtr * 256,
                Length = length,
                Bank = _freeBank,
                BlockName = blockName
            };

            if (++_freeArrayPtr == 16)
            {
                _freeArrayPtr = 0;
                _freeBank++;
            }

            _arrays.Add(newArray);
            return newArray;
        }

        public void ExportVariables(string filePath)
        {
            var lines = new List<string>();

            lines.Add(_freeMemPtr.ToString() + " bytes used.");
            lines.Add((MemSize - _freeMemPtr).ToString() + " bytes free.");
            lines.Add("");

            foreach (var variable in _allVariables)
            {
                var endAddr = "\t";
                if (variable.Type == DataType.Byte)
                    endAddr = "-$" + (variable.Address + 1).ToString("X3");
                else if (variable.Type == DataType.Word)
                    endAddr = "-$" + (variable.Address + 3).ToString("X3");

                lines.Add($"${variable.Address.ToString("X3")}{endAddr}\t{variable.Type.ToString()}\t{variable.Name}");
            }

            lines.Add("");
            foreach (var array in _arrays)
            {
                var size = 1;
                if (array.Type == DataType.Byte)
                    size = 2;
                else if (array.Type == DataType.Word)
                    size = 4;

                var bank = array.Bank.ToString();
                var startAddr = array.Address.ToString("X3");
                var endAddr = (array.Address + array.Length * size - 1).ToString("X3");
                lines.Add($"${bank}:{startAddr}-${bank}:{endAddr}\t{array.Type.ToString()}[{array.Length}]\t{array.Name}");
            }

            File.WriteAllLines(filePath, lines);
        }

        private DataType ParseDataType(string typeName)
        {
            switch (typeName)
            {
                case "nibble":
                    return DataType.Nibble;
                case "byte":
                    return DataType.Byte;
                case "word":
                    return DataType.Word;
            }
            return DataType.None;
        }
    }
}
