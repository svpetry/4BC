var
  scancode: byte,
  key: byte;

// -----------------------------------------------------------------------------

procedure ReadScancode;
var
  a: nibble,
  j: nibble,
  i: byte;

begin
  // port 4.0: kb_data (write: 1 = pull down)
  // port 4.1: kb_clock (write: 1 = pull down)

  outp 4, 0; // release clock line
  
  asm
  ; wait 50 µs
    LDA 01h
    LDBL
    STC 0
    LDA 08h
@SC_wait1:
    SUB
    JZ @SC_exitwait1
    JMP @SC_wait1
@SC_exitwait1:
  end;

  scancode := 0;

  asm
; read start bit
    LDA 0
    STA [(i)]
    STA [(i + 1)]
@SC_loop1:
    LDA 02h
    LDBL
    IN 4
    AND
    JZ @SC_loop1exit
    STC 1
    LDA [(i)]
    ADD
    STA [(i)]
    LDA [(i + 1)]
    ADD
    STA [(i + 1)]

    LDBL
    LDA 0Ch
    CMP
    JZ @SC_exit

    JMP @SC_loop1
@SC_loop1exit:

    LDA 02h
    LDBL
@SC_loop2:
    IN 4
    AND
    JZ @SC_loop2

; read data bits
    LDA 0
    STA [(j)]
@SC_loop3:

; wait until clock line low
    LDA 02h
    LDBL
@SC_loop4:
    IN 4
    AND
    JZ @SC_loop4exit
    JMP @SC_loop4
@SC_loop4exit:

; read next data bit
    LDA 1
    LDBL
    IN 4
    AND
    JZ @SC_zero
    STC 1
    JMP @SC_continue
@SC_zero:
    STC 0
@SC_continue:

    LDA [(scancode + 1)]
    SHR
    STA [(scancode + 1)]
    LDA [(scancode)]
    SHR
    STA [(scancode)]

    STC 0
    LDA [(j)]
    ADD
    STA [(j)]

    LDA 08h
    LDBL
    LDA [(j)]
    CMP
    JZ @SC_loop3exit

; wait until clock line high
    LDA 02h
    LDBL
@SC_loop5:
    IN 4
    AND
    JZ @SC_loop5


    JMP @SC_loop3
@SC_loop3exit:

; read parity bit
    LDA 02h
    LDBL
@SC_loop6:
    IN 4
    AND
    JZ @SC_loop7
    JMP @SC_loop6
@SC_loop7:
    IN 4
    AND
    JZ @SC_loop7

; read stop bit
@SC_loop8:
    IN 4
    AND
    JZ @SC_loop9
    JMP @SC_loop8
@SC_loop9:
    IN 4
    AND
    JZ @SC_loop9

@SC_exit:
  end;

  outp 4, 2; // pull clock line to 0
  call Delay50us;
end;

// -----------------------------------------------------------------------------

procedure ReadKey;
begin
  call ReadScancode;
  if scancode # 0 then
  begin
    if scancode = $1C then key := 'A'
    else if scancode = $32 then key := 'B'
    else if scancode = $21 then key := 'C'
    else if scancode = $23 then key := 'D'
    else if scancode = $24 then key := 'E'
    else if scancode = $2B then key := 'F'
    else if scancode = $34 then key := 'G'
    else if scancode = $33 then key := 'H'
    else if scancode = $43 then key := 'I'
    else if scancode = $3B then key := 'J'
    else if scancode = $42 then key := 'K'
    else if scancode = $4B then key := 'L'
    else if scancode = $3A then key := 'M'
    else if scancode = $31 then key := 'N'
    else if scancode = $44 then key := 'O'
    else if scancode = $4D then key := 'P'
    else if scancode = $15 then key := 'Q'
    else if scancode = $2D then key := 'R'
    else if scancode = $1B then key := 'S'
    else if scancode = $2C then key := 'T'
    else if scancode = $3C then key := 'U'
    else if scancode = $2A then key := 'V'
    else if scancode = $1D then key := 'W'
    else if scancode = $22 then key := 'X'
    else if scancode = $1A then key := 'Y'
    else if scancode = $35 then key := 'Z'
    else if scancode = $45 then key := '0'
    else if scancode = $16 then key := '1'
    else if scancode = $1E then key := '2'
    else if scancode = $26 then key := '3'
    else if scancode = $25 then key := '4'
    else if scancode = $2E then key := '5'
    else if scancode = $36 then key := '6'
    else if scancode = $3D then key := '7'
    else if scancode = $3E then key := '8'
    else if scancode = $46 then key := '9'
    else if scancode = $29 then key := ' ' // space
    else if scancode = $0D then key := 9 // TAB
    else if scancode = $5A then key := 10 // enter
    else if scancode = $66 then key := 8 // backspace
    else if scancode = $76 then key := 27 // ESC
    else
      key := 0;

    while scancode > 0 do
      call ReadScancode;
  end;
end;

// -----------------------------------------------------------------------------

procedure WaitForKey;
begin
  key := 0;
  while key = 0 do
    call ReadKey;
end;

// -----------------------------------------------------------------------------
