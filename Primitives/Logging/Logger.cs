using System;
using System.IO;

namespace Primitives.Logging
{
	public class Logger : ILogger
	{
		private const string LogFileName = "main.log";
		private const string AchivedLogFileName = LogFileName + "1";
		private const int MaxLogSizeBytes = 5 * 1000 * 1000;

		private readonly string _filePath;
		private readonly string _archiveFilePath;
		private readonly object _writeLock;

		public Logger()
		{
			_writeLock = new object();

			var currentInstanceFolder = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
			var logDirectory = Path.Combine(GlobalConstants.AppLogsPath, currentInstanceFolder);
			Directory.CreateDirectory(logDirectory);
			_filePath = Path.Combine(logDirectory, LogFileName);
			_archiveFilePath = Path.Combine(logDirectory, AchivedLogFileName);
		}

		public void LogInfo(string message)
		{
			WriteMessageToLog("INFO", message);
		}

		public void LogError(string message)
		{
			WriteMessageToLog("ERROR", message);
		}

		public void LogException(string message, Exception ex)
		{
			WriteMessageToLog("EXCEPTION", $"{message} {ex}");
			Console.Beep();
		}

		public void LogDebug(string message)
		{
			WriteMessageToLog("DEBUG", message);
		}

		private void WriteMessageToLog(string type, string message)
		{
			lock (_writeLock)
			{
				var time = DateTime.Now;
				using (var sw = File.AppendText(_filePath))
				{
					sw.WriteLine($"{time.ToShortDateString()} {time:HH:mm:ss.fff} {type}: {message}");
					sw.Flush();
				}

				var logFileInfo = new FileInfo(_filePath);
				var needToArchiveLogFile = logFileInfo.Length > MaxLogSizeBytes;
				if (!needToArchiveLogFile)
					return;

				ArchiveLogFile();
			}
		}

		private void ArchiveLogFile()
		{
			try
			{
				var additionalLogFileInfo = new FileInfo(_archiveFilePath);
				if (additionalLogFileInfo.Exists)
					File.Delete(_archiveFilePath);

				File.Move(_filePath, _archiveFilePath);
			}
			catch (Exception ex)
			{
				LogException("Failed to archive log file", ex);
			}
		}
	}
}