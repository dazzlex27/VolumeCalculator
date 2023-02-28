using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Primitives.Logging
{
	public sealed class TxtLogger : ILogger
	{
		private readonly ConcurrentQueue<LogData> _messageQueue;
		private readonly CancellationTokenSource _tokenSource;

		private const int MaxLogSizeBytes = 5 * 1000 * 1000;
		private static readonly DateTime StartupTime = DateTime.Now;

		private readonly string _filePath;
		private readonly string _archiveFilePath;

		public TxtLogger(string appName, string logName)
		{
			var logFileName = $"{logName}.log";
			var archivedLogFileName = $"{logFileName}1";
			var currentInstanceFolder = StartupTime.ToString("yyyy-MM-dd-HH-mm-ss");
			var logDirectory = Path.Combine(GlobalConstants.AppLogsPath, appName, currentInstanceFolder);

			Directory.CreateDirectory(logDirectory);
			_filePath = Path.Combine(logDirectory, logFileName);
			_archiveFilePath = Path.Combine(logDirectory, archivedLogFileName);

			_tokenSource = new CancellationTokenSource();
			_messageQueue = new ConcurrentQueue<LogData>();
			Task.Factory.StartNew(
				async (o) => await ProcessLoggingToFile(), TaskCreationOptions.LongRunning, _tokenSource.Token);
		}

		public void LogInfo(string message)
		{
			EnqueueMessage("INFO", message);
		}

		public void LogError(string message)
		{
			EnqueueMessage("ERROR", message);
		}

		public void LogException(string message, Exception ex)
		{
			EnqueueMessage("EXCEPTION", $"{message} {ex}");
			
			#if DEBUG
			Console.Beep();
			#endif
		}

		public void LogDebug(string message)
		{
			EnqueueMessage("DEBUG", message);
		}

		public void Dispose()
		{
			_tokenSource.Cancel();
			_tokenSource.Dispose();
		}

		private void EnqueueMessage(string type, string message)
		{
			_messageQueue.Enqueue(new LogData(type, message));
		}

		private async Task WriteMessageToLog(string type, string message)
		{
			var time = DateTime.Now;
			using (var sw = File.AppendText(_filePath))
			{
				await sw.WriteLineAsync($"{time.ToShortDateString()} {time:HH:mm:ss.fff} {type}: {message}");
				await sw.FlushAsync();
			}

			var logFileInfo = new FileInfo(_filePath);
			var needToArchiveLogFile = logFileInfo.Length > MaxLogSizeBytes;
			if (!needToArchiveLogFile)
				return;

			ArchiveLogFile();
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

		private async Task ProcessLoggingToFile()
		{
			try
			{
				while (!_tokenSource.IsCancellationRequested)
				{
					while (!_messageQueue.IsEmpty)
					{
						bool success = _messageQueue.TryDequeue(out LogData data);
						if (success)
							await WriteMessageToLog(data.Type, data.Message);
					}
					await Task.Delay(50);
				}
			}
			finally { }
		}
	}
}
