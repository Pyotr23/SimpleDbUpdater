using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using SimpleDbUpdater.Loggers;
using SimpleDbUpdater.ViewModels;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace SimpleDbUpdater.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            string majorVersion = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString();
            string minorVersion = Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
            Title = $"Обновление БД {majorVersion}.{minorVersion}";
            Closing += LogRecordAboutClosing;
        }   
        
        private void LogRecordAboutClosing(object sender, CancelEventArgs e)
        {
            UpdaterLogger.Instance.Information("Программа закрывается.");
        }
    }
}
