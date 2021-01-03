using System.Collections.ObjectModel;
using GuiCommon;
using Primitives.Settings;

namespace VCConfigurator
{
	internal class ScalesSettingsVm : BaseViewModel
	{
		private ObservableCollection<string> _scalesNames;
		private string _activeScalesName;
		private string _scalesPort;
        private int _scalesMinWeight;

		public ObservableCollection<string> ScalesNames
		{
			get => _scalesNames;
			set => SetField(ref _scalesNames, value, nameof(ScalesNames));
		}

		public string ActiveScalesName
		{
			get => _activeScalesName;
			set => SetField(ref _activeScalesName, value, nameof(ActiveScalesName));
		}

		public string ScalesPort
		{
			get => _scalesPort;
			set => SetField(ref _scalesPort, value, nameof(ScalesPort));
		}
		
        public int ScalesMinWeight
        {
            get => _scalesMinWeight;
            set => SetField(ref _scalesMinWeight, value, nameof(ScalesMinWeight));
        }

        public ScalesSettingsVm()
		{
			ScalesNames = new ObservableCollection<string> { "", "massak", "casm", "fakescales", "ci2001a", "oka" };
		}

		public void FillValuesFromSettings(ScalesSettings settings)
		{
			ActiveScalesName = settings.Name;
			ScalesPort = settings.Port;
			ScalesMinWeight = settings.MinWeight;
		}

		public void FillSettingsFromValues(ScalesSettings settings)
		{
			settings.Name = ActiveScalesName;
			settings.Port = ScalesPort;
			settings.MinWeight = ScalesMinWeight;
        }
	}
}