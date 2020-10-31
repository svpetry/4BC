var
  ch: byte,
  cmd: byte,
  str: byte[20], 
  chardef: byte[8],
  printlen: byte,
  row: nibble, col: byte,
  light: nibble,
  digit: nibble,
  digdef: byte[9];

// -----------------------------------------------------------------------------

procedure LcdDelayShort;
var
  i: byte;

begin
  i := 20;
  while i > 0 do
    i := i - 1;
end;

// -----------------------------------------------------------------------------

procedure LcdDelayLong;
var
  i: word;

begin
  i := 500;
  while i > 0 do
    i := i - 1;
end;

// -----------------------------------------------------------------------------

procedure LcdCmd;
var
  l: nibble;

begin
  if light # 0 then
    l := %1000
  else
    l := 0;

  // %(light)(E)(RW)(RS)
  outp 3, hi(cmd);
  outp 2, %0100 | l;
  outp 2, %0000 | l;

  outp 3, lo(cmd);
  outp 2, %0100 | l;
  outp 2, %0000 | l;

  call LcdDelayShort;
end;

// -----------------------------------------------------------------------------

procedure InitDisplay;
begin
  outp 3, 0;
  light := 0;

  call LcdDelayLong;
  call LcdDelayLong;
  call LcdDelayLong;

  // %(light)(E)(RW)(RS)
  outp 3, %0011;

  outp 2, %0100;
  outp 2, %0000;
  call LcdDelayLong;

  outp 2, %0100;
  outp 2, %0000;
  call LcdDelayLong;

  outp 2, %0100;
  outp 2, %0000;
  call LcdDelayLong;

  outp 3, %0010; // init 4 bit mode

  outp 2, %0100;
  outp 2, %0000;
  call LcdDelayLong;

  cmd := %00000001; // clear display, cursor to 0
  call LcdCmd;
  cmd := %00000110; // cursor direction right, shift off
  call LcdCmd;
  cmd := %00001100; // display on, cursor off
  call LcdCmd;
  cmd := %00101000; // 4 bit interface, two rows 
  call LcdCmd;
end;

// -----------------------------------------------------------------------------

procedure ClearScreen;
begin
  cmd := %00000001;
  call LcdCmd;
  call LcdDelayLong;
end;

// -----------------------------------------------------------------------------

procedure PrintChar;
begin
  outp 3, hi(ch);
  outp 2, %1101;
  outp 2, %1001;

  outp 3, lo(ch);
  outp 2, %1101;
  outp 2, %1001;

  call LcdDelayShort;
end;

// -----------------------------------------------------------------------------

procedure SetPos;
begin
  if row = 0 then
    cmd := $00
  else if row = 1 then
    cmd := $40
  else if row = 2 then
    cmd := $14
  else if row = 3 then
    cmd := $54;
  cmd := (cmd + col) | %10000000;

  call LcdCmd;
end;

// -----------------------------------------------------------------------------

procedure EnableCursor;
begin
  cmd := %00001111;
  call LcdCmd;
end;

// -----------------------------------------------------------------------------

procedure DisableCursor;
begin
  cmd := %00001100;
  call LcdCmd;
end;

// -----------------------------------------------------------------------------

procedure PrintString;
var
  i: byte;
begin
  i := 0;
  while i < printlen do
  begin
    ch := str[i];
    call PrintChar;
    inc i;
  end;
end;

// -----------------------------------------------------------------------------

procedure PrintStringPos;
begin
  call SetPos;
  call PrintString;
end;

// -----------------------------------------------------------------------------

procedure PrintCharPos;
begin
  call SetPos;
  call PrintChar;
end;

// -----------------------------------------------------------------------------

procedure FadeIn;
var
  rel: nibble,
  i: byte, j: nibble;

begin
  rel := 15;

  while rel > 0 do
  begin

    i := 200;
    while i > 0 do
    begin
      outp 2, %0000;
      
      j := rel;
      while j > 0 do
      begin
        dec j;
        asm
          nop
          nop
          nop
          nop
          nop
        end;
      end;

      outp 2, %1000;

      j := 15 - rel;
      while j > 0 do
      begin
        dec j;
        asm
          nop
          nop
          nop
          nop
          nop
        end;
      end;

      dec i;
    end;

    dec rel;
  end;

  light := 1;
end;

// -----------------------------------------------------------------------------

procedure ScrollLineIntoView;
var
  i: byte, j: word;

begin
  i := 20;
  while i > 0 do
  begin
    dec i;

    printlen := 20 - i;
    col := i;
    call SetPos;
    call PrintString;

    j := 500;
    while j > 0 do
      dec j;
  end;
end;

// -----------------------------------------------------------------------------

procedure LoadCustomChar;
var
  i: nibble;

begin
  i := 0;
  while i < 8 do
  begin
    ch := chardef[i];
    call PrintChar;
    inc i;
  end;
end;

// -----------------------------------------------------------------------------

procedure LoadCustomDigits;
begin
  // no idea why this is neccessary
  cmd := $80;
  call LcdCmd;

  // set CGRAM address
  cmd := $40;
  call LcdCmd;

  chardef := (0, 0, 0, 0, 0, 0, 0, 0);
  call LoadCustomChar;
  chardef := (3, 7, 15, 31, 0, 0, 0, 0);
  call LoadCustomChar;
  chardef := (24, 28, 30, 31, 0, 0, 0, 0);
  call LoadCustomChar;
  chardef := (31, 31, 31, 31, 0, 0, 0, 0);
  call LoadCustomChar;
  chardef := (31, 30, 28, 24, 0, 0, 0, 0);
  call LoadCustomChar;
  chardef := (31, 15,7, 3, 0, 0, 0, 0);
  call LoadCustomChar;
  chardef := (3, 7, 15, 31, 31, 31, 31, 31);
  call LoadCustomChar;
  chardef := (24, 28, 30, 31, 31, 31, 31);
  call LoadCustomChar;
end;

// -----------------------------------------------------------------------------

procedure PrintLargeDigit;
var
  pcol: byte,
  i: nibble,
  pos: nibble;

begin
  if digit = 0 then
    digdef := (6, 3, 7, 255, 32, 255, 5, 3, 4)
  else if digit = 1 then
    digdef := (1, 255, 32, 32, 255, 32, 3, 3, 3)
  else if digit = 2 then
    digdef := (1, 3, 7, 6, 3, 4, 5, 3, 4)
  else if digit = 3 then
    digdef := (1, 3, 7, 32, 3, 255, 5, 3, 4)
  else if digit = 4 then
    digdef := (255, 32, 255, 5, 3, 255, 32, 32, 3)
  else if digit = 5 then
    digdef := (255, 3, 4, 3, 3, 7, 5, 3, 4)
  else if digit = 6 then
    digdef := (6, 3, 2, 255, 3, 7 ,5, 3, 4)
  else if digit = 7 then
    digdef := (5, 3, 255, 32, 6, 4, 32, 3, 32)
  else if digit = 8 then
    digdef := (6, 3, 7, 255, 3, 255, 5, 3, 4)
  else if digit = 9 then
    digdef := (6, 3, 7, 5, 3, 255, 5, 3, 4);

  pcol := col;
  row := 0;
  pos := 0;
  while row < 3 do
  begin
    col := pcol;
    call SetPos;
    
    i := 0;
    while i < 3 do
    begin
      ch := digdef[pos];
      call PrintChar;
      inc i;
      inc pos;
    end;

    inc row;
  end;
end;

// -----------------------------------------------------------------------------
