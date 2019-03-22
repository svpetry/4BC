var
  line0: byte[20], line1: byte[20], line2: byte[20], line3: byte[20],
  lineNo: nibble, currWindowLine: nibble, windowOffset: nibble;

// -----------------------------------------------------------------------------

procedure InitScrollWindow;
var
  i: byte;
begin
  call ClearScreen;

  currWindowLine := 0;
  windowOffset := 0;
  i := 0;
  while i < 20 do
  begin
    line0[i] := ' ';
    line1[i] := ' ';
    line2[i] := ' ';
    line3[i] := ' ';
    inc i;
  end;
end;

// -----------------------------------------------------------------------------

procedure PrintLinePos;
begin
  col := 0;
  if lineNo = 0 then
    str := line0
  else if lineNo = 1 then
    str := line1
  else if lineNo = 2 then
    str := line2
  else
    str := line3;
  printlen := 20;
  call PrintStringPos;
end;

// -----------------------------------------------------------------------------

procedure SetWindowLine;
begin
  if lineNo = 0 then
    line0 := str
  else if lineNo = 1 then
    line1 := str
  else if lineNo = 2 then
    line2 := str
  else
    line3 := str;
end;

// -----------------------------------------------------------------------------

procedure PrintWindowLine;
var
  i: nibble;

begin
  if currWindowLine < 4 then
  begin
    lineNo := (windowOffset + currWindowLine) & %0011;
    call SetWindowLine;
    col := 0;
    row := currWindowLine;
    printlen := 20;
    call PrintStringPos;
    inc currWindowLine;
  end
  else begin
    windowOffset := (windowOffset + 1) & %0011;
    lineNo := (windowOffset + 3) & %0011;
    call SetWindowLine;

    i := 0;
    while i < 4 do
    begin
      lineNo := (windowOffset + i) & %0011;
      row := i;
      call PrintLinePos;
      inc i;
    end;
  end;
end;

// -----------------------------------------------------------------------------
