using SimpleDbUpdater.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleDbUpdater.Managers
{
    class ProgressBarManager
    {
        private static ProgressBarManager _instance;

        public delegate void ProgressBarManagerHandler(object sender, NewProgressBarValueEventsArgs e);
        public event ProgressBarManagerHandler NewProgressBarValue;

        private ProgressBarManager()
        {

        }

        public static ProgressBarManager Instance
        {
            get 
            {
                if (_instance == null)
                    _instance = new ProgressBarManager();
                return _instance;
            }                            
        } 
        
        public void ChangeProgressBarValue(int newValue)
        {
            NewProgressBarValue?.Invoke(this, new NewProgressBarValueEventsArgs(newValue));
        }
    }
}
