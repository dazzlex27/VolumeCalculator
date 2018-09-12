using System.Windows.Media;

namespace VolumeCalculatorGUI.Utils
{
	internal static class Constants
	{
		public static readonly int BytesPerPixel24 = (PixelFormats.Bgr24.BitsPerPixel + 7) / 8;

		public static readonly int BytesPerPixel32 = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
	}
}