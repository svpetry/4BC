// -----------------------------------------------------------------------------

procedure CheckSpeed;
var
  p5: nibble,
  kips: word;

begin
  kips := 0;

  inp p5, 5;
  while p5 & %1000 > 0 do
    inp p5, 5;
  inp p5, 5;
  while p5 & %1000 = 0 do
    inp p5, 5;

  asm
@loop:
      LDA 0
      LDBL
      STC 1
      LDA [(kips)]
      ADD
      STA [(kips)]
      LDA [(kips + 1)]
      ADD
      STA [(kips + 1)]
      LDA [(kips + 2)]
      ADD
      STA [(kips + 2)]
      LDA [(kips + 3)]
      ADD
      STA [(kips + 3)]

      LDA 08h
      LDBL
      IN 5
      AND
      JZ @endloop
      NOP
      NOP
      NOP
      NOP
      JMP @loop
@endloop:
  end;

  col := 0;

  str := '0';
  printlen := 1;
  call PrintStringPos;

  kips := kips / 20;

  numWord := kips;
  call WordToStr;

  str[0] := '.';
  printlen := 4;
  call PrintString;

  printlen := 5;
  str := ' MIPS';
  call PrintString;
end;

// -----------------------------------------------------------------------------

procedure CheckMemory;
var
  mem: byte;

begin
  mem := 8;
  asm
    BNK 4
    LDA 05h
    LDBL
    STA [0]
    LDA 0Ah
    LDA [0]
    CMP
    BNK 0
    JA @CM_error
    JB @CM_error
  end;
  mem := 16;
  asm
    BNK 8
    LDA 05h
    LDBL
    STA [0]
    LDA 0Ah
    LDA [0]
    CMP
    BNK 0
    JA @CM_error
    JB @CM_error
  end;
  mem := 24;
  asm
    BNK 12
    LDA 05h
    LDBL
    STA [0]
    LDA 0Ah
    LDA [0]
    CMP
    BNK 0
    JA @CM_error
    JB @CM_error
  end;
  mem := 32;

  asm
@CM_error:
  end;

  numByte := mem;
  call ByteToStr;

  col := 0;
  printlen := 2;
  call PrintStringPos;

  col := 3;
  str := 'KB RAM';
  printlen := 6;
  call PrintStringPos;
end;

// -----------------------------------------------------------------------------

procedure CheckTimer;
var  
  a: word,
  p5: nibble;

begin
  col := 0;
  str := 'checking timer...';
  printlen := 17;
  call PrintStringPos;
  
  a := 50000;
  inp p5, 5;
  while p5 & %1000 > 0 and a > 0 do
  begin
    inp p5, 5;
    dec a;
  end;
  while p5 & %1000 = 0 and a > 0 do
  begin
    inp p5, 5;
    dec a;
  end;

  if a > 0 then
    str := 'timer OK         '
  else
    str := 'timer ERROR      ';
  col := 0;
  call PrintStringPos;
end;

// -----------------------------------------------------------------------------

procedure ShowBootScreen;
var
  i: word, j: nibble;
  
begin
  call FadeIn;
  
  row := 0;
  str := '4BC system startup  ';
  call ScrollLineIntoView;
  
  row := 1;
  call CheckTimer;

  row := 2;
  call CheckMemory;

  row := 3;
  call CheckSpeed;

  // wait a moment
  i := 10000;
  while i > 0 do
  begin
    j := 10;
    while j > 0 do
      dec j;
    dec i;
  end;

  call ClearScreen;

end;