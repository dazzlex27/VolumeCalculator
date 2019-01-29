using System.ComponentModel;

namespace VolumeCalculatorGUI.GUI
{
	internal partial class MainWindow
	{
		private readonly MainWindowVm _vm;

		public MainWindow()
		{
			InitializeComponent();
			_vm = (MainWindowVm) DataContext;
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (!_vm.ShutDown(_vm.ShutDownByDefault, false))
				e.Cancel = true;
		}
	}
}