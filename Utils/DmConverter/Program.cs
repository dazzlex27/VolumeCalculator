using Primitives;
using ProcessingUtils;
using SixLabors.ImageSharp;
using System.Threading.Tasks;

namespace DmConverter
{
	internal class Program
	{
		const int MinDepth = 600;
		const int MaxDepth = 10000;

		static async Task Main(string[] args)
		{
			await TestDmLoading();
			await TestDmCulling();
		}

		private static async Task TestDmLoading()
		{
			var dm = await DepthMapUtils.ReadDepthMapFromRawFileAsync("0.dm");
			var imageData = DepthMapUtils.GetGrayscaleImageDataFromfDepthMap(dm, MinDepth, MaxDepth);
			await imageData.SaveAsync("0.png");
		}

		private static async Task TestDmCulling()
		{
			var dm = await DepthMapUtils.ReadDepthMapFromRawFileAsync("0.dm");
			await DepthMapUtils.SaveDepthMapImageToFile(dm, "1.png", MinDepth, MaxDepth, 750);
		}
	}
}
