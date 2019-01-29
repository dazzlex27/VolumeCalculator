using System;
using DeviceIntegrations.Scales;

namespace DeviceIntegration.Scales
{
    public interface IScales : IDisposable
    {
	    event Action<ScaleMeasurementData> MeasurementReady;

	    void ResetWeight();
    }
}