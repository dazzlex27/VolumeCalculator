using System.ComponentModel;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

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
	        if (MessageBox.Show("Вы действительно хотите выйти?", "Завершение работы", MessageBoxButton.YesNo,
		            MessageBoxImage.Question) == MessageBoxResult.No)
	        {
                e.Cancel = true;
		        return;
	        }

			_vm.ExitApplication();
        }

	    private void MiExitApp_OnClick(object sender, RoutedEventArgs e)
	    {
		    Close();
	    }
    }
}