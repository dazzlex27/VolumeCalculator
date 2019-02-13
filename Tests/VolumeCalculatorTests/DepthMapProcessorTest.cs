using FrameProcessor;
using NUnit.Framework;
using Primitives;
using Primitives.Logging;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class DepthMapProcessorTest
	{
		private readonly ILogger _logger;

		public DepthMapProcessorTest()
		{
			_logger = new DummyLogger();
		}

		[Test]
		public void CalculateFloorDepth_WhenGivenAnEmpty4x3Map_ReturnsZero()
		{
			const int mapWidth = 4;
			const int mapHeight = 3;
			var mapData = new short[mapWidth * mapHeight];
			var emptyMap = new DepthMap(mapWidth, mapHeight, mapData);

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var floorDepth = processor.CalculateFloorDepth(emptyMap);
				Assert.IsTrue(floorDepth == 0);
			}
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

			using (var processor = new DepthMapProcessor(_logger, 
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var floorDepth = processor.CalculateFloorDepth(map);
				Assert.IsTrue(floorDepth == validDepthValue);
			}
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

			using (var processor = new DepthMapProcessor(_logger, 
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var floorDepth = processor.CalculateFloorDepth(map);
				Assert.IsTrue(floorDepth == modeDepthValue);
			}
		}

		[Test]
		public void SelectAlgorithm_WhenMapAndImageAreEmpty_ReturnsNoObjectFoundResult()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var algorithmSelectionResult = processor.SelectAlgorithm(map, image, 0, true, true, true);
				Assert.IsTrue(algorithmSelectionResult == AlgorithmSelectionResult.NoObjectFound);
			}
		}

		[Test]
		public void SelectAlgorithm_WhenNoModeIsAvailable_ReturnsNoModesAreAvailable()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var algorithmSelectionResult = processor.SelectAlgorithm(map, image, 0, false, false, false);
				Assert.IsTrue(algorithmSelectionResult == AlgorithmSelectionResult.NoModesAreAvailable);
			}
		}

		[Test]
		public void SelectAlgorithm_WhenOnlyDm1IsAbailable_ReturnsDm1Result()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var algorithmSelectionResult = processor.SelectAlgorithm(map, image, 0, true, false, false);
				Assert.IsTrue(algorithmSelectionResult == AlgorithmSelectionResult.Dm);
			}
		}

		[Test]
		public void SelectAlgorithm_WhenOnlyDm2IsAbailable_ReturnsDm2Result()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var algorithmSelectionResult = processor.SelectAlgorithm(map, image, 0, false, true, false);
				Assert.IsTrue(algorithmSelectionResult == AlgorithmSelectionResult.DmPersp);
			}
		}

		[Test]
		public void SelectAlgorithm_WhenOnlyRgbIsAbailable_ReturnsRgbResult()
		{
			var image = new ImageData(1, 1, new byte[3], 3);
			var map = new DepthMap(1, 1, new short[1]);

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var algorithmSelectionResult = processor.SelectAlgorithm(map, image, 0, false, false, true);
				Assert.IsTrue(algorithmSelectionResult == AlgorithmSelectionResult.Rgb);
			}
		}

		[Test]
		public void SelectAlgorithm_WhenDataIsINvalid_ReturnsDataIsInvalidResult()
		{
			ImageData image = null;
			DepthMap map = null;

			using (var processor = new DepthMapProcessor(_logger,
				TestUtils.GetDummyColorCameraParams(), TestUtils.GetDummyDepthCameraParams()))
			{
				var algorithmSelectionResult = processor.SelectAlgorithm(map, image, 0, false, false, true);
				Assert.IsTrue(algorithmSelectionResult == AlgorithmSelectionResult.DataIsInvalid);
			}
		}
	}
}