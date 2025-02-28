using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator
{
    internal class FileLogger : ILogger
    {
        private readonly string _logDirectory;

        public FileLogger(string logDirectory = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(_logDirectory);
        }

        public void LogInfo(string message)
        {
            WriteToLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteToLog("WARNING", message);
        }

        public void LogError(string message)
        {
            WriteToLog("Error", message);
        }

        private void WriteToLog(string level, string message)
        {
            try
            {
                string logFile = Path.Combine(_logDirectory, $"vrr_generator_{DateTime.Now:yyyyMMdd}.log");
                using (StreamWriter writer = File.AppendText(logFile))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] - {message}");
                }
            }
            catch
            {
                // Fail silently
            }
        }
    }
}
