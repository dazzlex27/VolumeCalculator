using System.Windows.Media;

namespace DepthMapProcessorGUI.Utils
{
	internal static class Constants
	{
		public const float FovX = 86;

		public const float FovY = 57; 

		public const short MinDepth = 300;

		public static readonly int BytesPerPixel24 = (PixelFormats.Bgr24.BitsPerPixel + 7) / 8;
	}
}