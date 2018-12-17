var
  ch: byte,
  cmd: byte,
  str: byte[20], 
  printlen: byte,
  row: nibble, col: byte;

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
begin
  // %(light)(E)(RW)(RS)
  outp 3, hi(cmd);
  outp 2, %1100;
  outp 2, %1000;

  outp 3, lo(cmd);
  outp 2, %1100;
  outp 2, %1000;

  call LcdDelayShort;
end;

// -----------------------------------------------------------------------------

procedure InitDisplay;
begin
  call LcdDelayLong;
  call LcdDelayLong;
  call LcdDelayLong;

  // %(light)(E)(RW)(RS)
  outp 3, %0011;

  outp 2, %1100;
  outp 2, %1000;
  call LcdDelayLong;

  outp 2, %1100;
  outp 2, %1000;
  call LcdDelayLong;

  outp 2, %1100;
  outp 2, %1000;
  call LcdDelayLong;

  outp 3, %0010; // init 4 bit mode

  outp 2, %1100;
  outp 2, %1000;
  call LcdDelayLong;

  cmd := %00000001;
  call LcdCmd;
  cmd := %00000110;
  call LcdCmd;
  cmd := %00001100;
  call LcdCmd;
  cmd := %00101000;
  call LcdCmd;
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
        dec j;

      outp 2, %1000;

      j := 15 - rel;
      while j > 0 do
        dec j;

      dec i;
    end;

    dec rel;
  end;
end;

