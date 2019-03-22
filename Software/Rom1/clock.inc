var
  sec: byte, min: byte, hour: byte, adv_day: nibble,
  day: byte, month: byte, year: word,
  weekday: nibble,
  numkey: byte;

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
  row := 1;
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

procedure DisplayLargeTimePoints;
begin
  ch := 'o';
  row := 0;
  call PrintCharPos;
  row := 1;
  call PrintCharPos;
end;

// -----------------------------------------------------------------------------

procedure DisplayLargeTime;
var
  v: byte;

begin
  // hours
  v := hour / 10;
  col := 0;
  digit := v;
  call PrintLargeDigit;

  col := 3;
  digit := hour - v * 10;
  call PrintLargeDigit;

  // points
  col := 6;
  call DisplayLargeTimePoints;

  // minutes
  v := min / 10;
  col := 7;
  digit := v;
  call PrintLargeDigit;

  col := 10;
  digit := min - v * 10;
  call PrintLargeDigit;

  // points
  col := 13;
  call DisplayLargeTimePoints;

  // seconds
  v := sec / 10;
  col := 14;
  digit := v;
  call PrintLargeDigit;

  col := 17;
  digit := sec - v * 10;
  call PrintLargeDigit;
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
  // generate weekday string
  if weekday = 0 then
    str := 'Sunday   '
  else if weekday = 1 then
    str := 'Monday   '
  else if weekday = 2 then
    str := 'Tuesday  '
  else if weekday = 3 then
    str := 'Wednesday'
  else if weekday = 4 then
    str := 'Thursday '
  else if weekday = 5 then
    str := 'Friday   '
  else if weekday = 6 then
    str := 'Saturday ';

  // print weekday
  col := 0;
  row := 3;
  printlen := 9;
  call PrintStringPos;  

  // print date ##.##.####
  col := 10;
  row := 3;
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

procedure ReadNumkey;
begin
  key := 0;
  while key < '0' or key > '9' do
    call WaitForKey;
  numkey := key - '0';
  outp 0, numkey;
end;

// -----------------------------------------------------------------------------

procedure SetTime;
begin
  call ClearScreen;
  sec := 0;
  call DisplayTime;
  call DisplayDate;

  row := 1;
  col := 6;
  call SetPos;
  call EnableCursor;

  // hours
  hour := 0;
  call ReadNumkey;
  hour := numkey * 10;
  ch := key;
  call PrintChar;

  call ReadNumkey;
  hour := hour + numkey;
  ch := key;
  call PrintChar;

  if hour > 23 then
    hour := 23;

  // minutes
  col := 9;
  call SetPos;
  min := 0;
  call ReadNumkey;
  min := numkey * 10;
  ch := key;
  call PrintChar;

  call ReadNumkey;
  min := min + numkey;
  ch := key;
  call PrintChar;
  
  if min > 59 then
    min := 59;

  // day
  row := 3;
  col := 10;
  call SetPos;

  day := 0;
  call ReadNumkey;
  day := numkey * 10;
  ch := key;
  call PrintChar;

  call ReadNumkey;
  day := day + numkey;
  ch := key;
  call PrintChar;

  if day > 31 then
    day := 31;

  // month
  col := 13;
  call SetPos;

  month := 0;
  call ReadNumkey;
  month := numkey * 10;
  ch := key;
  call PrintChar;

  call ReadNumkey;
  month := month + numkey;
  ch := key;
  call PrintChar;

  if month > 1 then
    month := 12;

  // year
  col := 18;
  call SetPos;

  year := 2000;

  call ReadNumkey;
  year := year + numkey * 10;
  ch := key;
  call PrintChar;

  call ReadNumkey;
  year := year + numkey;
  ch := key;
  call PrintChar;

  call DisableCursor;
  call ClearScreen;

  call DisplayLargeTime;
  call CalcWeekday;
  call DisplayDate;
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
    call DisplayLargeTime;

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
