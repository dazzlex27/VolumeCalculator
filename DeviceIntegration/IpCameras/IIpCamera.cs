using Primitives;
using System.Threading.Tasks;

namespace DeviceIntegration.Cameras
{
	public interface IIpCamera
	{
		Task<bool> ConnectAsync();

		Task<ImageData> GetSnaphostAsync();

		Task<bool> GoToPresetAsync(int presetIndex);

		Task<bool> DisconnectAsync();

		bool Initialized();
	}
}