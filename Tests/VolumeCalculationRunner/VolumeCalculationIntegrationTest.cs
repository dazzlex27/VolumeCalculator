namespace VolumeCalculationRunner
{
	[TestFixture]
	internal class VolumeCalculationIntegrationTest
	{
		[Test]
		public async Task TestAllCasesAsync_WhenGivenManyTestCases_ReturnWithNoExceptions()
		{
			using var runner = new VolumeCalculationRunner();
			await runner.TestAllCasesAsync();
		}
	}
}
