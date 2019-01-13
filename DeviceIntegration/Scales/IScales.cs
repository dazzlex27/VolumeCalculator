using System;

namespace DeviceIntegrations.Scales
{
    public interface IScales : IDisposable
    {
	    event Action<ScaleMeasurementData> MeasurementReady;

	    void ResetWeight();
    }
}