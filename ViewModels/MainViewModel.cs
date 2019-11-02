using SimpleDbUpdater.Realizations;
using System;
using System.Collections.Generic;
using System.Configuration;
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
using Tulpep.NotificationWindow;
using WinForms = System.Windows.Forms;

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

        Regex _regexDatabase = new Regex(@"(?<=Database\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);
        Regex _regexInitialCatalog = new Regex(@"(?<=Initial Catalog\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);

        private delegate void ScriptHandler(int scriptNumber);
        private event ScriptHandler ScriptIsExecuting;

        public ICommand OpenScriptsFolderPath { get; }
        public ICommand SetScriptsFolderPath { get; }
        public ICommand ExecuteScripts { get; }
        public ICommand AskAboutTheme { get; }

        public bool DeleteScriptsAfterExecute
        {
            get => _deleteScriptsAfterExecute;
            set
            {
                SetProperty(ref _deleteScriptsAfterExecute, value);
                SaveAppSetting(nameof(DeleteScriptsAfterExecute), value.ToString());
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
                SaveAppSetting(nameof(IsDarkTheme), value.ToString());
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
                SaveAppSetting(nameof(DualLaunch), value.ToString());
            }
        }

        public string ScriptsFolderPath
        {
            get => _scriptsFolderPath;
            set
            {
                SetProperty(ref _scriptsFolderPath, value);
                SaveAppSetting(nameof(ScriptsFolderPath), value);
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
                SaveAppSetting(nameof(ConnectionString), value);
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
            bool.TryParse(ConfigurationManager.AppSettings[nameof(DeleteScriptsAfterExecute)], out bool deleteScripts);
            DeleteScriptsAfterExecute = deleteScripts;
            bool.TryParse(ConfigurationManager.AppSettings[nameof(IsDarkTheme)], out bool isDarkTheme);
            IsDarkTheme = isDarkTheme;
            bool.TryParse(ConfigurationManager.AppSettings[nameof(DualLaunch)], out bool dualLaunch);
            DualLaunch = dualLaunch;
            CurrentTime = DateTime.Now.ToLongTimeString();

            ExecuteScripts = new RelayCommand(
                o =>
                Task.Run(() => RunningScripts()),
                x => TemplateScriptsNumber != 0 && ConnectionColor == _limeGreenColor && !AreScriptsExecuted);

            OpenScriptsFolderPath = new RelayCommand(o => OpenFolder(ScriptsFolderPath), x => Directory.Exists(ScriptsFolderPath));
            SetScriptsFolderPath = new RelayCommand(o => ScriptsFolderPath = GetScriptsFolderPath());
            AskAboutTheme = new RelayCommand(o => ReloadIfNeeding(), x => !AreScriptsExecuted);

            StartClock();

            ScriptIsExecuting += ChangeSlider;
        }

        private void ReloadIfNeeding()
        {
            var result = MessageBox.Show("Перезагрузить программу немедленно для изменения темы?",
                    "Что дальше", MessageBoxButton.YesNo, MessageBoxImage.Question);
            IsDarkTheme = !IsDarkTheme;
            if (result == MessageBoxResult.Yes)
            {
                
                Application.Current.Shutdown();
                WinForms.Application.Restart();
            }            
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

        private void RunningScripts()
        {
            AreScriptsExecuted = true;            
            _templateScriptsCountBeforeExecuting = DualLaunch ? TemplateScriptsNumber * 2 : TemplateScriptsNumber;

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
                    ShowMessageBox(errorMessage);
            }
            else
            {
                errorMessage = GetErrorAfterExecutingScripts(sqlFileWithCorrectNames, DeleteScriptsAfterExecute, isRerun);
                if (!string.IsNullOrEmpty(errorMessage))
                    ShowMessageBox(errorMessage);
            }

            AreScriptsExecuted = false;
        }

        private void ShowMessageBox(string errorMessage)
        {
            string errorTitle = new string(errorMessage.TakeWhile(c => c != '\n').ToArray());
            string error = new string(errorMessage.SkipWhile(c => c != '\n').Skip(1).ToArray());
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

        private static void SaveAppSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Full, true);
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

        private string GetErrorAfterExecutingScripts(string[] scriptPaths, bool deleteScript, bool isRerun)
        {            
            string error = string.Empty;
            ScriptIsExecuting?.Invoke(isRerun ? _templateScriptsCountBeforeExecuting / 2 : 0);
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
                                return error;
                            }
                        }
                    }
                    int progressBarValue = isRerun
                        ? i + 1 + _templateScriptsCountBeforeExecuting / 2
                        : i + 1;
                    ScriptIsExecuting?.Invoke(progressBarValue);
                    if (deleteScript)
                        File.Delete(scriptPaths[i]);
                }               
            }
            return error;
        }

        private void ChangeSlider(int scriptNumber)
        {
            ItemProgressValue = scriptNumber / (double)_templateScriptsCountBeforeExecuting;
            ProgressBarValue = ItemProgressValue * 100;            
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