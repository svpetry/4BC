using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CpuEmu.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ActionCommand : ICommand
    {
        private readonly Action<object> _executeHandler;
        private bool _enabled = true;

        public ActionCommand(Action<object> execute)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            _executeHandler = execute;
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                CanExecuteChanged?.Invoke(this, null);
            }
        }

        public void Execute(object parameter)
        {
            _executeHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _enabled;
        }

        public event EventHandler CanExecuteChanged;
    }

    public class AsyncActionCommand : ICommand
    {
        private readonly Func<object, Task> _executeHandler;
        private bool _enabled = true;

        public AsyncActionCommand(Func<object, Task> execute)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            _executeHandler = execute;
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                CanExecuteChanged?.Invoke(this, null);
            }
        }

        public async void Execute(object parameter)
        {
            await _executeHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _enabled;
        }

        public event EventHandler CanExecuteChanged;
    }
}
