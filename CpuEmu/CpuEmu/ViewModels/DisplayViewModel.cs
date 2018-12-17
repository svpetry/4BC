using System.Text;

namespace CpuEmu.ViewModels
{
    enum DisplayMode
    {
        Mode8bit,
        Mode4bit
    }

    class DisplayViewModel : BaseViewModel
    {
        private DisplayMode _displayMode = DisplayMode.Mode8bit;

        private byte[] _ddram = new byte[0x80];

        private string _displayRow0;
        private string _displayRow1;
        private string _displayRow2;
        private string _displayRow3;
        private int _ramAddr;
        private byte _data;
        private byte _cmd;
        private bool _rs;
        private bool _rw;

        private int _byteNo;
        private byte _fullData;

        public DisplayViewModel()
        {
        }

        public string DisplayRow0
        {
            get => _displayRow0;
            set
            {
                if (_displayRow0 == value) return;
                _displayRow0 = value;
                OnPropertyChanged();
            }
        }

        public string DisplayRow1
        {
            get => _displayRow1;
            set
            {
                if (_displayRow1 == value) return;
                _displayRow1 = value;
                OnPropertyChanged();
            }
        }

        public string DisplayRow2
        {
            get => _displayRow2;
            set
            {
                if (_displayRow2 == value) return;
                _displayRow2 = value;
                OnPropertyChanged();
            }
        }

        public string DisplayRow3
        {
            get => _displayRow3;
            set
            {
                if (_displayRow3 == value) return;
                _displayRow3 = value;
                OnPropertyChanged();
            }
        }

        public void SetCommand(byte cmd)
        {
            var rs = (cmd & 0b0001) > 0;
            var rw = (cmd & 0b0010) > 0;
            var en = (cmd & 0b0100) > 0;

            if (en && (_cmd & 0x04) == 0)
            {
                // enable lo -> hi
                _rs = rs;
                _rw = rw;
            }
            else if (!en && (_cmd & 0x04) > 0)
            {
                // enable hi -> lo
                if (_displayMode == DisplayMode.Mode8bit)
                {
                    if (_data == 0b0010)
                        _displayMode = DisplayMode.Mode4bit;
                }
                else
                {
                    if (_byteNo == 0)
                        _fullData = (byte)(_data << 4);
                    else
                    {
                        _fullData = (byte)(_fullData | _data);
                        ExecCommand(_fullData);
                    }
                    _byteNo = 1 - _byteNo;
                }
            }

            _cmd = cmd;
        }

        public void SetData(byte data)
        {
            _data = data;
        }

        private void ExecCommand(byte data)
        {
            if (_rs && !_rw)
            {
                // write data to text buffer
                _ddram[_ramAddr++] = data;

                UpdateDisplay();
            }
            else if (!_rs && !_rw)
            {
                // set control register
                if ((data & 0b11111110) == 0b00000010)
                {
                    // return home
                    _ramAddr = 0;
                }
                else if ((data & 0b10000000) == 0b10000000)
                {
                    // set DDRAM address
                    _ramAddr = data & 0b01111111;
                }
                else if (data == 0b00000001)
                {
                    // clear screen
                    for (var i = 0; i < _ddram.Length; i++)
                        _ddram[i] = (byte)' ';

                    UpdateDisplay();
                }
            }
        }

        private string ConvertText(int startPos, int count)
        {
            var sb = new StringBuilder();
            for (var idx = 0; idx < count; idx++)
                sb.Append((char)_ddram[idx + startPos]);
            return sb.ToString();
        }

        private void UpdateDisplay()
        {
            DisplayRow0 = ConvertText(0x00, 20);
            DisplayRow1 = ConvertText(0x40, 20);
            DisplayRow2 = ConvertText(0x14, 20);
            DisplayRow3 = ConvertText(0x54, 20);
        }
    }
}
