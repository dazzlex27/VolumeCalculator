using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace VolumeCheckerGUI
{
    internal partial class SettingsWindow : Window, INotifyPropertyChanged
    {
		private short _distanceToFloor;
		private short _minObjHeight;

		public short DistanceToFloor
		{
			get { return _distanceToFloor; }
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
			get { return _minObjHeight; }
			set
			{
				if (_minObjHeight == value)
					return;

				_minObjHeight = value;
				OnPropertyChanged();
			}
		}

		public SettingsWindow(CheckerSettings settings)
        {
			_distanceToFloor = settings.DistanceToFloor;
			_minObjHeight = settings.MinObjHeight;

            InitializeComponent();
        }

		public event PropertyChangedEventHandler PropertyChanged;

		public CheckerSettings GetSettings()
		{
			return new CheckerSettings(_distanceToFloor, _minObjHeight);
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void BtOk_Click(object sender, RoutedEventArgs e)
		{
			var validationPassed = IsValid(TbDistanceToFloor as DependencyObject) && IsValid(TbMinObjHeight as DependencyObject);
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

		private bool IsValid(DependencyObject obj)
		{
			// The dependency object is valid if it has no errors and all
			// of its children (that are dependency objects) are error-free.
			return !Validation.GetHasError(obj) &&
			LogicalTreeHelper.GetChildren(obj)
			.OfType<DependencyObject>()
			.All(IsValid);
		}
	}
}