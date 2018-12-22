var
  numByte: byte, numWord: word;

// -----------------------------------------------------------------------------

procedure ByteToStr;
var
  a: byte;
begin
  a := numByte / 10;
  str[0] := '0' + a;
  str[1] := '0' + numByte - a * 10;
end;

// -----------------------------------------------------------------------------

procedure WordToStr;
var
  a: word;
begin
  str := '0000';

  a := numWord / 1000;
  if a > 0 then
    str[0] := '0' + a;
  numWord := numWord - a * 1000;
  
  a := numWord / 100;
  if a > 0 then
    str[1] := '0' + a;
  numWord := numWord - a * 100;

  a := numWord / 10;
  if a > 0 then
    str[2] := '0' + a;
  str[3] := '0' + numWord - a * 10;
end;

// -----------------------------------------------------------------------------

procedure Delay10ms;
var
  i: word;
begin
  i := 230;
  while i > 0 do // 32 instructions per loop = 42 µs
    dec i;
end;

// -----------------------------------------------------------------------------
// 1.3 µs per instruction

procedure Delay50us;
begin
  asm 
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop

    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
  end;
end;

// -----------------------------------------------------------------------------
