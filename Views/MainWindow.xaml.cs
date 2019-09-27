using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using SimpleDbUpdater.ViewModels;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
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
        }                 
    }
}
