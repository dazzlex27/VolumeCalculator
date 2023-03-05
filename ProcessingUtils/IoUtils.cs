using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProcessingUtils
{
	public static class IoUtils
	{
		private static readonly object CounterLock = new();
		
		public static async Task SerializeSettingsToFileAsync<T>(T settings, string filepath)
		{
			if (settings == null)
				return;

			var fileInfo = new FileInfo(filepath);
			if (!string.IsNullOrEmpty(fileInfo.DirectoryName))
				Directory.CreateDirectory(fileInfo.DirectoryName);
			
			var settingsText = JsonConvert.SerializeObject(settings);
			await File.WriteAllTextAsync(fileInfo.FullName, settingsText);
		}

		public static async Task<T> DeserializeSettingsFromFileAsync<T>(string filepath)
		{
			var fileInfo = new FileInfo(filepath);
			if (!fileInfo.Exists)
				return default;

			if (!string.IsNullOrEmpty(fileInfo.DirectoryName))
				Directory.CreateDirectory(fileInfo.DirectoryName);

			var settingsText = await File.ReadAllTextAsync(fileInfo.FullName);
			return JsonConvert.DeserializeObject<T>(settingsText);
		}

		public static int GetCurrentUniversalObjectCounter(string countersFileName)
		{
			lock (CounterLock)
			{
				if (!File.Exists(countersFileName))
				{
					File.WriteAllText(countersFileName, 0.ToString());
					return 0;
				}

				var fileContents = File.ReadAllText(countersFileName);
				var previousCounter = int.Parse(fileContents);

				return previousCounter;
			}
		}

		public static void IncrementUniversalObjectCounter(string countersFileName)
		{
			lock (CounterLock)
			{
				if (!File.Exists(countersFileName))
				{
					File.WriteAllText(countersFileName, 1.ToString());
					return;
				}

				var fileContents = File.ReadAllText(countersFileName);
				var previousCounter = int.Parse(fileContents);

				var nextCounter = previousCounter + 1;
				File.WriteAllText(countersFileName, nextCounter.ToString());
			}
		}

		public static void OpenFile(string filepath)
		{
			var process = new Process();
			process.StartInfo.FileName = filepath;
			process.StartInfo.UseShellExecute = true;
			process.Start();
		}

		public static void OpenFolder(string pathToFolder)
		{
			Process.Start("explorer.exe", pathToFolder);
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
