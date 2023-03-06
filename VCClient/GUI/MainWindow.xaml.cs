using System;

namespace VCClient.GUI
{
	internal partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnContentRendered(object sender, EventArgs e)
		{
			Focus();
			Activate();
		}
	}
}
