using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.SqlServer;
using System.Windows;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace SimpleDbUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = tbxConnectionString.Text;            
            if (!string.IsNullOrEmpty(connectionString))
            {
                var sqlFiles = GetSqlFilePaths();
                try
                {
                    ExecuteAndDeleteNonQueryScripts(sqlFiles, connectionString);                    
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }           
            }
            else
                MessageBox.Show("Заполните все поля.");
        }

        private void ExecuteAndDeleteNonQueryScripts(string[] scriptPaths, string connectionString)
        {
            if (scriptPaths.Length != 0)
            {
                var sqlConnection = new SqlConnection(connectionString);
                var server = new Server(new ServerConnection(sqlConnection));
                string dbName = GetDbNameFromConnectionString(connectionString);                
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
                        File.Delete(scriptPaths[i]);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{ex.Message}\nОшибка при выполнении скрипта {Path.GetFileName(filePath)}.");
                    }
                }
                MessageBox.Show("Обновление базы данных окончено.");
            }
            else
                MessageBox.Show("Скрипты не найдены.");
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

        private string[] GetSqlFilePaths()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var fileFullPaths = Directory.GetFiles(currentDirectory);
            var fileNames = fileFullPaths.Select(f => new FileInfo(f).Name).ToArray();
            var sqlFileNames = fileNames.Where(s => new FileInfo(s).Extension == ".sql").ToArray();

            // Если скрипты именуются числом с постоянным количетством символов (001, 010, 205), то их пути создаются этой строчкой.
            var sortedSqlFilePathes = sqlFileNames.Select(n => Path.Combine(currentDirectory, n)).ToArray();

            // Если файлы именуются числом с переменным количеством символом (1_, 12_, 104_), то нужно переопределять сортировку так.
            //var sortedSqlFileNames = sqlFileNames.OrderBy(name => { return GetScriptNumber(name); }).ToArray();              
            //var sortedSqlFilePathes = sortedSqlFileNames.Select(n => Path.Combine(currentDirectory, n)).ToArray();
            return sortedSqlFilePathes;
        }

        // Парсер сработает, если перед названием БД нет пробела, сразу равно.
        private string GetDbNameFromConnectionString(string connectionString)
        {            
            string[] connectionSubstrings = connectionString.Split(new char[] { '=', ';', }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            int dbNameIndex = -1;
            for (int i = 0; i < connectionSubstrings.Length; i++)
            {
                if (connectionSubstrings[i].Contains("Database"))
                {
                    dbNameIndex = ++i;
                    break;
                }
            }
            return connectionSubstrings[dbNameIndex];
        }

        private int GetScriptNumber(string fileName)
        {
            var charsBeforeUnderscore = fileName.TakeWhile(c => c != '_').ToArray();
            string stringBeforeUnderscore = new string(charsBeforeUnderscore);
            int intBeforeUnderscore = int.Parse(stringBeforeUnderscore);
            return intBeforeUnderscore;
        }        
    }
}
