using GalaSoft.MvvmLight;

namespace TelloController.Models
{
    public class ControlCommand : ViewModelBase
    {
        private string _baseCommand;

        public ControlCommand(string command)
        {
            _baseCommand = command;
            Command = command;
            HasValue = false;
        }

        public ControlCommand(string command, string unit, int max, int min, int defaultValue)
        {
            _baseCommand = command;
            Unit = unit;
            Max = max;
            Min = min;
            Value = defaultValue;
            HasValue = true;
        }

        private string _command;
        public string Command
        {
            get => _command;
            private set => Set(ref _command, value);
        }

        public bool HasValue { get; }

        public string Unit { get; }

        public int Max { get; }

        public int Min { get; }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                Set(ref _value, value);
                Command = $"{_baseCommand} {Value}";
            }
        }
    }
}
