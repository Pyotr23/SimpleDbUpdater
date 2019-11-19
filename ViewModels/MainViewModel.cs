using SimpleDbUpdater.Events;
using SimpleDbUpdater.Managers;
using SimpleDbUpdater.Realizations;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using SimpleDbUpdater.Loggers;
using SimpleDbUpdater.Properties;
using Notifications.Wpf;
using System.Reflection;

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
        private int _templateScriptsCountBeforeExecuting;
        private static SolidColorBrush _indianRedColor = new SolidColorBrush(Colors.IndianRed);
        private static SolidColorBrush _limeGreenColor = new SolidColorBrush(Colors.LimeGreen);
        private SolidColorBrush _connectionColor = _indianRedColor;
        private double _progressBarValue = 0;
        private double _itemProgressValue;
        private string _currentScriptName;
        private bool _isDarkTheme;
        private const int SqlCommandTimeout = 6000;
        private int _spinDuration = 0;
        private Visibility _spinnerVisibility = Visibility.Hidden;
        private bool _areScriptsExecuted = false;
        private bool _deleteScriptsAfterExecute;
        private string _logFilePath = string.Empty;
        private NotificationManager _notificationManager = new NotificationManager();

        Regex _regexDatabase = new Regex(@"(?<=Database\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);
        Regex _regexInitialCatalog = new Regex(@"(?<=Initial Catalog\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);

        public ProgressBarManager ProgressBarManager { get; } = ProgressBarManager.Instance;

        public Logger Logger { get; } = UpdaterLogger.Instance;

        public ICommand OpenScriptsFolderPath { get; }
        public ICommand SetScriptsFolderPath { get; }
        public ICommand ExecuteScripts { get; }
        public ICommand AskAboutTheme { get; }

        public ICommand ClickVerboseLogLevel { get; }
        public ICommand ClickDebugLogLevel { get; }
        public ICommand ClickInformationLogLevel { get; }
        public ICommand ClickWarningLogLevel { get; }
        public ICommand ClickErrorLogLevel { get; }
        public ICommand ClickFatalLogLevel { get; }
        public ICommand OpenLog { get; }

        public bool DeleteScriptsAfterExecute
        {
            get => _deleteScriptsAfterExecute;
            set
            {
                SetProperty(ref _deleteScriptsAfterExecute, value);
                Settings.Default[nameof(DeleteScriptsAfterExecute)] = value;
            }             
        }

        public Visibility SpinnerVisibility
        {
            get => _spinnerVisibility;
            set
            {
                SetProperty(ref _spinnerVisibility, value);
                if (value == Visibility.Visible)
                    SpinDuration = 4;
                else
                    SpinDuration = 0;
            }
        }

        public int SpinDuration
        {
            get => _spinDuration;
            set => SetProperty(ref _spinDuration, value);            
        }

        public bool AreScriptsExecuted
        {
            get => _areScriptsExecuted;
            set
            {
                _areScriptsExecuted = value;
                if (value)
                    SpinnerVisibility = Visibility.Visible;
                else
                {
                    ItemProgressValue = 0;
                    CurrentScriptName = "";
                    SpinnerVisibility = Visibility.Hidden;
                }
            }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {                
                SetProperty(ref _isDarkTheme, value);
                Settings.Default[nameof(IsDarkTheme)] = value;
            }                 
        }

        public string CurrentScriptName
        {
            get => _currentScriptName;
            set => SetProperty(ref _currentScriptName, value);
        }

        public double ItemProgressValue
        {
            get => _itemProgressValue;
            set => SetProperty(ref _itemProgressValue, value);
        }

        public SolidColorBrush ConnectionColor
        {
            get => _connectionColor;
            set => SetProperty(ref _connectionColor, value);
        }

        public double ProgressBarValue
        {
            get => _progressBarValue;
            set => SetProperty(ref _progressBarValue, value);
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
                Settings.Default[nameof(DualLaunch)] = value;
            }
        }

        public string ScriptsFolderPath
        {
            get => _scriptsFolderPath;
            set
            {
                SetProperty(ref _scriptsFolderPath, value);
                Settings.Default[nameof(ScriptsFolderPath)] = value;
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
                Settings.Default[nameof(ConnectionString)] = value;
            }
        }

        public string DatabaseName
        {
            get => _databaseName;
            set => _databaseName = value;
        }

        private bool _isCheckedVerboseLevel;
        private bool _isCheckedDebugLevel;
        private bool _isCheckedInformationLevel;
        private bool _isCheckedWarningLevel;
        private bool _isCheckedErrorLevel;
        private bool _isCheckedFatalLevel;

        public bool IsCheckedVerboseLevel
        {
            get => _isCheckedVerboseLevel;
            set
            {
                SetProperty(ref _isCheckedVerboseLevel, value);
                if (value)
                {
                    ClickVerboseLogLevel?.Execute(null);
                    Settings.Default["LogLevel"] = "Verbose";
                }                
            }
        }
        
        public bool IsCheckedDebugLevel
        {
            get => _isCheckedDebugLevel;
            set
            {
                SetProperty(ref _isCheckedDebugLevel, value);
                if (value)
                {
                    ClickDebugLogLevel?.Execute(null);
                    Settings.Default["LogLevel"] = "Debug";
                }                
            }
        }

        public bool IsCheckedInformationLevel
        {
            get => _isCheckedInformationLevel;
            set
            {
                SetProperty(ref _isCheckedInformationLevel, value);
                if (value)
                {
                    ClickInformationLogLevel?.Execute(null);
                    Settings.Default["LogLevel"] = "Information";
                }                
            }
        }

        public bool IsCheckedWarningLevel
        {
            get => _isCheckedWarningLevel;
            set
            {
                SetProperty(ref _isCheckedWarningLevel, value);
                if (value)
                {
                    ClickWarningLogLevel?.Execute(null);
                    Settings.Default["LogLevel"] = "Warning";
                }                
            }
        }

        public bool IsCheckedErrorLevel
        {
            get => _isCheckedErrorLevel;
            set
            {
                SetProperty(ref _isCheckedErrorLevel, value);
                if (value)
                {
                    ClickErrorLogLevel?.Execute(null);
                    Settings.Default["LogLevel"] = "Error";
                }                
            }
        }

        public bool IsCheckedFatalLevel
        {
            get => _isCheckedFatalLevel;
            set
            {
                SetProperty(ref _isCheckedFatalLevel, value);
                if (value)
                {
                    ClickFatalLogLevel?.Execute(null);
                    Settings.Default["LogLevel"] = "Fatal";
                }                
            }
        }

        public MainViewModel()
        {
            ClickVerboseLogLevel = new RelayCommand(o => UpdaterLogger.LogEventLevel = LogEventLevel.Verbose);
            ClickDebugLogLevel = new RelayCommand(o => UpdaterLogger.LogEventLevel = LogEventLevel.Debug);
            ClickInformationLogLevel = new RelayCommand(o => UpdaterLogger.LogEventLevel = LogEventLevel.Information);
            ClickWarningLogLevel = new RelayCommand(o => UpdaterLogger.LogEventLevel = LogEventLevel.Warning);
            ClickErrorLogLevel = new RelayCommand(o => UpdaterLogger.LogEventLevel = LogEventLevel.Error);
            ClickFatalLogLevel = new RelayCommand(o => UpdaterLogger.LogEventLevel = LogEventLevel.Fatal);
            
            SetLogLevelFromSettings();
            Logger.Information("Программа запущена.");

            ScriptsFolderPath = (string)Settings.Default[nameof(ScriptsFolderPath)];
            ConnectionString = (string)Settings.Default[nameof(ConnectionString)];
            DeleteScriptsAfterExecute = (bool)Settings.Default[nameof(DeleteScriptsAfterExecute)];
            IsDarkTheme = (bool)Settings.Default[nameof(IsDarkTheme)];
            DualLaunch = (bool)Settings.Default[nameof(DualLaunch)];

            CurrentTime = DateTime.Now.ToLongTimeString();

            ExecuteScripts = new RelayCommand(
                o =>
                Task.Run(() => RunningScripts()),
                x => TemplateScriptsNumber != 0 && ConnectionColor == _limeGreenColor && !AreScriptsExecuted);

            OpenScriptsFolderPath = new RelayCommand(o => OpenFolder(ScriptsFolderPath), x => Directory.Exists(ScriptsFolderPath));
            SetScriptsFolderPath = new RelayCommand(o => ScriptsFolderPath = GetScriptsFolderPath());
            AskAboutTheme = new RelayCommand(o => ReloadIfNeeding(), x => !AreScriptsExecuted);
            OpenLog = new RelayCommand(o => OpenLogFile(), x => IsLogFileExist());
                                    
            StartClock();
            ProgressBarManager.NewProgressBarValue += ChangeSlider;
        }       

        private void OpenLogFile()
        {
            Process.Start(_logFilePath);
        }

        private bool IsLogFileExist()
        {
            string currentFilePath = Assembly.GetExecutingAssembly().Location;
            _logFilePath = Path.ChangeExtension(currentFilePath, "log");
            return File.Exists(_logFilePath);
        }

        private void SetLogLevelFromSettings()
        {
            switch ((string)Settings.Default["LogLevel"])
            {
                case "Verbose":
                    IsCheckedVerboseLevel = true;
                    break;
                case "Debug":
                    IsCheckedDebugLevel = true;
                    break;
                case "Information":
                    IsCheckedInformationLevel = true;
                    break;
                case "Warning":
                    IsCheckedWarningLevel = true;
                    break;
                case "Error":
                    IsCheckedErrorLevel = true;
                    break;
                case "Fatal":
                    IsCheckedFatalLevel = true;
                    break;
            }
        }

        private void ReloadIfNeeding()
        {
            var result = MessageBox.Show("Перезагрузить программу немедленно для изменения темы?",
                    "Что дальше", MessageBoxButton.YesNo, MessageBoxImage.Question);
            IsDarkTheme = !IsDarkTheme;
            if (result == MessageBoxResult.Yes)
            {
                Settings.Default.Save();
                Settings.Default.Reload();
                Logger.Information("Программа будет перезапущена для смены темы интерфейса.");
                Application.Current.Shutdown();
                WinForms.Application.Restart();
            }            
        }

        private void OpenFolder(string folderPath)
        {
            Logger.Debug("Выбор пути папки с файлами.");
            Process.Start(folderPath);
        }

        private void StartClock()
        {
            Logger.Debug("Подготовка и запуск ежесекундного оспроса.");
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void RunningScripts()
        {
            AreScriptsExecuted = true;

            if (DualLaunch)
            {
                _templateScriptsCountBeforeExecuting = TemplateScriptsNumber * 2;
                Logger.Information("Двойной запуск {TemplateScriptsNumber} скриптов.", TemplateScriptsNumber);
            }
            else
            {
                _templateScriptsCountBeforeExecuting = TemplateScriptsNumber;
                Logger.Information("Запуск {TemplateScriptsNumber} скриптов.", TemplateScriptsNumber);
            }                       

            var sqlFileWithCorrectNames = GetSqlFilePaths().Where(s => IsTemplateScriptName(new FileInfo(s).Name)).ToArray();
            string errorMessage = string.Empty;
            bool isRerun = false;

            if (DualLaunch)
            {
                bool deleteScriptAfterExecuting = false;
                errorMessage = GetErrorAfterExecutingScripts(sqlFileWithCorrectNames, deleteScriptAfterExecuting, isRerun);
                if (string.IsNullOrEmpty(errorMessage))
                {
                    isRerun = true;
                    errorMessage = GetErrorAfterExecutingScripts(sqlFileWithCorrectNames, DeleteScriptsAfterExecute, isRerun);
                    if (!string.IsNullOrEmpty(errorMessage))
                        ShowRerunMessageBox(errorMessage);
                }
                else
                    ShowErrorMessageBox(errorMessage);
            }
            else
            {
                errorMessage = GetErrorAfterExecutingScripts(sqlFileWithCorrectNames, DeleteScriptsAfterExecute, isRerun);
                if (!string.IsNullOrEmpty(errorMessage))
                    ShowErrorMessageBox(errorMessage);
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                bool successUpdate = true;
                ShowNotification(successUpdate);
            }

            Logger.Information("Обновление базы данных окончено.");
            AreScriptsExecuted = false;
        }

        private void ShowNotification(bool isSuccess)
        {
            if (isSuccess)
                _notificationManager.Show(new NotificationContent
                {
                    Title = "Обновление базы данных успешно выполнено.",
                    Type = NotificationType.Success
                });
            else
                _notificationManager.Show(new NotificationContent
                {
                    Title = "Обновление базы данных закончилось с ошибкой.",
                    Type = NotificationType.Error
                });
        }

        private void ShowErrorMessageBox(string errorMessage)
        {
            string errorTitle = new string(errorMessage.TakeWhile(c => c != '\n').ToArray());
            string error = new string(errorMessage.SkipWhile(c => c != '\n').Skip(1).ToArray());
            bool successUpdate = false;
            ShowNotification(successUpdate);
            MessageBox.Show(error, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowRerunMessageBox(string errorMessage)
        {
            string errorTitle = new string(errorMessage.TakeWhile(c => c != '\n').ToArray());
            string error = new string(errorMessage.SkipWhile(c => c != '\n').Skip(1).ToArray());            
            string errorWithRerunProposition = $"Ошибка при повторном выполнении скрипта.\n{error}";
            MessageBox.Show(errorWithRerunProposition, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
                var dbConnectionBuilder = new DbConnectionStringBuilder();
                try
                {
                    dbConnectionBuilder.ConnectionString = ConnectionString;
                    using (var connection = new SqlConnection(ConnectionString))
                    {                        
                        await connection.OpenAsync();
                        ConnectionColor = _limeGreenColor;                        
                    }
                }
                catch { }                
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private bool IsTemplateScriptName(string scriptName)
        {
            var regex = new Regex(@"^\d+?_.*");
            string matchValue = regex.Match(scriptName).Value;
            return !string.IsNullOrEmpty(matchValue);
        }

        private string GetScriptsFolderPath()
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == WinForms.DialogResult.OK)
                    return dialog.SelectedPath;
                return ScriptsFolderPath;
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

        private string GetErrorAfterExecutingScripts(string[] scriptPaths, bool deleteScript, bool isRerun)
        {            
            string error = string.Empty;
            int startProgressBarValue = isRerun ? _templateScriptsCountBeforeExecuting / 2 : 0;            
            ProgressBarManager.ChangeProgressBarValue(startProgressBarValue);
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();                
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    string filePath = scriptPaths[i];
                    string script = GetScriptText(filePath);
                    script = ModifyScript(script);
                    CurrentScriptName = Path.GetFileNameWithoutExtension(filePath);
                    var scriptsPartsBetweenGo = SplitSqlStatements(script);
                    Logger.Debug("Запуск cкрипта №{i} \"{CurrentScriptName}\"", i, CurrentScriptName);
                    foreach (var scriptsPart in scriptsPartsBetweenGo)
                    {
                        using (var command = new SqlCommand(scriptsPart, sqlConnection))
                        {
                            try
                            {
                                command.CommandTimeout = SqlCommandTimeout;
                                command.ExecuteNonQuery();                                
                            }
                            catch (Exception ex)
                            {                                
                                error = $"Ошибка, скрипт \"{Path.GetFileName(filePath)}\"\n{ex.Message}";
                                Logger.Warning("Ошибка выполнения скрипта.");
                                return error;
                            }
                        }
                    }
                    int currentProgressBarValue = isRerun
                        ? i + 1 + _templateScriptsCountBeforeExecuting / 2
                        : i + 1;
                    ProgressBarManager.ChangeProgressBarValue(currentProgressBarValue);
                    if (deleteScript)
                        File.Delete(scriptPaths[i]);
                }               
            }
            return error;
        }

        private void ChangeSlider(object sender, NewProgressBarValueEventsArgs e)
        {
            int newValue = e.NewValue;
            ItemProgressValue = newValue / (double)_templateScriptsCountBeforeExecuting;
            ProgressBarValue = ItemProgressValue * 100;            
        }

        private string GetScriptText(string scriptPath)
        {
            using (var streamReader = new StreamReader(scriptPath))
            {
                return streamReader.ReadToEnd();
            }
        }        

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
            var regex = new Regex(@"\A\s*USE\s*\S+\s*\n*\s*GO[\r\n]*", RegexOptions.IgnoreCase);
            var match = regex.Match(script);
            int matchValueLength = match.Value.Length;
            return script.Substring(matchValueLength);
        }

        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            var statements = Regex.Split(
                    sqlScript,
                    @"^GO.*",
                    RegexOptions.Multiline |
                    RegexOptions.IgnorePatternWhitespace |
                    RegexOptions.IgnoreCase);            
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\r', '\n'));
        }
    }
}