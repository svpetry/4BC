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
