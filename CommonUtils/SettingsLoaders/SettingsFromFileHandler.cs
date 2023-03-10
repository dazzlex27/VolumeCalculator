using Primitives.Settings;
using System.Threading.Tasks;

namespace CommonUtils.SettingsLoaders
{
	public class SettingsFromFileHandler : ISettingsHandler
	{
		private readonly string _filepath;

		public SettingsFromFileHandler(string filepath)
		{
			_filepath = filepath;
		}

		public async Task<ApplicationSettings> LoadAsync()
		{
			return await IoUtils.DeserializeSettingsFromFileAsync<ApplicationSettings>(_filepath);
		}

		public async Task SaveAsync(ApplicationSettings settings)
		{
			await IoUtils.SerializeSettingsToFileAsync(settings, _filepath);
		}
	}
}
