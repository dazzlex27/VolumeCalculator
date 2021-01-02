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

		public void FillValuesFromSettings(IoSettings settings)
		{
			ActiveScalesName = settings.ActiveScales.Name;
			ScalesPort = settings.ActiveScales.Port;
			ScalesMinWeight = settings.ActiveScales.MinWeight;
		}

		public void FillSettingsFromValues(IoSettings settings)
		{
			settings.ActiveScales.Name = ActiveScalesName;
			settings.ActiveScales.Port = ScalesPort;
			settings.ActiveScales.MinWeight = ScalesMinWeight;
        }
	}
}