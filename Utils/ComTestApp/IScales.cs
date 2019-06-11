using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComTestApp
{
	public interface IScales : IDisposable
	{
		event Action<ScaleMeasurementData> MeasurementReady;

		void ResetWeight();
	}
}
