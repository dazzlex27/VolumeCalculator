using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Primitives;

namespace ProcessingUtils
{
	public static class IoUtils
	{
		private static readonly object CounterLock = new object();
		
		public static void SerializeSettings<T>(T settings)
		{
			var logFileInfo = new FileInfo(GlobalConstants.ConfigFileName);
			if (!string.IsNullOrEmpty(logFileInfo.DirectoryName))
				Directory.CreateDirectory(logFileInfo.DirectoryName);
			
			if (settings == null)
				return;

			var settingsText = JsonConvert.SerializeObject(settings);
			File.WriteAllText(GlobalConstants.ConfigFileName, settingsText);
		}

		public static T DeserializeSettings<T>()
		{
			var configFileInfo = new FileInfo(GlobalConstants.ConfigFileName);
			if (!string.IsNullOrEmpty(configFileInfo.DirectoryName))
				Directory.CreateDirectory(configFileInfo.DirectoryName);
			
			if (!configFileInfo.Exists)
				return default;

			var settingsText = File.ReadAllText(configFileInfo.FullName);
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

		public static IReadOnlyList<string> GetLocalIpAddresses()
		{
			var addresses = new List<string>();

			var localIPs = Dns.GetHostAddresses(Dns.GetHostName());
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

		public static byte[] GetL()
		{
			// Path to license file: C:/Program Files/MOXA/USBDriver/v2.txt
			return new byte[] { 67, 58, 47, 80, 114, 111, 103, 114, 97, 109, 32, 70, 105, 108, 101, 115, 47, 77, 79,
				88, 65, 47, 85, 83, 66, 68, 114, 105, 118, 101, 114, 47, 118, 50, 46, 116, 120, 116 };
		}
	}
}