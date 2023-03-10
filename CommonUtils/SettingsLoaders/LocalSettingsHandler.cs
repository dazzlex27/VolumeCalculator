using Primitives.Settings;
using System.Threading.Tasks;

namespace CommonUtils.SettingsLoaders
{
	public class LocalSettingsHandler : ISettingsHandler
	{
		public LocalSettingsHandler(ApplicationSettings settings)
		{
			Settings = settings;
		}

		public ApplicationSettings Settings { get; private set; }

		public async Task<ApplicationSettings> LoadAsync()
		{
			return await Task.FromResult(Settings);
		}

		public async Task SaveAsync(ApplicationSettings settings)
		{
			await Task.FromResult(Settings = settings);
		}
	}
}
