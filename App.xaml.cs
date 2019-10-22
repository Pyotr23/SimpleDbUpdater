using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleDbUpdater
{
    public enum Skin { Light, Dark }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Skin Skin { get; set; } = Skin.Light;

        public void ChangeSkin(Skin newSkin)
        {
            Skin = newSkin;

            foreach (ResourceDictionary dictionary in Resources.MergedDictionaries)
            {
                if (dictionary is SkinResourceDictionary)
            }
        }
    }
}
