using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Primitives;

namespace ProcessingUtils
{
	public static class IoUtils
	{
		private static readonly object CounterLock = new object();
		
		public static async Task SerializeSettingsAsync<T>(T settings)
		{
			var logFileInfo = new FileInfo(GlobalConstants.ConfigFileName);
			if (!string.IsNullOrEmpty(logFileInfo.DirectoryName))
				Directory.CreateDirectory(logFileInfo.DirectoryName);
			
			if (settings == null)
				return;

			var settingsText = JsonConvert.SerializeObject(settings);
			await File.WriteAllTextAsync(GlobalConstants.ConfigFileName, settingsText);
		}

		public static async Task<T> DeserializeSettingsAsync<T>()
		{
			var configFileInfo = new FileInfo(GlobalConstants.ConfigFileName);
			if (!string.IsNullOrEmpty(configFileInfo.DirectoryName))
				Directory.CreateDirectory(configFileInfo.DirectoryName);
			
			if (!configFileInfo.Exists)
				return default;

			var settingsText = await File.ReadAllTextAsync(configFileInfo.FullName);
			return JsonConvert.DeserializeObject<T>(settingsText);
		}

		public static int GetCurrentUniversalObjectCounter()
		{
			lock (CounterLock)
			{
				if (!File.Exists(GlobalConstants.CountersFileName))
				{
					File.WriteAllText(GlobalConstants.CountersFileName, 0.ToString());
					return 0;
				}

				var fileContents = File.ReadAllText(GlobalConstants.CountersFileName);
				var previousCounter = int.Parse(fileContents);

				return previousCounter;
			}
		}

		public static void IncrementUniversalObjectCounter()
		{
			lock (CounterLock)
			{
				if (!File.Exists(GlobalConstants.CountersFileName))
				{
					File.WriteAllText(GlobalConstants.CountersFileName, 1.ToString());
					return;
				}

				var fileContents = File.ReadAllText(GlobalConstants.CountersFileName);
				var previousCounter = int.Parse(fileContents);

				var nextCounter = previousCounter + 1;
				File.WriteAllText(GlobalConstants.CountersFileName, nextCounter.ToString());
			}
		}

		public static void OpenFile(string filepath)
		{
			Process.Start(filepath);
		}

		public static bool IsProcessRunning(string processName)
		{
			var runningProcesses = Process.GetProcesses();
			var matchingProcesses = runningProcesses
					.Where(p => p.ProcessName.ToLowerInvariant() == processName.ToLowerInvariant()).ToList();

			return matchingProcesses.Count > 0;
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

		public static string GetHostName()
		{
			return Dns.GetHostName();
		}

		public static async Task<IReadOnlyList<string>> GetLocalIpAddressesAsync()
		{
			var addresses = new List<string>();

			var localIPs = await Dns.GetHostAddressesAsync(Dns.GetHostName());
			foreach (IPAddress addr in localIPs)
			{
				if (addr.AddressFamily == AddressFamily.InterNetwork)
					addresses.Add(addr.ToString());
			}

			return addresses;
		}

		public static void StartProcess(string processPath, bool asAdmin)
		{
			var process = new ProcessStartInfo(processPath)
			{
				UseShellExecute = true,
				Verb = asAdmin ? "runas" : ""
			};

			Process.Start(process);
		}
	}
}
