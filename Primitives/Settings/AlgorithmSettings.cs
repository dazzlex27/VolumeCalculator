using System.Runtime.Serialization;
using System.Text;

namespace Primitives.Settings
{
	public class AlgorithmSettings
	{
		private const int DefaultTimerValueMs = 1000;
		private const int DefaultSampleCount = 5;

		public WorkAreaSettings WorkArea { get; set; }
		
		public byte SampleDepthMapCount { get; set; }

		public bool EnableAutoTimer { get; set; }

		public long TimeToStartMeasurementMs { get; set; }

		public bool RequireBarcode { get; set; }

		public WeightUnits SelectedWeightUnits { get; set; }
		
		public bool EnablePalletSubtraction { get; set; }
		
		public double PalletWeightGr { get; set; }
		
		public int PalletHeightMm { get; set; }

		public AlgorithmSettings(WorkAreaSettings workArea, 
			byte sampleDepthMapCount, bool enableAutoTimer, long timeToStartMeasurementMs, 
			bool requireBarcode, WeightUnits selectedWeightUnits, 
			bool enablePalletSubtraction, double palletWeightGr, int palletHeightMm)
		{
			WorkArea = workArea;
			SampleDepthMapCount = sampleDepthMapCount;
			EnableAutoTimer = enableAutoTimer;
			TimeToStartMeasurementMs = timeToStartMeasurementMs;
			RequireBarcode = requireBarcode;
			SelectedWeightUnits = selectedWeightUnits;
			PalletWeightGr = palletWeightGr;
			PalletHeightMm = palletHeightMm;
			EnablePalletSubtraction = enablePalletSubtraction;
		}

		public static AlgorithmSettings GetDefaultSettings()
		{
			return new AlgorithmSettings(WorkAreaSettings.GetDefaultSettings(),
				DefaultSampleCount, true, 
				DefaultTimerValueMs, true, WeightUnits.Gr, false, 0, 0);
		}

		public override string ToString()
		{
			var builder = new StringBuilder("AlgorithmSettings:");
			builder.Append($",sampleCount={SampleDepthMapCount}");
			builder.Append($",enableAutTimer={EnableAutoTimer}");
			builder.Append($",timeToStartMeasurementMs={TimeToStartMeasurementMs}");
			builder.Append($",requireBarcode={RequireBarcode}");
			builder.Append($",selectedWeightUnits={SelectedWeightUnits}");
			builder.Append($",enablePalletSubtraction={EnablePalletSubtraction}");
			builder.Append($",palletWeightKg={PalletWeightGr}");
			builder.Append($",palletHeightMm={PalletHeightMm}");

			return builder.ToString();
		}
	
		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (WorkArea == null)
				WorkArea = WorkAreaSettings.GetDefaultSettings();

			if (SampleDepthMapCount <= 0)
				SampleDepthMapCount = DefaultSampleCount;

			if (TimeToStartMeasurementMs <= 0)
				TimeToStartMeasurementMs = DefaultTimerValueMs;
		}
	}
}