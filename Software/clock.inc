var
  sec: byte, min: byte, hour: byte, adv_day: nibble,
  day: byte, month: byte, year: word,
  weekday: nibble;

// -----------------------------------------------------------------------------

procedure AdvanceTime;
begin
  inc sec;
  if sec = 60 then
  begin
    sec := 0;
    inc min;
  end;
  if min = 60 then
  begin
    min := 0;
    inc hour;
  end;
  if hour = 24 then
  begin
    hour := 0;
    adv_day := 1;
  end;
end;

// -----------------------------------------------------------------------------

procedure DisplayTime;
begin
  col := 6;
  row := 0;
  call SetPos;

  numByte := hour;
  call ByteToStr;
  printlen := 2;
  call PrintString;

  str[0] := ':';
  printlen := 1;
  call PrintString;

  numByte := min;
  call ByteToStr;
  printlen := 2;
  call PrintString;

  str[0] := ':';
  printlen := 1;
  call PrintString;

  numByte := sec;
  call ByteToStr;
  printlen := 2;
  call PrintString;
end;

// -----------------------------------------------------------------------------

procedure AdvanceDate;
begin
  inc day;
  
  if month = 4 or month = 6 or month = 9 or month = 11 then
  begin
    if day > 30 then
    begin
      day := 1;
      inc month;
    end;
  end 
  else if month = 2 then
  begin
    if year & %00000011 = 0 then
    begin
      if day > 29 then
      begin
        day := 1;
        inc month;
      end;
    end
    else begin
      if day > 28 then
      begin
        day := 1;
        inc month;
      end;
    end;
  end
  else begin
    if day > 31 then
    begin
      day := 1;
      inc month;
    end;
  end;

  if month > 12 then
  begin
    inc year;
    month := 1;
  end;
end;

// -----------------------------------------------------------------------------

procedure DisplayDate;
begin
  if weekday = 0 then
    str := 'Sonntag   '
  else if weekday = 1 then
    str := 'Montag    '
  else if weekday = 2 then
    str := 'Dienstag  '
  else if weekday = 3 then
    str := 'Mittwoch  '
  else if weekday = 4 then
    str := 'Donnerstag'
  else if weekday = 5 then
    str := 'Freitag   '
  else if weekday = 6 then
    str := 'Samstag   ';

  // print weekday
  col := 5;
  row := 1;
  printlen := 11;
  call PrintStringPos;  

  col := 5;
  row := 2;

  // print date ##.##.####
  numByte := day;
  call ByteToStr;
  printlen := 2;
  call PrintStringPos;  

  str[0] := '.';
  printlen := 1;
  call PrintString;

  numByte := month;
  call ByteToStr;
  printlen := 2;
  call PrintString;

  str[0] := '.';
  printlen := 1;
  call PrintString;

  numWord := year;
  call WordToStr;
  printlen := 4;
  call PrintString;
end;

// -----------------------------------------------------------------------------

procedure CalcWeekday;
var
  c: word, e: nibble, f:nibble, g:word, i: nibble;

begin
  if month <= 2 then
  begin
    c := (year - 1) / 100;
    g := year - 1 - 100 * c;
  end
  else begin
    c := year / 100;
    g := year - 100 * c;
  end;

  if month = 1 then
    e := 0
  else if month = 2 then
    e := 3
  else if month = 3 then
    e := 2
  else if month = 4 then
    e := 5
  else if month = 5 then
    e := 0
  else if month = 6 then
    e := 3
  else if month = 7 then
    e := 5
  else if month = 8 then
    e := 1
  else if month = 9 then
    e := 4
  else if month = 10 then
    e := 6
  else if month = 11 then
    e := 2
  else if month = 12 then
    e := 4;

  i := c % 4;
  if i = 0 then
    f := 0
  else if i = 1 then
    f := 5
  else if i = 2 then
    f := 3
  else if i = 3 then
    f := 1;

  weekday := (day + e + f + g + g / 4) % 7;
end;

// -----------------------------------------------------------------------------

procedure WaitForNextSecond;
var
  p5: nibble;

begin
  inp p5, 5;
  while p5 & %1000 > 0 do
    inp p5, 5;
  while p5 & %1000 = 0 do
    inp p5, 5;
end;

// -----------------------------------------------------------------------------

procedure SetTime;
begin
  sec := 0;
  call DisplayTime;

  asdf

end;

// -----------------------------------------------------------------------------

procedure RunClock;
begin
  call ClearScreen;

  year := 2018;
  month := 12;
  day := 01;

  hour := 12;
  min := 0;
  sec := 0;    

  adv_day := 0;

  call CalcWeekday;
  call DisplayDate;

  while 0 = 0 do
  begin
    call WaitForNextSecond;
    call AdvanceTime;
    call DisplayTime;

    if adv_day > 0 then
    begin
      adv_day := 0;
      call AdvanceDate;
      call CalcWeekday;
      call DisplayDate;
    end;

    call ReadKey;

    if key = 10 then
      call SetTime;

  end;
end;

// -----------------------------------------------------------------------------
