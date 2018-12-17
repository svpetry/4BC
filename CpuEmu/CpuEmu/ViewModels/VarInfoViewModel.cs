using CpuEmu.Emulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CpuEmu.ViewModels
{
    class VarInfo : BaseViewModel
    {
        private string _varName;

        private string _value;

        public string VarName
        {
            get => _varName;
            set
            {
                if (_varName == value) return;
                _varName = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public int Address { get; set; }

        public int Size { get; set; }
    }

    class VarInfoViewModel : BaseViewModel
    {
        private IMemAccess _memAccess;

        public VarInfoViewModel(IMemAccess memAccess)
        {
            _memAccess = memAccess;
            Variables = new ObservableCollection<VarInfo>();
        }

        public ObservableCollection<VarInfo> Variables { get; }

        public void ParseVarList(string path)
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var fields = line.Split('\t');
                if (fields.Length >= 3 && fields[0].StartsWith("$"))
                {
                    var address = fields[0].Substring(1, 3);
                    if (address.Contains(":")) continue;
                    var fieldType = fields.Length == 3 ? fields[1] : fields[2];
                    var fieldName = fields.Length == 3 ? fields[2] : fields[3];
                    var varInfo = new VarInfo
                    {
                        Address = int.Parse(address, System.Globalization.NumberStyles.HexNumber),
                        VarName = fieldName
                    };
                    switch (fieldType)
                    {
                        case "Nibble":
                            varInfo.Size = 1;
                            break;
                        case "Byte":
                            varInfo.Size = 2;
                            break;
                        case "Word":
                            varInfo.Size = 4;
                            break;
                    }
                    Variables.Add(varInfo);
                }
            }
        }

        public void UpdateVariables()
        {
            foreach (var varInfo in Variables)
            {
                switch (varInfo.Size)
                {
                    case 1:
                        {
                            var value = _memAccess.ReadMem(varInfo.Address);
                            varInfo.Value = value.ToString();
                        }
                        break;
                    case 2:
                        {
                            var value = _memAccess.ReadMem(varInfo.Address)
                                + (_memAccess.ReadMem(varInfo.Address + 1) << 4);
                            varInfo.Value = value.ToString();
                        }
                        break;
                    case 4:
                        {
                            var value = _memAccess.ReadMem(varInfo.Address)
                                + (_memAccess.ReadMem(varInfo.Address + 1) << 4)
                                + (_memAccess.ReadMem(varInfo.Address + 2) << 8)
                                + (_memAccess.ReadMem(varInfo.Address + 3) << 12);
                            varInfo.Value = value.ToString();
                        }
                        break;
                }
            }
        }
    }
}
