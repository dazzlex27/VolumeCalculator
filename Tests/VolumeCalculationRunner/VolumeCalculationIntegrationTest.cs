namespace VolumeCalculationRunner
{
	[TestFixture]
	internal class VolumeCalculationIntegrationTest
	{
		[Test]
		public void TestVolumeCalculation_WhenGivenManyTestCases_ReturnWithNoExceptions()
		{
			new VolumeCalculationRunner().TestAllCases();
		}
	}
}
