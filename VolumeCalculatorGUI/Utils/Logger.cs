using System;
using System.Diagnostics;
using System.IO;

namespace DepthMapProcessorGUI.Utils
{
    internal class Logger
    {
        private readonly string _filepath;

        public Logger()
        {
            var commonFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appName = Process.GetCurrentProcess().ProcessName;
            var currentInstanceFolder = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var folderPath = Path.Combine(commonFolderPath, appName, currentInstanceFolder);
            Directory.CreateDirectory(folderPath);
            _filepath = Path.Combine(folderPath, "main.log");
        }

        public void LogInfo(string info)
        {
            var time = DateTime.Now;
            using (var sw = File.AppendText(_filepath))
            {
                sw.WriteLine($"{time.ToShortDateString()} {time:HH:mm:ss.fff} INFO: {info}");
            }
        }

        public void LogError(string info)
        {
            var time = DateTime.Now;
            using (var sw = File.AppendText(_filepath))
            {
                sw.WriteLine($"{time.ToShortDateString()} {time:HH:mm:ss.fff} ERROR: {info}");
            }
        }

        public void LogException(string info, Exception ex)
        {
            var time = DateTime.Now;
            using (var sw = File.AppendText(_filepath))
            {
                sw.WriteLine($"{time.ToShortDateString()} {time:HH:mm:ss.fff} EXCEPTION: {info} {ex.Message} : {ex}");
            }
        }
    }
}