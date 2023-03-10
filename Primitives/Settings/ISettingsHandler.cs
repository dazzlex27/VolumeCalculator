using System.Threading.Tasks;

namespace Primitives.Settings
{
	public interface ISettingsHandler
	{
		Task<ApplicationSettings> LoadAsync();

		Task SaveAsync(ApplicationSettings settings);
	}
}
