using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDbUpdater.Loggers
{
    class UpdaterLogger
    {
        private const string LogExtension = "log";
        private static Logger _instance;        
        private static LogEventLevel _logEventLevel = LogEventLevel.Information;
        private static LoggingLevelSwitch _loggingLevel = new LoggingLevelSwitch(_logEventLevel);

        private UpdaterLogger() { }

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {                    
                    string loggerPath = GetLoggerPath();
                    _instance = new LoggerConfiguration().MinimumLevel.ControlledBy(_loggingLevel).WriteTo.File(loggerPath).CreateLogger();
                }
                return _instance;
            }
        }

        public LogEventLevel LogEventLevel 
        {
            get => _logEventLevel;
            set
            {
                _logEventLevel = value;
                _loggingLevel.MinimumLevel = value;
            }
        }
        
        private static string GetLoggerPath()
        {
            string programFolder = AppDomain.CurrentDomain.BaseDirectory;
            string programFileName = AppDomain.CurrentDomain.FriendlyName;
            string programFullPath = Path.Combine(programFolder, programFileName);
            return Path.ChangeExtension(programFullPath, LogExtension);
        }
    }
}
