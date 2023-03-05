using Primitives;
using ProcessingUtils;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class ImageUtilsTest
	{
		[Test]
		public void CtorImageData_WhenGivenOnlyWidthAndHeight_ReturnsImageDataWithDataAllocatedCorrectly()
		{
			const int gtWidth = 40;
			const int gtHeight = 30;
			const int gtBytesPerPixel = 3;
			const int gtDataLength = gtWidth * gtHeight * gtBytesPerPixel;

			var imageData = new ImageData(gtWidth, gtHeight, gtBytesPerPixel);

			Assert.That(imageData.Data, Has.Length.EqualTo(gtDataLength));
		}

		[Test]
		public void CopyCtorImageData_WhenGivenValidImageData_ReturnsDeepCopiedImageData()
		{
			const int gtWidth = 40;
			const int gtHeight = 30;
			const int gtBytesPerPixel = 3;
			const int gtDataLength = gtWidth * gtHeight * gtBytesPerPixel;
			var gtImageData = new ImageData(gtWidth, gtHeight, gtBytesPerPixel);

			var copyImageData = new ImageData(gtImageData);

			Assert.That(copyImageData, Is.Not.SameAs(gtImageData));
			Assert.That(copyImageData.Data, Is.Not.SameAs(gtImageData.Data));
			Assert.Multiple(() =>
			{
				Assert.That(copyImageData.Width, Is.EqualTo(gtWidth));
				Assert.That(copyImageData.Height, Is.EqualTo(gtHeight));
				Assert.That(copyImageData.BytesPerPixel, Is.EqualTo(gtBytesPerPixel));
				Assert.That(copyImageData.Data, Has.Length.EqualTo(gtDataLength));
			});
		}

		[Test]
		public async Task ReadImageDataFromFileAsync_WhenGivenValidImageFile_ReturnsValidImageData()
		{
			const string filepath = "data/frames/color/0.jpg";
			const int gtWidth = 960;
			const int gtHeight = 540;
			const int gtBytesPerPixel = 3;
			const int gtDataLength = gtWidth * gtHeight * gtBytesPerPixel;

			var imageData = await ImageUtils.ReadImageDataFromFileAsync(filepath);

			Assert.That(imageData, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(imageData.Width, Is.EqualTo(gtWidth));
				Assert.That(imageData.Height, Is.EqualTo(gtHeight));
				Assert.That(imageData.BytesPerPixel, Is.EqualTo(gtBytesPerPixel));
				Assert.That(imageData.Data, Has.Length.EqualTo(gtDataLength));
			});
		}

		[Test]
		public async Task SaveImageDataToFileAsync_WhenCreatedBlankImage_SavesBlankImage()
		{
			const string filepath = "data/frames/temp.png";
			const int gtWidth = 90;
			const int gtHeight = 160;
			const int gtBytesPerPixel = 3;
			const int gtDataLength = gtWidth * gtHeight * gtBytesPerPixel;

			var imageData = new ImageData(gtWidth, gtHeight, gtBytesPerPixel);
			await ImageUtils.SaveImageDataToFileAsync(imageData, filepath);
			var imageDataFromFile = await ImageUtils.ReadImageDataFromFileAsync(filepath);

			Assert.That(imageDataFromFile, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(imageDataFromFile.Width, Is.EqualTo(gtWidth));
				Assert.That(imageDataFromFile.Height, Is.EqualTo(gtHeight));
				Assert.That(imageDataFromFile.BytesPerPixel, Is.EqualTo(gtBytesPerPixel));
				Assert.That(imageDataFromFile.Data, Has.Length.EqualTo(gtDataLength));
			});
		}

		[Test]
		public async Task GetBase64StringFromImageDataAsync_WhenGivenValidImage_ReturnsCorrectBase64String()
		{
			const string rootFolder = "data/frames/color/";
			string imageFilepath = Path.Combine(rootFolder, "1.png");
			string gtBase64Filepath = Path.Combine(rootFolder, "1_base64.txt");

			var image = await ImageUtils.ReadImageDataFromFileAsync(imageFilepath);
			var base64 = await ImageUtils.GetBase64StringFromImageDataAsync(image);
			var gtBase64 = await File.ReadAllTextAsync(gtBase64Filepath);

			Assert.That(base64, Is.EqualTo(gtBase64.Trim()));
		}

		[Test]
		public async Task GetImageDataFromBase64StringAsync_WhenGivenValidBase64String_ReturnsValidImage()
		{
			const string rootFolder = "data/frames/color/";
			string gtImageFilepath = Path.Combine(rootFolder, "1.png");
			string base64Filepath = Path.Combine(rootFolder, "1_base64.txt");

			var gtImageData = await ImageUtils.ReadImageDataFromFileAsync(gtImageFilepath);
			var base64 = await File.ReadAllTextAsync(base64Filepath);
			var imageData = await ImageUtils.GetImageDataFromBase64StringAsync(base64);

			Assert.That(imageData, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(imageData.Width, Is.EqualTo(gtImageData.Width));
				Assert.That(imageData.Height, Is.EqualTo(gtImageData.Height));
				Assert.That(imageData.BytesPerPixel, Is.EqualTo(gtImageData.BytesPerPixel));
				Assert.That(imageData.Data, Has.Length.EqualTo(gtImageData.Data.Length));
			});
		}
	}
}
