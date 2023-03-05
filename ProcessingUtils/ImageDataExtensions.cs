using Primitives;
using System.Threading.Tasks;

namespace ProcessingUtils
{
	public static class ImageDataExtensions
	{
		public static async Task SaveAsync(this ImageData imageData, string filepath)
		{
			await ImageUtils.SaveImageDataToFileAsync(imageData, filepath);
		}
	}
}
