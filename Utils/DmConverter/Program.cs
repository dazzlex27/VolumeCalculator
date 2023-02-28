using Primitives;
using ProcessingUtils;

namespace DmConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			var dm = DepthMapUtils.ReadDepthMapFromRawFile("0.dm");
			var colorizedData = DepthMapUtils.GetColorizedDepthMapDataBgr(dm, 600, 2000);
			var image = new ImageData(dm.Width, dm.Height, colorizedData, 3);
			ImageUtils.SaveImageDataToFile(image, "0.png");
		}
	}
}
