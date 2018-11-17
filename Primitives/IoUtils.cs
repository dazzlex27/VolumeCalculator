using System.IO;
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

		public static int GetNextUniversalObjectCounter()
		{
			if (!File.Exists(Constants.CountersFileName))
			{
				File.WriteAllText(Constants.CountersFileName, 0.ToString());
				return 0;
			}

			var fileContents = File.ReadAllText(Constants.CountersFileName);
			var previousCounter = int.Parse(fileContents);
			var nextCounter = previousCounter + 1;
			File.WriteAllText(Constants.CountersFileName, nextCounter.ToString());

			return nextCounter;
		}
	}
}