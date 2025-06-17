using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ODBCManager.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ODBCManager.Views
{
    public partial class ConnectionDialog : MetroWindow, INotifyPropertyChanged
    {
        private List<OdbcDriver> _drivers;
        
        public List<OdbcDriver> Drivers
        {
            get => _drivers;
            set => SetProperty(ref _drivers, value);
        }

        public OdbcConnection? Connection { get; private set; }

        public ConnectionDialog(List<OdbcDriver> drivers)
        {
            InitializeComponent();
            DataContext = this;
            
            _drivers = drivers ?? new List<OdbcDriver>();
            
            // Filter drivers based on selected architecture
            ArchitectureComboBox.SelectionChanged += ArchitectureComboBox_SelectionChanged;
            UpdateDriverList();
        }

        private void ArchitectureComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateDriverList();
        }

        private void UpdateDriverList()
        {
            if (ArchitectureComboBox.SelectedIndex == -1) return;

            bool is64Bit = ArchitectureComboBox.SelectedIndex == 1; // 0 = 32-Bit, 1 = 64-Bit
            var filteredDrivers = _drivers.Where(d => d.Is64Bit == is64Bit).ToList();
            
            DriverComboBox.ItemsSource = filteredDrivers;
            if (filteredDrivers.Any())
            {
                DriverComboBox.SelectedIndex = 0;
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                await this.ShowMessageAsync("Validierungsfehler", 
                    "Bitte füllen Sie alle Pflichtfelder aus:\n" +
                    "- Verbindungsname\n" +
                    "- ODBC-Treiber\n" +
                    "- Server/Hostname");
                return;
            }

            try
            {
                Connection = CreateConnectionFromInput();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Fehler", $"Fehler beim Erstellen der Verbindung: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            return !string.IsNullOrWhiteSpace(NameTextBox.Text) &&
                   DriverComboBox.SelectedItem != null &&
                   !string.IsNullOrWhiteSpace(ServerTextBox.Text);
        }

        private OdbcConnection CreateConnectionFromInput()
        {
            var selectedDriver = (OdbcDriver)DriverComboBox.SelectedItem;
            bool is64Bit = ArchitectureComboBox.SelectedIndex == 1; // 0 = 32-Bit, 1 = 64-Bit

            return new OdbcConnection
            {
                Name = NameTextBox.Text.Trim(),
                Driver = selectedDriver.Name,
                Server = ServerTextBox.Text.Trim(),
                Database = DatabaseTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                Is64Bit = is64Bit
            };
        }

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
}