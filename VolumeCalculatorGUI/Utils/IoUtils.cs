using System.IO;
using System.Windows.Media.Imaging;
using DepthMapProcessorGUI.Entities;
using Newtonsoft.Json;

namespace DepthMapProcessorGUI.Utils
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

		public static void SaveWriteableBitmap(string filename, BitmapSource image5)
		{
			if (filename == string.Empty)
				return;

			using (var stream5 = new FileStream(filename, FileMode.Create))
			{
				var encoder5 = new PngBitmapEncoder();
				encoder5.Frames.Add(BitmapFrame.Create(image5));
				encoder5.Save(stream5);
			}
		}
	}
}