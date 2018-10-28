using System.IO;
using Newtonsoft.Json;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Utils
{
	internal static class IoUtils
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
	}
}