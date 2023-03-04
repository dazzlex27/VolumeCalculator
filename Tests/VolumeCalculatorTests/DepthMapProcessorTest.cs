using FrameProcessor;
using Primitives;
using Primitives.Logging;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class DepthMapProcessorTest : IDisposable
	{
		private ILogger _logger;

		[SetUp]
		public void Setup()
		{
			_logger = new DummyLogger();
		}

		[TearDown]
		public void TearDown()
		{
			Dispose();
		}

		[Test]
		public void CalculateFloorDepth_WhenGivenAnEmpty4x3Map_ReturnsZero()
		{
			const int mapWidth = 4;
			const int mapHeight = 3;
			var mapData = new short[mapWidth * mapHeight];
			var emptyMap = new DepthMap(mapWidth, mapHeight, mapData);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var floorDepth = processor.CalculateFloorDepth(emptyMap);
			Assert.IsTrue(floorDepth == 0);
		}

		[Test]
		public void CalculateFloorDepth_WhenGivenAMapWithASingleValidDepthValue_ReturnOne()
		{
			const short validDepthValue = 1;
			const int mapWidth = 4;
			const int mapHeight = 3;
			var mapData = new short[mapWidth * mapHeight];
			mapData[0] = validDepthValue;
			var map = new DepthMap(mapWidth, mapHeight, mapData);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var floorDepth = processor.CalculateFloorDepth(map);
			Assert.IsTrue(floorDepth == validDepthValue);
		}

		[Test]
		public void CalculateFloorDepth_WhenGivenAMapWithAFewValidDepthValues_ReturnTheMode()
		{
			const short modeDepthValue = 1;
			const int mapWidth = 4;
			const int mapHeight = 3;
			var mapData = new short[mapWidth * mapHeight];
			mapData[0] = modeDepthValue;
			mapData[1] = modeDepthValue;
			mapData[2] = 2;
			var map = new DepthMap(mapWidth, mapHeight, mapData);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var floorDepth = processor.CalculateFloorDepth(map);
			Assert.IsTrue(floorDepth == modeDepthValue);
		}

		[Test]
		public void SelectAlgorithm_WhenMapAndImageAreEmpty_ReturnsNoObjectFoundResult()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var data = new AlgorithmSelectionData(map, image, 0, true, true, true, "");
			var algorithmSelectionResult = processor.SelectAlgorithm(data);
			Assert.IsTrue(algorithmSelectionResult.Status == AlgorithmSelectionStatus.NoObjectFound);
		}

		[Test]
		public void SelectAlgorithm_WhenNoModeIsAvailable_ReturnsNoModesAreAvailable()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var data = new AlgorithmSelectionData(map, image, 0, false, false, false, "");
			var algorithmSelectionResult = processor.SelectAlgorithm(data);
			Assert.IsTrue(algorithmSelectionResult.Status == AlgorithmSelectionStatus.NoAlgorithmsAllowed);
		}

		[Test]
		public void SelectAlgorithm_WhenOnlyDm1IsAvailable_ReturnsNoObjectFound()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var data = new AlgorithmSelectionData(map, image, 0, true, false, false, "");
			var algorithmSelectionResult = processor.SelectAlgorithm(data);
			Assert.IsTrue(algorithmSelectionResult.Status == AlgorithmSelectionStatus.NoObjectFound);
		}

		[Test]
		public void SelectAlgorithm_WhenOnlyDm2IsAvailable_ReturnsNoObjectFound()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var data = new AlgorithmSelectionData(map, image, 0, false, true, false, "");
			var algorithmSelectionResult = processor.SelectAlgorithm(data);
			Assert.IsTrue(algorithmSelectionResult.Status == AlgorithmSelectionStatus.NoObjectFound);
		}

		[Test]
		public void SelectAlgorithm_WhenOnlyRgbIsAvailable_ReturnsNoObjectFound()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var data = new AlgorithmSelectionData(map, image, 0, false, false, true, "");
			var algorithmSelectionResult = processor.SelectAlgorithm(data);
			Assert.IsTrue(algorithmSelectionResult.Status == AlgorithmSelectionStatus.NoObjectFound);
		}

		[Test]
		public void SelectAlgorithm_WhenDataIsInvalid_ReturnsDataIsInvalidResult()
		{
			using var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams());
			var data = new AlgorithmSelectionData(null, null, 0, false, false, true, "");
			var algorithmSelectionResult = processor.SelectAlgorithm(data);
			Assert.IsTrue(algorithmSelectionResult.Status == AlgorithmSelectionStatus.DataIsInvalid);
		}

		public void Dispose()
		{
			_logger?.Dispose();
		}
	}
}