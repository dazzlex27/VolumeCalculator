using Primitives;
using ProcessingUtils;
using SixLabors.ImageSharp;

namespace DmConverter
{
	class Program
	{
		const int MinDepth = 600;
		const int MaxDepth = 10000;

		static void Main(string[] args)
		{
			TestDmLoading();
			TestDmCulling();
			TestImageLoading();
		}

		private static void TestDmLoading()
		{
			var dm = DepthMapUtils.ReadDepthMapFromRawFile("0.dm");

			var colorizedData = DepthMapUtils.GetGrayscaleDepthMapDataBgr(dm, MinDepth, MaxDepth);
			var image = new ImageData(dm.Width, dm.Height, colorizedData, 1);
			ImageUtils.SaveImageDataToFile(image, "0.png");
		}

		private static void TestDmCulling()
		{
			var dm = DepthMapUtils.ReadDepthMapFromRawFile("0.dm");
			DepthMapUtils.SaveDepthMapImageToFile(dm, "1.png", MinDepth, 750, MaxDepth);
		}

		private static void TestImageLoading()
		{
			var image = Image.Load("1.png");
			var imageData = ImageUtils.GetImageDataFromImage(image);
			ImageUtils.SaveImageDataToFile(imageData, "2.png");

		}
	}
}
