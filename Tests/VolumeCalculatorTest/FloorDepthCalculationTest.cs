using Common;
using FrameSources;
using NUnit.Framework;
using VolumeCalculatorGUI.Logic;

namespace VolumeCalculatorTest
{
	[TestFixture]
	internal class FloorDepthCalculationTest
	{
		private readonly ILogger _logger;

		public FloorDepthCalculationTest()
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

			using (var processor = new DepthMapProcessor(_logger, GetDummyDeviceParams()))
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

			using (var processor = new DepthMapProcessor(_logger, GetDummyDeviceParams()))
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

			using (var processor = new DepthMapProcessor(_logger, GetDummyDeviceParams()))
			{
				var floorDepth = processor.CalculateFloorDepth(map);
				Assert.IsTrue(floorDepth == modeDepthValue);
			}
		}

		private static DeviceParams GetDummyDeviceParams()
		{
			return new DeviceParams(1, 1, 1, 1, 1, 1, 1, 1);
		}
	}
}