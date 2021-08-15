using System;
using System.ComponentModel;

namespace VolumeCalculator.GUI
{
	internal partial class MainWindow
	{
		private readonly MainWindowVm _vm;

		public MainWindow()
		{
			InitializeComponent();
			_vm = (MainWindowVm) DataContext;
		}

		private void OnWindowClosing(object sender, CancelEventArgs e)
		{
			if (!_vm.ShutDown(_vm.ShutDownByDefault, false))
				e.Cancel = true;
		}

		private void OnContentRendered(object sender, EventArgs e)
		{
			Focus();
			Activate();
		}
	}
}