using System;

namespace DeviceIntegration
{
    public interface IBarcodeScanner : IDisposable
    {
        event Action<string> CharSequenceFormed;

        void TogglePause(bool pause);
    }
}