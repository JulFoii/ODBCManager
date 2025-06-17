using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ODBCManager.Models
{
    public class OdbcConnection : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _driver = string.Empty;
        private string _server = string.Empty;
        private string _database = string.Empty;
        private string _description = string.Empty;
        private bool _is64Bit;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Driver
        {
            get => _driver;
            set => SetProperty(ref _driver, value);
        }

        public string Server
        {
            get => _server;
            set => SetProperty(ref _server, value);
        }

        public string Database
        {
            get => _database;
            set => SetProperty(ref _database, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool Is64Bit
        {
            get => _is64Bit;
            set => SetProperty(ref _is64Bit, value);
        }

        public string Architecture => Is64Bit ? "64-Bit" : "32-Bit";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class OdbcDriver
    {
        public string Name { get; set; } = string.Empty;
        public bool Is64Bit { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Architecture => Is64Bit ? "64-Bit" : "32-Bit";
        
        public override string ToString() => $"{Name} ({Architecture})";
    }
}