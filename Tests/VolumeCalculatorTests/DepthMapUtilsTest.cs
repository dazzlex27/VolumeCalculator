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
			var depthMap = new DepthMap(1, 1, new short[] {30});
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
	}
}