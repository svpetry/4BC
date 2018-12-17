namespace CpuEmu.Emulator
{
    static class Disassembler
    {
        public static string Disassemble(byte[] data)
        {
            var instr = data[0] & 0x0F;
            var imm = data[0] >> 4;

            switch (instr)
            {
                case 0x00:
                    // NOP
                    return "NOP";

                case 0x01:
                    // LDA mem abs
                    {
                        var addr = data[1] + (imm << 8);
                        return $"LDA [0{addr.ToString("X3")}h]";
                    }

                case 0x02:
                    // LDA mem ind
                    {
                        var addr = imm << 8;
                        return $"LDA [B][0{addr.ToString("X3")}h]";
                    }

                case 0x03:
                    // LDA imm
                    return $"LDA 0{imm.ToString("X1")}h";

                case 0x04:
                    // STA mem abs
                    {
                        var addr = data[1] + (imm << 8);
                        return $"STA [0{addr.ToString("X3")}h]";
                    }

                case 0x05:
                    // STA mem ind
                    {
                        var addr = imm << 8;
                        return $"STA [B][0{addr.ToString("X3")}h]";
                    }

                case 0x06:
                    // LDBL/LDBH
                    if ((imm & 0x1) == 0)
                        return "LDBL";
                    else
                        return "LDBH";

                case 0x07:
                    // ALU operations
                    return ExecAlu(imm);

                case 0x08:
                    // jump operations
                    return ExecJump(imm, data);

                case 0x09:
                    // STC
                    return $"STC {imm & 1}";

                case 0x0A:
                    // BNK
                    return $"STC 0{imm.ToString("X1")}h";

                case 0x0B:
                    // CMP
                    return "CMP";

                case 0x0C:
                    // OUT
                    return $"OUT 0{imm.ToString("X1")}h";

                case 0x0D:
                    // IN
                    return $"IN 0{imm.ToString("X1")}h";

                case 0x0E:
                    // HLT
                    return "HLT";
            }

            return "???";
        }

        private static string ExecJump(int subcode, byte[] data)
        {
            var dest = data[1] + (data[2] << 8);
            var instr = "";
            switch (subcode)
            {
                case 0:
                    // JMP
                    instr = "JMP";
                    break;
                case 1:
                    // JA
                    instr = "JA";
                    break;
                case 2:
                    // JB
                    instr = "JB";
                    break;
                case 3:
                    // JZ
                    instr = "JZ";
                    break;
                default:
                    instr = "???";
                    break;
            }
            return $"{instr} 0{dest.ToString("X4")}h";
        }

        private static string ExecAlu(int subcode)
        {
            switch (subcode)
            {
                case 0:
                    return "NOT";

                case 1:
                    return "ADD";

                case 2:
                    return "SUB";

                case 3:
                    return "AND";

                case 4:
                    return "OR";

                case 5:
                    return "SHR";

                case 6:
                    return "SHL";

                case 7:
                    return "XOR";

                default:
                    return "???";
            }
        }
    }
}
