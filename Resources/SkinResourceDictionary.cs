using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleDbUpdater.Resources
{
    class SkinResourceDictionary : ResourceDictionary
    {
        private Uri _lightTheme;
        private Uri _darkTheme;

        public Uri LightTheme
        {
            get => _lightTheme;
            set
            {
                _lightTheme = value;
                UpdateTheme();
            }
        }

        public Uri DarkTheme
        {
            get => _darkTheme;
            set
            {
                _darkTheme = value;
                UpdateTheme();
            }
        }

        private void UpdateTheme()
        {
            var newTheme = App.Theme == Theme.Light ? LightTheme : DarkTheme;
            if (newTheme != null && base.Source != newTheme)
                base.Source = newTheme;
        }
    }
}
