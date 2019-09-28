using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using SimpleDbUpdater.Realizations;
using SimpleDbUpdater.ViewModels;
using System;
using System.Collections.Generic;
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
        public ICommand SetScriptsFolderPath { get; }
        public ICommand ExecuteScripts { get; }
        public string ScriptsFolderPath
        {
            get => _scriptsFolderPath;
            set => SetProperty(ref _scriptsFolderPath, value); 
        }
        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
        }
        
        public MainViewModel()
        {
            ExecuteScripts = new RelayCommand(
                o => 
                {                    
                    var sqlFiles = GetSqlFilePaths();
                    try
                    {
                        ExecuteAndDeleteNonQueryScripts(sqlFiles);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }                    
                }, 
                x => !(string.IsNullOrEmpty(ScriptsFolderPath) || string.IsNullOrEmpty(ConnectionString)));

            SetScriptsFolderPath = new RelayCommand(o => ScriptsFolderPath = GetScriptsFolderPath());
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
                string dbName = GetDbNameFromConnectionString();
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    var filePath = scriptPaths[i];
                    string script;
                    using (var streamReader = new StreamReader(filePath))
                    {
                        script = streamReader.ReadToEnd();
                    }
                    script = ModifyScript(script, i, dbName);
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

        // Парсер "Database<любое количество пробелов(ЛКП)>=<ЛКП><искомое название БД без пробелов><ЛКП>;"
        private string GetDbNameFromConnectionString()
        {
            var regex = new Regex(@"(?<=Database\s*=\s*)\S+(?=\s*;)", RegexOptions.IgnoreCase);            
            var match = regex.Match(ConnectionString);
            return match.Value;
        }

        private string ModifyScript(string script, int scriptIndex, string newDbName)
        {
            string modifiedScript;
            if (scriptIndex == 0)
                modifiedScript = script.Replace("DatabaseName", newDbName);
            else
            {
                var neededChars = script.SkipWhile(c => c != '/').ToArray();
                modifiedScript = new string(neededChars);
            }
            return modifiedScript;
        }

        //private string ModifyFirstScript(string script)
        //{
        //    var 
        //}

        private bool IsBeginWithSpecifyingDatabaseName(string script)
        {
            var regex = new Regex(@"^\s*USE", RegexOptions.IgnoreCase);            
            var match = regex.Match(script);            
            return !string.IsNullOrEmpty(match.Value);
        }
    }
}
