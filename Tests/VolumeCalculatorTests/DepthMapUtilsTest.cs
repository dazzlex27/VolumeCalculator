using Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class DepthMapUtilsTest
	{
		[Test]
		public void CtorDepthMap_WhenGivenOnlyWidthAndHeight_ReturnsDmWithDataAllocatedCorrectly()
		{
			var gtWidth = 40;
			var gtHeight = 30;
			var gtDataLength = gtWidth * gtHeight;

			var dm = new DepthMap(gtWidth, gtHeight);

			Assert.That(dm.Data, Has.Length.EqualTo(gtDataLength));
		}

		[Test]
		public void CopyCtorDepthMap_WhenGivenValidDepthMap_ReturnsDeepCopiedDepthMap()
		{
			var gtWidth = 40;
			var gtHeight = 30;
			var gtDataLength = gtWidth * gtHeight;
			var gtDm = new DepthMap(gtWidth, gtHeight);

			var copyDm = new DepthMap(gtDm);

			Assert.That(copyDm, Is.Not.SameAs(gtDm));
			Assert.That(copyDm.Data, Is.Not.SameAs(gtDm.Data));
			Assert.Multiple(() =>
			{
				Assert.That(copyDm.Width, Is.EqualTo(gtWidth));
				Assert.That(copyDm.Height, Is.EqualTo(gtHeight));
				Assert.That(copyDm.Data, Has.Length.EqualTo(gtDataLength));
			});
		}

		[Test]
		public async Task ReadDepthMapFromRawFileAsync_WhenGivenValidDmFile_ReturnsValidDm()
		{
			const string dmFile = "data/frames/depth/0.dm";
			const int gtWidth = 16;
			const int gtHeight = 9;
			const int gtDataLength = gtWidth * gtHeight;

			var dm = await DepthMapUtils.ReadDepthMapFromRawFileAsync(dmFile);

			Assert.That(dm, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(dm.Width, Is.EqualTo(gtWidth));
				Assert.That(dm.Height, Is.EqualTo(gtHeight));
				Assert.That(dm.Data, Has.Length.EqualTo(gtDataLength));
			});
		}

		[Test]
		public async Task SaveDepthMapToRawFileAsync_WhenCreatingSmallDm_SavesBlankDm()
		{
			const string filepath = "data/frames/temp.dm";
			const int gtWidth = 90;
			const int gtHeight = 120;
			const int gtDataLength = gtWidth * gtHeight;

			var depthMap = new DepthMap(gtWidth, gtHeight);
			await DepthMapUtils.SaveDepthMapToRawFileAsync(depthMap, filepath);
			var depthMapFromFile = await DepthMapUtils.ReadDepthMapFromRawFileAsync(filepath);

			Assert.That(depthMapFromFile, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(depthMapFromFile.Width, Is.EqualTo(gtWidth));
				Assert.That(depthMapFromFile.Height, Is.EqualTo(gtHeight));
				Assert.That(depthMapFromFile.Data, Has.Length.EqualTo(gtDataLength));
			});
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMap_ReturnsMapWithAllZeroes()
		{
			var depthMap = new DepthMap(1, 1, new short[] { 30 });

			var filteredMap = DepthMapUtils.GetDepthFilteredDepthMap(depthMap, 20);

			Assert.That(filteredMap.Data.All(a => a == 0), Is.True);
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMap_ReturnsMapWithNonZeroes()
		{
			var depthMap = new DepthMap(1, 1, new short[] { 30 });

			var filteredMap = DepthMapUtils.GetDepthFilteredDepthMap(depthMap, 40);

			Assert.That(filteredMap.Data.All(a => a != 0), Is.True);
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMapWithValueOnTheEdge_ReturnsMapWithNonZeroes()
		{
			var depthMap = new DepthMap(1, 1, new short[] { 30 });

			var filteredMap = DepthMapUtils.GetDepthFilteredDepthMap(depthMap, 30);

			Assert.That(filteredMap.Data.All(a => a != 0), Is.True);
		}

		[Test]
		public void FilterDepthMapByMaxDepth_WhenGivenA1x1DepthMapWithValueOnTheEdge_ThrowsNullReferenceException()
		{
			var depthMap = new DepthMap(3, 2, null);

			Assert.Throws<NullReferenceException>(() => DepthMapUtils.GetDepthFilteredDepthMap(depthMap, 30));
		}

		[Test]
		public void GetIntensityFromDepth_WhenDepthSmallerThanMinValue_ReturnsZero()
		{
			const int depthValue = -10;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 100);

			Assert.That(intensityValue, Is.EqualTo(0));
		}

		[Test]
		public void GetIntensityFromDepth_WhenDepthLargerThanMaxValue_ReturnsMaxValue()
		{
			const int depthValue = 150;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 100);

			Assert.That(intensityValue, Is.EqualTo(0));
		}

		[Test]
		public void GetIntensityFromDepth_WhenGivenValieValue_ReturnsValidIntensity()
		{
			const int depthValue = 40;
			const byte expectedIntensity = 153;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 100);

			Assert.That(intensityValue, Is.EqualTo(expectedIntensity));
		}

		[Test]
		public void GetIntensityFromDepth_WhenMaxValueIsZero_ReturnsZero()
		{
			const int depthValue = 40;

			var intensityValue = DepthMapUtils.GetIntensityFromDepth(depthValue, 0, 0);

			Assert.That(intensityValue, Is.EqualTo(0));
		}
	}
}
