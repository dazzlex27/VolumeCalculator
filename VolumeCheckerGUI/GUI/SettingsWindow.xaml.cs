using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VolumeCheckerGUI.Entities;

namespace VolumeCheckerGUI.GUI
{
    internal partial class SettingsWindow : INotifyPropertyChanged
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

		public SettingsWindow(ApplicationSettings settings)
		{
			var oldSettings = settings ?? ApplicationSettings.GetDefaultSettings();

			_distanceToFloor = oldSettings.DistanceToFloor;
			_minObjHeight = oldSettings.MinObjHeight;
	        _outputPath = oldSettings.OutputPath;

            InitializeComponent();
        }

		public event PropertyChangedEventHandler PropertyChanged;

		public ApplicationSettings GetSettings()
		{
			return new ApplicationSettings(_distanceToFloor, _minObjHeight, _outputPath);
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void BtOk_Click(object sender, RoutedEventArgs e)
		{
			var validationPassed = IsValid(TbDistanceToFloor) && IsValid(TbMinObjHeight);
			if (!validationPassed)
				return;

			DialogResult = true;
			Close();
		}

		private void BtCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private static bool IsValid(DependencyObject obj)
		{
			return !Validation.GetHasError(obj) && LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>().
				       All(IsValid);
		}
	}
}