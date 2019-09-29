using Microsoft.SqlServer.Management.Common;
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
using WinForms = System.Windows.Forms;

namespace SimpleDbUpdater.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private string _scriptsFolderPath;
        private string _connectionString;
        private string _databaseName;
        private bool _dualLaunch;

        public ICommand SetScriptsFolderPath { get; }
        public ICommand ExecuteScripts { get; }

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

            ExecuteScripts = new RelayCommand(
                o => 
                {                    
                    var sqlFiles = GetSqlFilePaths();
                    try
                    {
                        ExecuteAndDeleteNonQueryScripts(sqlFiles);
                        if (DualLaunch)
                            ExecuteAndDeleteNonQueryScripts(sqlFiles);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }                    
                }, 
                x => !(string.IsNullOrEmpty(ScriptsFolderPath) || string.IsNullOrEmpty(DatabaseName)));

            SetScriptsFolderPath = new RelayCommand(o => ScriptsFolderPath = GetScriptsFolderPath());
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

        private void ExecuteAndDeleteNonQueryScripts(string[] scriptPaths)
        {
            if (scriptPaths.Length != 0)
            {
                var sqlConnection = new SqlConnection(ConnectionString);
                var server = new Server(new ServerConnection(sqlConnection));                
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    var filePath = scriptPaths[i];
                    string script = GetScriptText(filePath);                    
                    string databaseNameFromFirstRow = GetDatabaseNameFromFirstRow(script);
                    script = ModifyScripts(script, i == 0, databaseNameFromFirstRow);                  
                    try
                    {
                        server.ConnectionContext.ExecuteNonQuery(script);                        
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{ex.Message}\nОшибка при выполнении скрипта {Path.GetFileName(filePath)}.");
                    }
                    File.Delete(scriptPaths[i]);
                }
                MessageBox.Show("Обновление базы данных окончено.");
            }
            else
                MessageBox.Show("Скрипты не найдены.");
        }

        private string GetScriptText(string scriptPath)
        {
            using (var streamReader = new StreamReader(scriptPath))
            {
                return streamReader.ReadToEnd();
            }
        }

        private string ModifyScripts(string script, bool isInitialScript, string databaseNameFromFirstRow)
        {
            if (isInitialScript)
                return ModifyInitialScript(script, databaseNameFromFirstRow);
            else
            {
                if (!string.IsNullOrEmpty(databaseNameFromFirstRow))
                    return ModifyScript(script);
                return script;
            }
        }

        // Парсер "Database<любое количество пробелов(ЛКП)>=<ЛКП><искомое название БД без пробелов><ЛКП>;"
        private string GetDbNameFromConnectionString()
        {
            var regex = new Regex(@"(?<=Database\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);            
            var match = regex.Match(ConnectionString);
            return match.Value;
        }

        private string ModifyInitialScript(string script, string databaseNameFromFirstRow)
        {            
            if (string.IsNullOrEmpty(databaseNameFromFirstRow))
            {
                script = $"USE {DatabaseName}\nGO\n{script}";
                return script;
            }
            else
                return script.Replace(databaseNameFromFirstRow, DatabaseName);
        }

        // Если скрипт начинается с USE <dbName> GO, то убираем.
        private string ModifyScript(string script)
        {
            var regex = new Regex(@"\A\s*USE\s*\S+\s*\n*\s*GO", RegexOptions.IgnoreCase);
            var match = regex.Match(script);
            int matchValueLength = match.Value.Length;
            return script.Substring(matchValueLength);
        }        

        private string GetDatabaseNameFromFirstRow(string script)
        {
            var regex = new Regex(@"(?<=\A\s*USE\s*)\S+(?=\s*\n*\s*GO)", RegexOptions.IgnoreCase);            
            var match = regex.Match(script);            
            return match.Value;
        }
    }
}
