using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDbUpdater.Events
{
    class NewProgressBarValueEventsArgs : EventArgs
    {
        public int NewValue { get; }

        public NewProgressBarValueEventsArgs(int newValue)
        {
            NewValue = newValue;
        }
    }
}
