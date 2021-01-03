using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Primitives.Settings
{
	public class IoSettings
	{
		public string ActiveCameraName { get; set; }

		public ScalesSettings ActiveScales { get; set; }

		public DeviceSettings[] ActiveScanners { get; set; }

		public DeviceSettings ActiveIoCircuit { get; set; }
		
		public string ActiveRangeMeterName { get; set; }

		public int RangeMeterSubtractionValueMm { get; set; }

		public IpCameraSettings IpCameraSettings { get; set; }

		public  IoSettings(string activeCameraName, ScalesSettings scalesSettings,
			DeviceSettings[] activeScanners, DeviceSettings activeIoCircuit, string activeRangeMeterName, 
            int rangeMeterSubtractionValueMm, IpCameraSettings ipCameraSettings)
		{
			ActiveCameraName = activeCameraName;
			ActiveScales = scalesSettings;
			ActiveScanners = activeScanners;
			ActiveIoCircuit = activeIoCircuit;
			ActiveRangeMeterName = activeRangeMeterName;
			RangeMeterSubtractionValueMm = rangeMeterSubtractionValueMm;
			IpCameraSettings = ipCameraSettings;
		}

		public override string ToString()
		{
			var builder = new StringBuilder("IOSetings:");
			builder.Append($"ActiveScales={ActiveScales}");
			builder.Append($",ActiveScanners={ActiveScanners.ToList().ConvertAll(s=>s.ToString())}");
			builder.Append($",rangeCorrection={RangeMeterSubtractionValueMm}");

			return builder.ToString();
		}

		public static IoSettings GetDefaultSettings()
		{
			var defaultScales = ScalesSettings.GetDefaultSettings();
			var defaultScanners = new[] { new DeviceSettings("keyboard", "") };
			var defaultIoCircuitBoard = new DeviceSettings("keusb24r", "");
			var defaultCameraSettings = IpCameraSettings.GetDefaultSettings();
			
			return new IoSettings("kinectv2", defaultScales, defaultScanners, 
				defaultIoCircuitBoard, "custom", 0, defaultCameraSettings);
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (ActiveScales == null)
				ActiveScales = ScalesSettings.GetDefaultSettings();

			if (ActiveScanners == null)
				ActiveScanners = new[] {new DeviceSettings("keyboard", "")};
			
			if (ActiveIoCircuit == null)
				ActiveIoCircuit = new DeviceSettings("keusb24r", "");
			
			if (IpCameraSettings == null)
				IpCameraSettings = IpCameraSettings.GetDefaultSettings();
		}
	}
}