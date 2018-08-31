using Newtonsoft.Json;
using System.IO;

namespace VolumeCheckerGUI
{
	internal static class IoUtils
	{
		private const string ConfigFileName = "settings.cfg";

		public static void SerializeSettings(ApplicationSettings settings)
		{
			if (settings == null)
				return;

			var settingsText = JsonConvert.SerializeObject(settings);

			File.WriteAllText(ConfigFileName, settingsText);
		}

		public static ApplicationSettings DeserializeSettings()
		{
			if (!File.Exists(ConfigFileName))
				return null;

			var settingsText = File.ReadAllText(ConfigFileName);
			return JsonConvert.DeserializeObject<ApplicationSettings>(settingsText);
		}
	}
}