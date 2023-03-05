using Primitives.Logging;
using System;
using System.ComponentModel;
using VCClient.Utils;
using VCClient.ViewModels;

namespace VCClient.GUI
{
	internal partial class MainWindow
	{
		private readonly ILogger _logger;
		private readonly MainWindowVm _vm;

		public MainWindow()
		{
			InitializeComponent();

			_logger = new TxtLogger(GuiUtils.AppTitle, "main");
			_vm = new MainWindowVm(_logger);
			DataContext = _vm;
		}

		private void OnWindowClosing(object sender, CancelEventArgs e)
		{
			try
			{
				if (!_vm.ShutDown(_vm.ShutDownByDefault, false))
					e.Cancel = true;
			}
			catch (Exception ex) 
			{
				_logger.LogException("failed to run teartown routine", ex);
			}
		}

		private async void OnContentRendered(object sender, EventArgs e)
		{
			try
			{
				await _vm.InitializeAsync();
				Focus();
				Activate();

			}
			catch (Exception ex)
			{
				_logger.LogException("failed to run initialization routine", ex);
			}
		}
	}
}
