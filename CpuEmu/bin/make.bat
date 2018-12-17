4BCC.exe %1.pl0
4BCasm.exe %1.asm
del rom.bin
ren %1.bin rom.bin
CpuEmu.exe
