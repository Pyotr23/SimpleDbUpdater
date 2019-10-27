using SimpleDbUpdater.ViewModels;
using System;
using System.Configuration;
using System.Windows;

namespace SimpleDbUpdater
{
    public enum Theme { Light, Dark }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Theme Theme { get; set; } 
        
        public App()
        {
            bool.TryParse(ConfigurationManager.AppSettings[nameof(MainViewModel.IsDarkTheme)], out bool isDarkTheme);
            Theme = isDarkTheme ? Theme.Dark : Theme.Light;
        }       
    }
}
