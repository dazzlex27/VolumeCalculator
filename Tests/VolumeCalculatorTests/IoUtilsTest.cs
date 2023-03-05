using Primitives.Settings;
using ProcessingUtils;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class IoUtilsTest
	{
		[Test]
		public async Task DeserializeSettingsFromFile_GivenValidSettingsFile_ReturnsValidApplicationSettings()
		{
			var fileInfo = new string("data/main.cfg");
			var settings = await IoUtils.DeserializeSettingsFromFileAsync<ApplicationSettings>(fileInfo);

			Assert.That(settings, Is.Not.Null);
		}

		[Test]
		public async Task SerializeSettingsToFile_GivenDefaultApplicationSettings_NoThrow()
		{
			var settings = ApplicationSettings.GetDefaultSettings();
			var settingsFileInfo = new string("data/test.cfg");

			await IoUtils.SerializeSettingsToFileAsync(settings, settingsFileInfo);

			Assert.That(true, Is.True); // no exception occurred
		}

		[Test]
		public void GetCurrentUniversalObjectCounter_WhenGivenValidCountersFile_ReturnsCorrectCount()
		{
			const int gtCounter = 173;
			const string counterFilePath = "data/counters";

			var readCounter = IoUtils.GetCurrentUniversalObjectCounter(counterFilePath);

			Assert.That(readCounter, Is.EqualTo(gtCounter));
		}

		[Test]
		public void IncrementCurrentUniversalObjectCounter_WhenGivenValidCountersFile_ReturnsIncrementedCount()
		{
			const string counterFilePath = "data/counters_inc";

			var startCounter = IoUtils.GetCurrentUniversalObjectCounter(counterFilePath);
			IoUtils.IncrementUniversalObjectCounter(counterFilePath);
			var incrementedCounter = IoUtils.GetCurrentUniversalObjectCounter(counterFilePath);

			Assert.That(incrementedCounter, Is.EqualTo(startCounter + 1));
		}
	}
}
