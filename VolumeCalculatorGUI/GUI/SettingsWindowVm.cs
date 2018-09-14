using VolumeCalculatorGUI.GUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class SettingsWindowVm : BaseViewModel
	{
		private short _distanceToFloor;
		private short _minObjHeight;
		private string _outputPath;

		public short DistanceToFloor
		{
			get => _distanceToFloor;
			set
			{
				if (_distanceToFloor == value)
					return;

				_distanceToFloor = value;
				OnPropertyChanged();
			}
		}

		public short MinObjHeight
		{
			get => _minObjHeight;
			set
			{
				if (_minObjHeight == value)
					return;

				_minObjHeight = value;
				OnPropertyChanged();
			}
		}

		public string OutputPath
		{
			get => _outputPath;
			set
			{
				if (_outputPath == value)
					return;

				_outputPath = value;
				OnPropertyChanged();
			}
		}
	}
}