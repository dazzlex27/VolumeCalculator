using System;

namespace DeviceIntegration.Scanners
{
	public interface IBarcodeScanner : IDisposable
	{
		event Action<string> CharSequenceFormed;
	}
}