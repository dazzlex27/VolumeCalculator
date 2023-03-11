using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VCClient.ViewModels;

namespace VolumeCalculatorTests.GUI
{
	[TestFixture]
	internal class StreamViewControlVmTest
	{
		ILogger _logger;

		[SetUp]
		public void SetUp()
		{
			_logger = new DummyLogger();
		}

		[TearDown]
		public void TearDown()
		{
			_logger?.Dispose();
		}

		[Test]
		public void Ctor_WhenCreated_DoesntThrow()
		{
			_ = new StreamViewControlVm(_logger);
		}

		[TestCase(1)]
		[TestCase(3)]
		[TestCase(4)]
		public void UpdateColorImage_WhenGivenValidFrameWithDifferentBpp_CreatesImageWithProperBpp(byte gtBytesPerPixel)
		{
			var image = new ImageData(40, 30, gtBytesPerPixel);
			var vm = new StreamViewControlVm(_logger);

			vm.UpdateColorImage(image);

			Assert.That((byte)(vm.ColorImageBitmap.Format.BitsPerPixel / 8), Is.EqualTo(gtBytesPerPixel));
		}

		[TestCase(40, 30)]
		[TestCase(30, 40)]
		[TestCase(1, 1)]
		public void UpdateColorImage_WhenGivenValidFrameWithDifferentParams_CreatesImageWithProperParams(
			int gtWidth, int gtHeight)
		{
			var image = new ImageData(gtWidth, gtHeight, 1);
			var vm = new StreamViewControlVm(_logger);

			vm.UpdateColorImage(image);

			Assert.Multiple(() =>
			{
				Assert.That(vm.ColorImageBitmap.Width, Is.EqualTo(gtWidth));
				Assert.That(vm.ColorImageBitmap.Height, Is.EqualTo(gtHeight));
			});
		}

		[Test]
		public void UpdateColorImage_WhenGivenValidImageData_CreatesProperImage()
		{
			var gtWidth = 3;
			var gtHeight = 2;
			var gtData = new byte[] { 124, 75, 93, 11, 4, 227 };
			byte gtBytesPerPixel = 1;

			var imageData = new ImageData(gtWidth, gtHeight, gtData, gtBytesPerPixel);
			var vm = new StreamViewControlVm(_logger);
			vm.UpdateColorImage(imageData);

			var bmp = vm.ColorImageBitmap;
			byte[] data = new byte[(int)bmp.Width * (int)bmp.Height];
			bmp.CopyPixels(data, gtWidth, 0);
			Assert.That(data, Has.Length.EqualTo(gtData.Length));
			Assert.Multiple(() =>
			{
				Assert.That(bmp.Width, Is.EqualTo(gtWidth));
				Assert.That(bmp.Height, Is.EqualTo(gtHeight));
				Assert.That(Enumerable.SequenceEqual(data, imageData.Data), Is.True);
			});
		}

		[TestCase(40, 30)]
		[TestCase(30, 40)]
		[TestCase(1, 1)]
		public void UpdateDepthImage_WhenGivenValidFrameWithDifferentParams_CreatesImageWithProperParams(
			int gtWidth, int gtHeight)
		{
			var image = new DepthMap(gtWidth, gtHeight);
			var vm = new StreamViewControlVm(_logger);

			vm.UpdateDepthImage(image);

			Assert.Multiple(() =>
			{
				Assert.That(vm.DepthImageBitmap.Width, Is.EqualTo(gtWidth));
				Assert.That(vm.DepthImageBitmap.Height, Is.EqualTo(gtHeight));
			});
		}

		[Test]
		public void UpdateDepthImage_WhenGivenValidDepthMap_CreatesProperImage()
		{
			short gtFloorDepth = 55;
			short gtMinObjectHeight = 15;
			short gtMinDepth = 5;
			var gtWidth = 3;
			var gtHeight = 3;
			var gtData = new short[] { 8, 0, 0, 39, 0, 27, 0, 35, 15 };
			var settings = AlgorithmSettings.GetDefaultSettings();
			settings.WorkArea.FloorDepth = gtFloorDepth;
			settings.WorkArea.MinObjectHeight = gtMinObjectHeight;

			var depthMap = new DepthMap(gtWidth, gtHeight, gtData);
			var depthMapImage = DepthMapUtils.GetColorizedDepthMapData(depthMap, gtMinDepth, gtFloorDepth);
			var vm = new StreamViewControlVm(_logger);
			vm.UpdateSettings(settings);
			vm.UpdateMinDepth(gtMinDepth);
			vm.UpdateDepthImage(depthMap);

			var bmp = vm.DepthImageBitmap;
			byte[] data = new byte[(int)bmp.Width * (int)bmp.Height];
			bmp.CopyPixels(data, gtWidth, 0);
			Assert.That(data, Has.Length.EqualTo(gtData.Length));
			Assert.Multiple(() =>
			{
				Assert.That(bmp.Width, Is.EqualTo(gtWidth));
				Assert.That(bmp.Height, Is.EqualTo(gtHeight));
				Assert.That(Enumerable.SequenceEqual(data, depthMapImage.Data), Is.True);
			});
		}

		[Test]
		public void UpdateSettings_WhenAssignedUseColorMask_SetsProperty()
		{
			var gtUseMask = true;
			var settings = AlgorithmSettings.GetDefaultSettings();
			settings.WorkArea.UseColorMask = gtUseMask;
			var vm = new StreamViewControlVm(_logger);

			vm.UpdateSettings(settings);

			Assert.That(vm.UseColorMask, Is.EqualTo(gtUseMask));
		}

		[Test]
		public void UpdateSettings_WhenAssignedUseDepthMask_SetsProperty()
		{
			var gtUseMask = true;
			var settings = AlgorithmSettings.GetDefaultSettings();
			settings.WorkArea.UseDepthMask = gtUseMask;
			var vm = new StreamViewControlVm(_logger);

			vm.UpdateSettings(settings);

			Assert.That(vm.UseDepthMask, Is.EqualTo(gtUseMask));
		}

		[Test]
		public void UpdateSettings_WhenAssignedColorMask_SetsProperty()
		{
			var settings = AlgorithmSettings.GetDefaultSettings();
			var list = new List<RelPoint> { new RelPoint(0.1, 0.1), new RelPoint(0.1, 0.3), new RelPoint(0.3, 0.3) };
			var polygon = settings.WorkArea.ColorMaskContour = list;
			settings.WorkArea.ColorMaskContour = polygon;
			settings.WorkArea.UseColorMask = true;

			var vm = new StreamViewControlVm(_logger);
			vm.UpdateSettings(settings);

			Assert.That(vm.ColorMaskPolygonControlVm.PolygonPoints, Has.Count.EqualTo(list.Count));
		}

		[Test]
		public void UpdateSettings_WhenAssignedDepthMask_SetsProperty()
		{
			var settings = AlgorithmSettings.GetDefaultSettings();
			var list = new List<RelPoint> { new RelPoint(0.1, 0.1), new RelPoint(0.1, 0.3), new RelPoint(0.3, 0.3) };
			var polygon = settings.WorkArea.DepthMaskContour = list;
			settings.WorkArea.DepthMaskContour = polygon;
			settings.WorkArea.UseDepthMask = true;

			var vm = new StreamViewControlVm(_logger);
			vm.UpdateSettings(settings);

			Assert.That(vm.DepthMaskPolygonControlVm.PolygonPoints, Has.Count.EqualTo(list.Count));
		}
	}
}
