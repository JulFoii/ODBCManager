using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ODBCManager.Models;
using ODBCManager.Services;
using ODBCManager.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ODBCManager
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private readonly OdbcService _odbcService;
        private OdbcConnection? _selectedConnection;
        
        public ObservableCollection<OdbcConnection> Connections { get; set; }
        public ObservableCollection<OdbcDriver> Drivers { get; set; }

        public OdbcConnection? SelectedConnection
        {
            get => _selectedConnection;
            set => SetProperty(ref _selectedConnection, value);
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            _odbcService = new OdbcService();
            Connections = new ObservableCollection<OdbcConnection>();
            Drivers = new ObservableCollection<OdbcDriver>();
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var progressDialog = await this.ShowProgressAsync("Laden", "ODBC-Daten werden geladen...");
                progressDialog.SetIndeterminate();

                await Task.Run(() =>
                {
                    // Load connections
                    var connections = _odbcService.GetDsnConnections();
                    
                    // Load drivers
                    var drivers = _odbcService.GetInstalledDrivers();

                    Dispatcher.Invoke(() =>
                    {
                        Connections.Clear();
                        foreach (var connection in connections)
                        {
                            Connections.Add(connection);
                        }

                        Drivers.Clear();
                        foreach (var driver in drivers)
                        {
                            Drivers.Add(driver);
                        }
                    });
                });

                await progressDialog.CloseAsync();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Fehler", $"Fehler beim Laden der ODBC-Daten: {ex.Message}");
            }
        }

        private void ConnectionsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var hasSelection = SelectedConnection != null;
            DeleteConnectionButton.IsEnabled = hasSelection;
            TestConnectionButton.IsEnabled = hasSelection;
        }

        private async void CreateConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConnectionDialog(_odbcService.GetInstalledDrivers());
            if (dialog.ShowDialog() == true && dialog.Connection != null)
            {
                try
                {
                    var success = _odbcService.CreateDsn(dialog.Connection);
                    if (success)
                    {
                        await this.ShowMessageAsync("Erfolg", "ODBC-Verbindung wurde erfolgreich erstellt.");
                        await LoadDataAsync();
                    }
                    else
                    {
                        await this.ShowMessageAsync("Fehler", "Fehler beim Erstellen der ODBC-Verbindung.");
                    }
                }
                catch (Exception ex)
                {
                    await this.ShowMessageAsync("Fehler", $"Fehler beim Erstellen der ODBC-Verbindung: {ex.Message}");
                }
            }
        }

        private async void DeleteConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection == null) return;

            var result = await this.ShowMessageAsync("Bestätigung", 
                $"Möchten Sie die ODBC-Verbindung '{SelectedConnection.Name}' wirklich löschen?",
                MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    var success = _odbcService.DeleteDsn(SelectedConnection.Name, SelectedConnection.Is64Bit);
                    if (success)
                    {
                        await this.ShowMessageAsync("Erfolg", "ODBC-Verbindung wurde erfolgreich gelöscht.");
                        await LoadDataAsync();
                    }
                    else
                    {
                        await this.ShowMessageAsync("Fehler", "Fehler beim Löschen der ODBC-Verbindung.");
                    }
                }
                catch (Exception ex)
                {
                    await this.ShowMessageAsync("Fehler", $"Fehler beim Löschen der ODBC-Verbindung: {ex.Message}");
                }
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection == null) return;

            var progressDialog = await this.ShowProgressAsync("Verbindungstest", "Verbindung wird getestet...");
            progressDialog.SetIndeterminate();

            try
            {
                var success = await Task.Run(() => _odbcService.TestConnection(SelectedConnection));
                
                await progressDialog.CloseAsync();
                
                if (success)
                {
                    await this.ShowMessageAsync("Verbindungstest", "Verbindung erfolgreich!", MessageDialogStyle.Affirmative);
                }
                else
                {
                    await this.ShowMessageAsync("Verbindungstest", "Verbindung fehlgeschlagen!", MessageDialogStyle.Affirmative);
                }
            }
            catch (Exception ex)
            {
                await progressDialog.CloseAsync();
                await this.ShowMessageAsync("Fehler", $"Fehler beim Testen der Verbindung: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("Über ODBC Manager", 
                "ODBC Connection Manager v1.0\n\n" +
                "Verwaltet 32-Bit und 64-Bit ODBC-Verbindungen\n" +
                "Erstellt mit WPF, MahApps.Metro und .NET 8.0\n\n" +
                "© 2024");
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