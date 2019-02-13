﻿using System;

namespace DeviceIntegration.Scales
{
    public interface IScales : IDisposable
    {
	    event Action<ScaleMeasurementData> MeasurementReady;

	    void ResetWeight();
    }
}