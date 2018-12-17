using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CpuEmu.Emulator
{
    public interface IMemAccess
    {
        byte ReadMem(int address);
    }

    enum PortMode
    {
        None,
        Read,
        Write
    }

    class PortWriteEventArgs : EventArgs
    {
        public PortWriteEventArgs(int address, int value)
        {
            Address = address;
            Value = value;
        }

        public int Address { get; }

        public int Value { get; }
    }

    class Cpu : IMemAccess
    {
        private byte _acc;

        private byte _bl;

        private byte _bh;

        private ushort _pc;

        private byte _memBank;

        private bool _carryFlag;

        private bool _zeroFlag;

        private ushort _memAddr;

        private byte[] _ports = new byte[16];

        private byte[] _memory = new byte[32768];

        private byte[] _rom = new byte[65536];

        public Cpu()
        {
        }

        public byte PortValue { get; set; }

        public byte PortAddress { get; set; }

        public byte ACC => _acc;

        public byte BL => _bl;

        public byte BH => _bh;

        public ushort PC => _pc;

        public bool CARRY => _carryFlag;

        public bool ZERO => _zeroFlag;

        public event EventHandler<PortWriteEventArgs> PortWrite;

        public void SetPort(byte address, byte value)
        {
            _ports[address & 0x0F] = (byte)(value & 0x0F);
        }

        public void Step()
        {
            GetInstruction(out var instr, out var imm);
            IncPc();
            ExecInstruction(instr, imm);
        }

        public void LoadRom(string _FileName)
        {
            var fileContent = File.ReadAllBytes(_FileName);
            for (var hIdx = 0; hIdx < _memory.Length && hIdx < fileContent.Length; hIdx++)
                _rom[hIdx] = fileContent[hIdx];
        }

        public void Reset()
        {
            _pc = 0;
        }

        public byte[] GetNextBytes()
        {
            return new byte[] { _rom[_pc], _rom[_pc + 1], _rom[_pc + 2] };
        }

        public byte ReadMem(int address)
        {
            if (address >= _memory.Length || address < 0) return 0;
            return _memory[address];
        }

        public void WriteMem(int address, byte value)
        {
            if (address >= _memory.Length || address < 0) return;
            _memory[address] = (byte)(value & 0x0F);
        }

        private void IncPc()
        {
            _pc = (ushort)((_pc + 1) & 0x7FFF);
        }

        private void ExecInstruction(byte instr, byte imm)
        {
            switch (instr)
            {
                case 0x00:
                    // NOP
                    break;

                case 0x01:
                    // LDA mem abs
                    _memAddr = (ushort)(_rom[_pc] + (imm << 8));
                    IncPc();
                    _acc = ReadMem(_memAddr + (_memBank << 12));
                    break;

                case 0x02:
                    // LDA mem ind
                    _memAddr = (ushort)(_bl + (_bh << 4) + (imm << 8));
                    _acc = ReadMem(_memAddr + (_memBank << 12));
                    break;

                case 0x03:
                    // LDA imm
                    _acc = imm;
                    break;

                case 0x04:
                    // STA mem abs
                    _memAddr = (ushort)(_rom[_pc] + (imm << 8));
                    IncPc();
                    WriteMem(_memAddr + (_memBank << 12), _acc);
                    break;

                case 0x05:
                    // STA mem ind
                    _memAddr = (ushort)(_bl + (_bh << 4) + (imm << 8));
                    WriteMem(_memAddr + (_memBank << 12), _acc);
                    break;

                case 0x06:
                    // LDB
                    if ((imm & 0x1) == 0)
                        _bl = _acc;
                    else
                        _bh = _acc;
                    break;

                case 0x07:
                    // ALU operations
                    ExecAlu(imm);
                    break;

                case 0x08:
                    // jump operations
                    ExecJump(imm);
                    break;

                case 0x09:
                    // STC
                    _carryFlag = (imm & 0x1) == 1;
                    break;

                case 0x0A:
                    // BNK
                    _memBank = imm;
                    break;

                case 0x0B:
                    // CMP
                    {
                        var result = _acc - _bl - (_carryFlag ? 1 : 0);
                        _carryFlag = result < 0;
                        _zeroFlag = result == 0;
                    }
                    break;

                case 0x0C:
                    // OUT
                    PortAddress = imm;
                    PortValue = _acc;
                    OnPortChanged(PortAddress, PortValue);
                    break;

                case 0x0D:
                    // IN
                    _acc = _ports[imm];
                    break;

                case 0x0E:
                    // HLT
                    _pc--;
                    break;
            }
        }

        private void OnPortChanged(int address, int value)
        {
            PortWrite?.Invoke(this, new PortWriteEventArgs(address, value));
        }

        private void ExecJump(byte subcode)
        {
            var dest = (ushort)_rom[_pc];
            IncPc();
            dest += (ushort)(_rom[_pc] << 8);
            IncPc();
            switch (subcode)
            {
                case 0:
                    // JMP
                    _pc = dest;
                    break;
                case 1:
                    // JA
                    if (!_carryFlag && !_zeroFlag)
                        _pc = dest;
                    break;
                case 2:
                    // JB
                    if (_carryFlag)
                        _pc = dest;
                    break;
                case 3:
                    // JZ
                    if (_zeroFlag)
                        _pc = dest;
                    break;
            }
        }

        private void ExecAlu(byte subcode)
        {
            switch (subcode)
            {
                case 0:
                    // NOT
                    _acc = (byte)((~_acc) & 0x0F);
                    _zeroFlag = _acc == 0;
                    break;

                case 1:
                    // ADD
                    {
                        var result = _acc + _bl + (_carryFlag ? 1 : 0);
                        _carryFlag = result > 0x0F;
                        _acc = (byte)(result & 0x0F);
                        _zeroFlag = _acc == 0;
                    }
                    break;

                case 2:
                    // SUB
                    {
                        var result = _acc - _bl - (_carryFlag ? 1 : 0);
                        _carryFlag = result < 0;
                        _acc = (byte)(result & 0x0F);
                        _zeroFlag = _acc == 0;
                    }
                    break;

                case 3:
                    // AND
                    _acc = (byte)(_acc & _bl);
                    _zeroFlag = _acc == 0;
                    break;

                case 4:
                    // OR
                    _acc = (byte)(_acc | _bl);
                    _zeroFlag = _acc == 0;
                    break;

                case 5:
                    // SHL
                    {
                        var result = (byte)(((_acc << 1) + (_carryFlag ? 1 : 0)) & 0x0F);
                        _carryFlag = (_acc & 0x08) > 0;
                        _acc = result;
                        _zeroFlag = _acc == 0;
                    }
                    break;

                case 6:
                    // SHR
                    {
                        var result = (byte)(((_acc >> 1) + (_carryFlag ? 8 : 0)) & 0x0F);
                        _carryFlag = (_acc & 0x01) > 0;
                        _acc = result;
                        _zeroFlag = _acc == 0;
                    }
                    break;

                case 7:
                    // XOR
                    _acc = (byte)(_acc ^ _bl);
                    _zeroFlag = _acc == 0;
                    break;
            }
        }

        private void GetInstruction(out byte instr, out byte imm)
        {
            var value = _rom[_pc];
            instr = (byte)(value & 0x0F);
            imm = (byte)((value & 0xF0) >> 4);
        }
    }
}
