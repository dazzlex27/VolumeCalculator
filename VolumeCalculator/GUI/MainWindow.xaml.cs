using System;
using System.ComponentModel;
using System.Threading.Tasks;
using VolumeCalculator.ViewModels;

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
			if (!Task.Run(async () => await _vm.ShutDownAsync(_vm.ShutDownByDefault, false)).Result)
				e.Cancel = true;
		}

		private void OnContentRendered(object sender, EventArgs e)
		{
			Focus();
			Activate();
		}
	}
}
