using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Primitives.Settings;

namespace Primitives
{
	public static class IoUtils
	{
		public static void SerializeSettings(ApplicationSettings settings)
		{
			Directory.CreateDirectory(GlobalConstants.AppConfigPath);
			if (settings == null)
				return;

			var settingsText = JsonConvert.SerializeObject(settings);
			File.WriteAllText(GlobalConstants.ConfigFileName, settingsText);
		}

		public static ApplicationSettings DeserializeSettings()
		{
			Directory.CreateDirectory(GlobalConstants.AppConfigPath);
			var configFile = GlobalConstants.ConfigFileName;
			if (!File.Exists(configFile))
				return null;

			var settingsText = File.ReadAllText(configFile);
			return JsonConvert.DeserializeObject<ApplicationSettings>(settingsText);
		}

		public static int GetCurrentUniversalObjectCounter()
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

		public static void IncrementUniversalObjectCounter()
		{
			if (!File.Exists(GlobalConstants.CountersFileName))
			{
				File.WriteAllText(GlobalConstants.CountersFileName, 1.ToString());
				return ;
			}

			var fileContents = File.ReadAllText(GlobalConstants.CountersFileName);
			var previousCounter = int.Parse(fileContents);

			var nextCounter = previousCounter + 1;
			File.WriteAllText(GlobalConstants.CountersFileName, nextCounter.ToString());
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

		public static bool NeedScalesDebugMode()
		{
			return File.Exists("SCDEBUG");
		}

		public static byte[] GetHwBytes()
		{
			return new byte[]
			{
				83, 69, 76, 69, 67, 84, 32, 42, 32, 70, 82, 79, 77, 32, 87, 105, 110, 51, 50, 95, 68, 105, 115, 107, 68,
				114, 105, 118, 101, 32, 87, 72, 69, 82, 69, 32, 73, 110, 116, 101, 114, 102, 97, 99, 101, 84, 121, 112,
				101, 61, 39, 73, 68, 69, 39
			};
		}
	}
}