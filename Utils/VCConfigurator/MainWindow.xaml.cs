namespace VCConfigurator
{
	internal partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainWindowVm();
		}
	}
}