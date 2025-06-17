using Microsoft.Win32;
using ODBCManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ODBCManager.Services
{
    public class OdbcService
    {
        private const string ODBC_DRIVERS_32_PATH = @"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers";
        private const string ODBC_DRIVERS_64_PATH = @"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers";
        private const string ODBC_DSN_32_PATH = @"SOFTWARE\ODBC\ODBC.INI";
        private const string ODBC_DSN_64_PATH = @"SOFTWARE\ODBC\ODBC.INI";

        public List<OdbcDriver> GetInstalledDrivers()
        {
            var drivers = new List<OdbcDriver>();
            
            // 32-Bit Treiber
            drivers.AddRange(GetDriversFromRegistry(RegistryView.Registry32, false));
            
            // 64-Bit Treiber
            if (Environment.Is64BitOperatingSystem)
            {
                drivers.AddRange(GetDriversFromRegistry(RegistryView.Registry64, true));
            }

            return drivers.OrderBy(d => d.Name).ToList();
        }

        private List<OdbcDriver> GetDriversFromRegistry(RegistryView view, bool is64Bit)
        {
            var drivers = new List<OdbcDriver>();
            
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var driversKey = baseKey.OpenSubKey(ODBC_DRIVERS_32_PATH);
                
                if (driversKey != null)
                {
                    foreach (var driverName in driversKey.GetValueNames())
                    {
                        var value = driversKey.GetValue(driverName)?.ToString();
                        if (value == "Installed")
                        {
                            drivers.Add(new OdbcDriver
                            {
                                Name = driverName,
                                Is64Bit = is64Bit,
                                Version = GetDriverVersion(baseKey, driverName)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error - in production you might want to use a logging framework
                System.Diagnostics.Debug.WriteLine($"Error reading drivers from registry: {ex.Message}");
            }

            return drivers;
        }

        private string GetDriverVersion(RegistryKey baseKey, string driverName)
        {
            try
            {
                using var driverKey = baseKey.OpenSubKey($@"SOFTWARE\ODBC\ODBCINST.INI\{driverName}");
                return driverKey?.GetValue("DriverODBCVer")?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public List<OdbcConnection> GetDsnConnections()
        {
            var connections = new List<OdbcConnection>();
            
            // 32-Bit DSNs
            connections.AddRange(GetDsnFromRegistry(RegistryView.Registry32, false));
            
            // 64-Bit DSNs
            if (Environment.Is64BitOperatingSystem)
            {
                connections.AddRange(GetDsnFromRegistry(RegistryView.Registry64, true));
            }

            return connections.OrderBy(c => c.Name).ToList();
        }

        private List<OdbcConnection> GetDsnFromRegistry(RegistryView view, bool is64Bit)
        {
            var connections = new List<OdbcConnection>();
            
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var dsnKey = baseKey.OpenSubKey(ODBC_DSN_32_PATH);
                
                if (dsnKey != null)
                {
                    // Get list of DSNs
                    using var dsnListKey = dsnKey.OpenSubKey("ODBC Data Sources");
                    if (dsnListKey != null)
                    {
                        foreach (var dsnName in dsnListKey.GetValueNames())
                        {
                            var driver = dsnListKey.GetValue(dsnName)?.ToString() ?? "";
                            var connectionDetails = GetConnectionDetails(dsnKey, dsnName, driver, is64Bit);
                            if (connectionDetails != null)
                            {
                                connections.Add(connectionDetails);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading DSN from registry: {ex.Message}");
            }

            return connections;
        }

        private OdbcConnection? GetConnectionDetails(RegistryKey dsnKey, string dsnName, string driver, bool is64Bit)
        {
            try
            {
                using var connectionKey = dsnKey.OpenSubKey(dsnName);
                if (connectionKey == null) return null;

                return new OdbcConnection
                {
                    Name = dsnName,
                    Driver = driver,
                    Server = connectionKey.GetValue("Server")?.ToString() ?? "",
                    Database = connectionKey.GetValue("Database")?.ToString() ?? "",
                    Description = connectionKey.GetValue("Description")?.ToString() ?? "",
                    Is64Bit = is64Bit
                };
            }
            catch
            {
                return null;
            }
        }

        public bool CreateDsn(OdbcConnection connection)
        {
            try
            {
                var view = connection.Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32;
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var dsnKey = baseKey.OpenSubKey(ODBC_DSN_32_PATH, true);
                
                if (dsnKey == null) return false;

                // Add to ODBC Data Sources list
                using var dsnListKey = dsnKey.OpenSubKey("ODBC Data Sources", true) ?? 
                                      dsnKey.CreateSubKey("ODBC Data Sources");
                dsnListKey.SetValue(connection.Name, connection.Driver);

                // Create DSN configuration
                using var connectionKey = dsnKey.CreateSubKey(connection.Name);
                connectionKey.SetValue("Driver", GetDriverPath(connection.Driver, connection.Is64Bit));
                connectionKey.SetValue("Server", connection.Server);
                connectionKey.SetValue("Database", connection.Database);
                connectionKey.SetValue("Description", connection.Description);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating DSN: {ex.Message}");
                return false;
            }
        }

        public bool DeleteDsn(string dsnName, bool is64Bit)
        {
            try
            {
                var view = is64Bit ? RegistryView.Registry64 : RegistryView.Registry32;
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var dsnKey = baseKey.OpenSubKey(ODBC_DSN_32_PATH, true);
                
                if (dsnKey == null) return false;

                // Remove from ODBC Data Sources list
                using var dsnListKey = dsnKey.OpenSubKey("ODBC Data Sources", true);
                dsnListKey?.DeleteValue(dsnName, false);

                // Delete DSN configuration
                dsnKey.DeleteSubKey(dsnName, false);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting DSN: {ex.Message}");
                return false;
            }
        }

        public bool TestConnection(OdbcConnection connection)
        {
            try
            {
                using var odbcConnection = new System.Data.Odbc.OdbcConnection(BuildConnectionString(connection));
                odbcConnection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string BuildConnectionString(OdbcConnection connection)
        {
            return $"DSN={connection.Name};SERVER={connection.Server};DATABASE={connection.Database};";
        }

        private string GetDriverPath(string driverName, bool is64Bit)
        {
            try
            {
                var view = is64Bit ? RegistryView.Registry64 : RegistryView.Registry32;
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var driverKey = baseKey.OpenSubKey($@"SOFTWARE\ODBC\ODBCINST.INI\{driverName}");
                
                return driverKey?.GetValue("Driver")?.ToString() ?? driverName;
            }
            catch
            {
                return driverName;
            }
        }
    }
}