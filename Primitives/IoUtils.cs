using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Primitives
{
	public static class IoUtils
	{
		public static void SerializeSettings(ApplicationSettings settings)
		{
			if (settings == null)
				return;

			var settingsText = JsonConvert.SerializeObject(settings);

			File.WriteAllText(Constants.ConfigFileName, settingsText);
		}

		public static ApplicationSettings DeserializeSettings()
		{
			if (!File.Exists(Constants.ConfigFileName))
				return null;

			var settingsText = File.ReadAllText(Constants.ConfigFileName);
			return JsonConvert.DeserializeObject<ApplicationSettings>(settingsText);
		}

		public static string ReadScannerPort()
		{
			if (!File.Exists(Constants.PortsFileName))
				return string.Empty;

			var fileLines = File.ReadAllLines(Constants.PortsFileName);
			return fileLines.Length == 0 ? string.Empty : fileLines[0];
		}

		public static string ReadScalesPort()
		{
			if (!File.Exists(Constants.PortsFileName))
				return string.Empty;

			var fileLines = File.ReadAllLines(Constants.PortsFileName);
			return fileLines.Length < 2 ? string.Empty : fileLines[1];
		}

		public static string ReadIoBoardPort()
		{
			if (!File.Exists(Constants.PortsFileName))
				return string.Empty;

			var fileLines = File.ReadAllLines(Constants.PortsFileName);
			return fileLines.Length < 3 ? string.Empty : fileLines[2];
		}

		public static int GetCurrentUniversalObjectCounter()
		{
			if (!File.Exists(Constants.CountersFileName))
			{
				File.WriteAllText(Constants.CountersFileName, 0.ToString());
				return 0;
			}

			var fileContents = File.ReadAllText(Constants.CountersFileName);
			var previousCounter = int.Parse(fileContents);

			return previousCounter;
		}

		public static void IncrementUniversalObjectCounter()
		{
			if (!File.Exists(Constants.CountersFileName))
			{
				File.WriteAllText(Constants.CountersFileName, 1.ToString());
				return ;
			}

			var fileContents = File.ReadAllText(Constants.CountersFileName);
			var previousCounter = int.Parse(fileContents);

			var nextCounter = previousCounter + 1;
			File.WriteAllText(Constants.CountersFileName, nextCounter.ToString());
		}

		public static void OpenFile(string filepath)
		{
			Process.Start(filepath);
		}

		public static bool KillProcess(string processName)
		{
			try
			{
				var runningProcesses = Process.GetProcesses();
				var matchingProcesses = runningProcesses
					.Where(p => p.ProcessName.ToLowerInvariant().Contains(processName.ToLowerInvariant())).ToList();
				foreach (var process in matchingProcesses)
					process.Kill();

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static void ShutPcDown()
		{
			var shutDownProcess = new ProcessStartInfo("shutdown", "/s /t 0")
			{
				CreateNoWindow = true,
				UseShellExecute = false
			};

			Process.Start(shutDownProcess);
		}
	}
}