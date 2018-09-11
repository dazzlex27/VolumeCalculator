using System.IO;
using System.Windows.Media.Imaging;
using DepthMapProcessorGUI.Entities;
using Newtonsoft.Json;
using Common;
using System.Drawing;

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

		public static Bitmap CreateBitmapFromImageData(ImageData image)
		{
			System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Canonical;
			switch (image.BytesPerPixel)
			{
				case 3:
					format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
					break;
				case 4:
					format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
					break;
			}

			var bitmap = new Bitmap(image.Width, image.Height, format);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);

			System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bmpData.Scan0, image.Data.Length);

			bitmap.UnlockBits(bmpData);

			return bitmap;
		}

		public static Bitmap CreateBitmapFromDepthMap(DepthMap map, short minDepth, short maxDepth, short cutOffDepth)
		{
			var bitmap = new Bitmap(map.Width, map.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);

			var data = DepthMapUtils.GetColorizedDepthMapData(map, minDepth, maxDepth, cutOffDepth);
			System.Runtime.InteropServices.Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

			bitmap.UnlockBits(bmpData);

			return bitmap;
		}
	}
}