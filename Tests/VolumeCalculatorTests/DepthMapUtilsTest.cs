using System;
using System.Linq;
using NUnit.Framework;
using Primitives;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class DepthMapUtilsTest
	{
		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMap_ReturnsMapWithAllZeroes()
		{
			var depthMap = new DepthMap(1, 1, new short[] { 30 });
			DepthMapUtils.FilterDepthMapByDepthtLimit(depthMap, 20);

			Assert.IsTrue(depthMap.Data.All(a => a == 0));
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMap_ReturnsMapWithNonZeroes()
		{
			var depthMap = new DepthMap(1, 1, new short[] { 30 });
			DepthMapUtils.FilterDepthMapByDepthtLimit(depthMap, 40);

			Assert.IsTrue(depthMap.Data.All(a => a != 0));
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMapWithValueOnTheEdge_ReturnsMapWithNonZeroes()
		{
			var depthMap = new DepthMap(1, 1, new short[] { 30 });
			DepthMapUtils.FilterDepthMapByDepthtLimit(depthMap, 30);

			Assert.IsTrue(depthMap.Data.All(a => a != 0));
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMapWithValueOnTheEdge_ThrowsNullReferenceException()
		{
			var depthMap = new DepthMap(3, 2, null);

			Assert.Throws<NullReferenceException>(() => DepthMapUtils.FilterDepthMapByDepthtLimit(depthMap, 30));
		}

		[Test]
		public void GetIntensityFromDepth_WhenDepthSmallerThanMinValue_ReturnsZero()
		{
			const int depthValue = -10;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 100);

			Assert.IsTrue(intensityValue == 0);
		}

		[Test]
		public void GetIntensityFromDepth_WhenDepthLargerThanMaxValue_ReturnsMaxValue()
		{
			const int depthValue = 150;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 100);

			Assert.IsTrue(intensityValue == 0);
		}

		[Test]
		public void GetIntensityFromDepth_WhenGivenValieValue_ReturnsValidIntensity()
		{
			const int depthValue = 40;
			const byte expectedIntensity = 153;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 100);

			Assert.IsTrue(intensityValue == expectedIntensity);
		}

		[Test]
		public void GetIntensityFromDepth_WhenMaxValueIsZero_ReturnsZero()
		{
			const int depthValue = 40;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 0);

			Assert.IsTrue(intensityValue == 0);
		}
	}
}