using SimpleDbUpdater.Properties;
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
            bool isDarkTheme = (bool)Settings.Default["IsDarkTheme"];
            Theme = isDarkTheme ? Theme.Dark : Theme.Light;
        }       
    }
}
