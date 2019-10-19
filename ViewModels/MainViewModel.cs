﻿using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using SimpleDbUpdater.Realizations;
using SimpleDbUpdater.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media;
using System.Data;

namespace SimpleDbUpdater.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private string _scriptsFolderPath;
        private string _connectionString;
        private string _databaseName;
        private bool _dualLaunch;
        private string _currentTime;
        private int _scriptsNumber;
        private int _templateScriptsNumber;
        private static SolidColorBrush _indianRedColor = new SolidColorBrush(Colors.IndianRed);
        private static SolidColorBrush _limeGreenColor = new SolidColorBrush(Colors.LimeGreen);
        private SolidColorBrush _connectionColor = _indianRedColor;
        Regex _regexDatabase = new Regex(@"(?<=Database\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);
        Regex _regexInitialCatalog = new Regex(@"(?<=Initial Catalog\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);

        public ICommand OpenScriptsFolderPath { get; }
        public ICommand SetScriptsFolderPath { get; }
        public ICommand ExecuteScripts { get; }

        public bool AreScriptsExecuted { get; set; } = false;

        public SolidColorBrush ConnectionColor
        {
            get => _connectionColor;
            set => SetProperty(ref _connectionColor, value);
        }

        public int ScriptsNumber
        {
            get => _scriptsNumber;
            set => SetProperty(ref _scriptsNumber, value);
        }

        public int TemplateScriptsNumber
        {
            get => _templateScriptsNumber;
            set => SetProperty(ref _templateScriptsNumber, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public bool DualLaunch
        {
            get => _dualLaunch;
            set
            {
                SetProperty(ref _dualLaunch, value);
                SetSetting(nameof(DualLaunch), value.ToString());
            }
        }

        public string ScriptsFolderPath
        {
            get => _scriptsFolderPath;
            set
            {
                SetProperty(ref _scriptsFolderPath, value);
                SetSetting(nameof(ScriptsFolderPath), value);
            }
        }

        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                SetProperty(ref _connectionString, value);
                string newDatabaseName = GetDbNameFromConnectionString();
                SetProperty(ref _databaseName, newDatabaseName, nameof(DatabaseName));
                SetSetting(nameof(ConnectionString), value);
            }
        }

        public string DatabaseName
        {
            get => _databaseName;
            set => _databaseName = value;
        }

        public MainViewModel()
        {
            ScriptsFolderPath = ConfigurationManager.AppSettings[nameof(ScriptsFolderPath)];
            ConnectionString = ConfigurationManager.AppSettings[nameof(ConnectionString)];
            bool.TryParse(ConfigurationManager.AppSettings[nameof(DualLaunch)], out bool dualLaunch);
            DualLaunch = dualLaunch;
            CurrentTime = DateTime.Now.ToLongTimeString();

            ExecuteScripts = new RelayCommand(
                o =>
                RunningScripts(),
                x => TemplateScriptsNumber != 0 && ConnectionColor == _limeGreenColor && !AreScriptsExecuted);

            OpenScriptsFolderPath = new RelayCommand(o => OpenFolder(ScriptsFolderPath), x => Directory.Exists(ScriptsFolderPath));
            SetScriptsFolderPath = new RelayCommand(o => ScriptsFolderPath = GetScriptsFolderPath());

            StartClock();
        }

        private void OpenFolder(string folderPath)
        {
            Process.Start(folderPath);
        }

        private void StartClock()
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private async void RunningScripts()
        {
            AreScriptsExecuted = true;

            var sqlFilesWithCorrectName = GetSqlFilePaths().Where(s => IsTemplateScriptName(new FileInfo(s).Name)).ToArray();
            string errorMessage = string.Empty;

            if (DualLaunch)
            {
                errorMessage = await GetErrorAfterExecutingScriptsAsync(sqlFilesWithCorrectName);
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = await GetErrorAfterExecutingAndDeletingScriptsAsync(sqlFilesWithCorrectName);
                    if (!string.IsNullOrEmpty(errorMessage))
                        ShowRerunMessageBox(errorMessage);
                }
                else
                    ShowMessageBox(errorMessage);
            }
            else
            {
                errorMessage = await GetErrorAfterExecutingAndDeletingScriptsAsync(sqlFilesWithCorrectName);
                if (!string.IsNullOrEmpty(errorMessage))
                    ShowMessageBox(errorMessage);
            }             
            AreScriptsExecuted = false;
        }

        private void ShowMessageBox(string errorMessage)
        {
            string[] errorParts = errorMessage.Split('\n');
            MessageBox.Show(errorParts[0], errorParts[1], MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowRerunMessageBox(string errorMessage)
        {
            string[] errorParts = errorMessage.Split('\n');
            string text = $"Ошибка при повторном выполнении скрипта.\n{errorParts[0]}";
            MessageBox.Show(text, errorParts[1], MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToLongTimeString();
            if (Directory.Exists(ScriptsFolderPath))
            {
                var scriptsNames = Directory.EnumerateFiles(ScriptsFolderPath)
                    .Where(f => new FileInfo(f).Extension == ".sql")
                    .Select(x => new FileInfo(x).Name).ToArray();
                ScriptsNumber = scriptsNames.Length;
                TemplateScriptsNumber = scriptsNames.Count(x => IsTemplateScriptName(x));
            }
            ConnectionColor = _indianRedColor;
            if (!string.IsNullOrEmpty(DatabaseName))
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                        ConnectionColor = _limeGreenColor;
                    }
                    catch { }
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private bool IsTemplateScriptName(string scriptName)
        {
            var regex = new Regex(@"^\d+?_.*");
            string matchValue = regex.Match(scriptName).Value;
            return !string.IsNullOrEmpty(matchValue);
        }

        private static void SetSetting(string key, string value)
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private string GetScriptsFolderPath()
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == WinForms.DialogResult.OK)
                    return dialog.SelectedPath;
                else
                    return string.Empty;
            }
        }

        private string[] GetSqlFilePaths()
        {
            var fileFullPaths = Directory.GetFiles(ScriptsFolderPath);
            var fileNames = fileFullPaths.Select(f => new FileInfo(f).Name).ToArray();
            var sqlFileNames = fileNames.Where(s => new FileInfo(s).Extension == ".sql").ToArray();
            var sortedSqlFilePathes = sqlFileNames.Select(n => Path.Combine(ScriptsFolderPath, n)).ToArray();
            return sortedSqlFilePathes;
        }

        private async Task<string> GetErrorAfterExecutingScriptsAsync(string[] scriptPaths)
        {
            string error = string.Empty;
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.OpenAsync().Wait();                
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    var filePath = scriptPaths[i];
                    string script = GetScriptText(filePath);
                    script = ModifyScript(script);
                    using (var command = new SqlCommand(script, sqlConnection))
                    {
                        try
                        {
                            int result = await command.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            sqlConnection.Close();
                            error = $"{ex.Message}\nОшибка, скрипт \"{Path.GetFileName(filePath)}\"";
                            return error;
                        }                        
                    }                    
                }
                sqlConnection.Close();
            }
            return error;
        }

        private async Task<string> GetErrorAfterExecutingAndDeletingScriptsAsync(string[] scriptPaths)
        {
            string error = string.Empty;
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.OpenAsync().Wait();
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    var filePath = scriptPaths[i];
                    string script = GetScriptText(filePath);
                    script = ModifyScript(script);
                    using (var command = new SqlCommand(script, sqlConnection))
                    {
                        try
                        {
                            int result = await command.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            sqlConnection.Close();
                            error = $"{ex.Message}\nОшибка, скрипт \"{Path.GetFileName(filePath)}\"";
                            return error;
                        }
                    }                    
                    File.Delete(scriptPaths[i]);
                }
                sqlConnection.Close();
            }
            return error;
        }

        private string GetScriptText(string scriptPath)
        {
            using (var streamReader = new StreamReader(scriptPath))
            {
                return streamReader.ReadToEnd();
            }
        }        

        // Парсер "Database<любое количество пробелов(ЛКП)>=<ЛКП><искомое название БД без пробелов><ЛКП>;"
        private string GetDbNameFromConnectionString()
        {
            var match = _regexDatabase.Match(ConnectionString);
            if (match.Success)
                return match.Value;
            match = _regexInitialCatalog.Match(ConnectionString);
            return match.Value;
        }          

        // Если скрипт начинается с USE <dbName> GO, то убираем.
        private string ModifyScript(string script)
        {
            var regex = new Regex(@"\A\s*USE\s*\S+\s*\n*\s*GO", RegexOptions.IgnoreCase);
            var match = regex.Match(script);
            int matchValueLength = match.Value.Length;
            return script.Substring(matchValueLength);
        }
    }
}
