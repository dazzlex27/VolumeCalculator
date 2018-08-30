using System;
using System.IO;
using System.Diagnostics;
using static System.Environment;

namespace VolumeCheckerGUI
{
    internal class Logger
    {
        private string _filepath;

        public Logger()
        {
            var commonFolderPath = GetFolderPath(SpecialFolder.CommonApplicationData);
            var appName = Process.GetCurrentProcess().ProcessName;
            var currentInstanceFolder = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var folderPath = Path.Combine(commonFolderPath, appName, currentInstanceFolder);
            Directory.CreateDirectory(folderPath);
            _filepath = Path.Combine(folderPath, "log.txt");
        }

        public void LogInfo(string info)
        {
            var time = DateTime.Now;
            using (var sw = File.AppendText(_filepath))
            {
                sw.WriteLine($"{time.ToShortDateString()} {time.ToString("HH:mm:ss.fff")} INFO: {info}");
            }
        }

        public void LogError(string info)
        {
            var time = DateTime.Now;
            using (var sw = File.AppendText(_filepath))
            {
                sw.WriteLine($"{time.ToShortDateString()} {time.ToString("HH:mm:ss.fff")} ERROR: {info}");
            }
        }

        public void LogException(string info, Exception ex)
        {
            var time = DateTime.Now;
            using (var sw = File.AppendText(_filepath))
            {
                sw.WriteLine($"{time.ToShortDateString()} {time.ToString("HH:mm:ss.fff")} EXCEPTION: {info} {ex.Message} : {ex.ToString()}");
            }
        }
    }
}