using System;

namespace DeviceIntegrations.Scanners
{
	public interface IBarcodeScanner : IDisposable
	{
		event Action<string> CharSequenceFormed;
	}
}