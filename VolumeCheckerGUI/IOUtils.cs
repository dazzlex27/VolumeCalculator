using Newtonsoft.Json;
using System.IO;

namespace VolumeCheckerGUI
{
	internal static class IOUtils
	{
		const string ConfigFileName = "settings.cfg";

		public static void SerializeSettings(CheckerSettings settings)
		{
			if (settings == null)
				return;

			var settingsText = JsonConvert.SerializeObject(settings);

			File.WriteAllText(ConfigFileName, settingsText);
		}

		public static CheckerSettings DeserializeSettings()
		{
			if (!File.Exists(ConfigFileName))
				return null;

			var settingsText = File.ReadAllText(ConfigFileName);
			return JsonConvert.DeserializeObject<CheckerSettings>(settingsText);
		}
	}
}