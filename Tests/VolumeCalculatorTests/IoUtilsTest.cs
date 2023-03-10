using CommonUtils;
using Primitives.Settings;

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
			var gtSettings = ApplicationSettings.GetDefaultSettings();
			var settingsFilepath = "data/test.cfg";

			await IoUtils.SerializeSettingsToFileAsync(gtSettings, settingsFilepath);
			var settings = await IoUtils.DeserializeSettingsFromFileAsync<ApplicationSettings>(settingsFilepath);

			Assert.That(settings, Is.Not.EqualTo(null));
			// TODO: check equality piece-wise
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
