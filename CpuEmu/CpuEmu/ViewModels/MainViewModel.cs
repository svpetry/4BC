using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using CpuEmu.Emulator;

namespace CpuEmu.ViewModels
{
    class MainViewModel : BaseViewModel
    {
        private const string RomFile = "rom.bin";

        private Cpu _cpu;

        private Task _rtcTask;

        private string _pc;
        private string _acc;
        private string _bl;
        private string _bh;
        private string _carry;
        private string _zero;
        private string _instruction;
        private string _port0;
        private string _port1;

        public MainViewModel()
        {
            StartCommand = new ActionCommand(DoStart);
            StopCommand = new ActionCommand(DoStop) { Enabled = false };
            StepCommand = new ActionCommand(DoStep);
            ResetCommand = new ActionCommand(DoReset);

            _cpu = new Cpu();
            _cpu.PortWrite += OnPortWrite;

            if (File.Exists(RomFile))
                _cpu.LoadRom(RomFile);

            DisplayCpuRegisters();
            Port0 = "0";
            Port1 = "0";

            DisplayViewModel = new DisplayViewModel();
            VarInfoViewModel = new VarInfoViewModel(_cpu);
            if (File.Exists("varlist.txt"))
                VarInfoViewModel.ParseVarList("varlist.txt");

            StartRtcTask();
        }

        private void StartRtcTask()
        {
            _rtcTask = Task.Run(() =>
            {
                while (true)
                {
                    _cpu.SetPort(5, 0b0000);
                    Thread.Sleep(500);
                    _cpu.SetPort(5, 0b1000);
                    Thread.Sleep(500);
                }
            });
        }

        private void OnPortWrite(object sender, PortWriteEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                switch (e.Address)
                {
                    case 0:
                        Port0 = e.Value.ToString("X1");
                        break;
                    case 1:
                        Port1 = e.Value.ToString("X1");
                        break;
                    case 2:
                        DisplayViewModel.SetCommand((byte)e.Value);
                        break;
                    case 3:
                        DisplayViewModel.SetData((byte)e.Value);
                        break;
                }
            });
        }

        public string Pc
        {
            get => _pc;
            set
            {
                if (_pc == value) return;
                _pc = value;
                OnPropertyChanged();
            }
        }

        public string Acc
        {
            get => _acc;
            set
            {
                if (_acc == value) return;
                _acc = value;
                OnPropertyChanged();
            }
        }

        public string Bl
        {
            get => _bl;
            set
            {
                if (_bl == value) return;
                _bl = value;
                OnPropertyChanged();
            }
        }

        public string Bh
        {
            get => _bh;
            set
            {
                if (_bh == value) return;
                _bh = value;
                OnPropertyChanged();
            }
        }

        public string Carry
        {
            get => _carry;
            set
            {
                if (_carry == value) return;
                _carry = value;
                OnPropertyChanged();
            }
        }

        public string Zero
        {
            get => _zero;
            set
            {
                if (_zero == value) return;
                _zero = value;
                OnPropertyChanged();
            }
        }

        public string Instruction
        {
            get => _instruction;
            set
            {
                if (_instruction == value) return;
                _instruction = value;
                OnPropertyChanged();
            }
        }

        public string Port0
        {
            get => _port0;
            set
            {
                if (_port0 == value) return;
                _port0 = value;
                OnPropertyChanged();
            }
        }

        public string Port1
        {
            get => _port1;
            set
            {
                if (_port1 == value) return;
                _port1 = value;
                OnPropertyChanged();
            }
        }

        public ActionCommand StartCommand { get; }

        public ActionCommand StopCommand { get; }

        public ActionCommand StepCommand { get; }

        public ActionCommand ResetCommand { get; }

        public DisplayViewModel DisplayViewModel { get; }

        public VarInfoViewModel VarInfoViewModel { get; }

        private async void DoStart(object obj)
        {
            StepCommand.Enabled = false;
            StartCommand.Enabled = false;
            StopCommand.Enabled = true;
            ResetCommand.Enabled = false;

            await Task.Run(() =>
            {
                // 1.0 MIPS
                var stopWatch = new Stopwatch();
                var ticksPer10Ms = Stopwatch.Frequency / 100;
                var instructionsPer10Ms = 1000000 / 100;
                long sleepTicks = 0;

                var updateCount = 0;

                while (Instruction != "HLT")
                {
                    stopWatch.Restart();

                    for (var i = 0; i < instructionsPer10Ms; i++)
                        _cpu.Step();

                    stopWatch.Stop();

                    if (++updateCount == 4)
                    {
                        Dispatcher.CurrentDispatcher.Invoke(() =>
                        {
                            DisplayCpuRegisters();
                            VarInfoViewModel.UpdateVariables();
                        });
                        updateCount = 0;
                    }

                    sleepTicks += ticksPer10Ms - stopWatch.ElapsedTicks;

                    if (sleepTicks > ticksPer10Ms)
                    {
                        sleepTicks -= ticksPer10Ms;
                        Task.Delay(10).Wait();
                    }
                }
            });

            DisplayCpuRegisters();
            StepCommand.Enabled = true;
            ResetCommand.Enabled = true;
        }

        private void DoStep(object obj)
        {
            _cpu.Step();
            DisplayCpuRegisters();
            VarInfoViewModel.UpdateVariables();
        }

        private void DoReset(object obj)
        {
            _cpu.Reset();
            DisplayCpuRegisters();
        }

        private void DoStop(object obj)
        { 
}

        private void DisplayCpuRegisters()
        {
            Pc = _cpu.PC.ToString("X4");
            Acc = _cpu.ACC.ToString("X1");
            Bl = _cpu.BL.ToString("X1");
            Bh = _cpu.BH.ToString("X1");
            Carry = _cpu.CARRY ? "1" : "0";
            Zero = _cpu.ZERO ? "1" : "0";

            Instruction = Disassembler.Disassemble(_cpu.GetNextBytes());
        }
    }
}
