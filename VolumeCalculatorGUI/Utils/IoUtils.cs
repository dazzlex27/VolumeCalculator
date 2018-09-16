using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Common;
using Newtonsoft.Json;
using VolumeCalculatorGUI.Entities;
using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Utils
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

		public static Bitmap CreateBitmapFromImageData(ImageData image)
		{
			var format = PixelFormat.Canonical;
			switch (image.BytesPerPixel)
			{
				case 3:
					format = PixelFormat.Format24bppRgb;
					break;
				case 4:
					format = PixelFormat.Format32bppArgb;
					break;
			}

			var bitmap = new Bitmap(image.Width, image.Height, format);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

			Marshal.Copy(image.Data, 0, bmpData.Scan0, image.Data.Length);

			bitmap.UnlockBits(bmpData);

			return bitmap;
		}

		public static Bitmap CreateBitmapFromDepthMap(DepthMap map, short minDepth, short maxDepth, short cutOffDepth)
		{
			var bitmap = new Bitmap(map.Width, map.Height, PixelFormat.Format24bppRgb);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

			var data = DepthMapUtils.GetColorizedDepthMapData(map, minDepth, maxDepth, cutOffDepth);
			Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

			bitmap.UnlockBits(bmpData);

			return bitmap;
		}
	}
}