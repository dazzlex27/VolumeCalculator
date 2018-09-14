using System;
using System.Windows.Input;
using Common;
using FrameSources;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Logic;

namespace VolumeCalculatorGUI.GUI
{
    internal class TestDataGenerationControlVm : BaseViewModel
    {
	    private TestDataGenerator _testDataGenerator;
	    private DeviceParams _deviceParams;
	    private ApplicationSettings _applicationSettings;

		private string _testCaseName;
	    private string _testCaseFolderPath;
	    private string _description;
	    private int _objWidth;
	    private int _objHeight;
	    private int _objDepth;
	    private byte _timesToSave;
	    private bool _showControl;

	    private ICommand _runTestDataGenerationCommand;

		public bool CanRunTestDataGeneration => true;

	    private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

	    public ICommand RunTestDataGenerationCommand =>
		    _runTestDataGenerationCommand ?? (_runTestDataGenerationCommand =
			    new CommandHandler(RunTestDataGeneration, CanRunTestDataGeneration));

		public string TestCaseName
	    {
		    get => _testCaseName;
		    set
		    {
			    if (value == "")
				    throw new ArgumentOutOfRangeException();

				if (_testCaseName == value)
				    return;

			    _testCaseName = value;
			    OnPropertyChanged();
		    }
	    }

	    public string TestCaseFolderPath
	    {
		    get => _testCaseFolderPath;
		    set
		    {
			    if (value == "")
				    throw new ArgumentOutOfRangeException();

			    if (_testCaseFolderPath == value)
				    return;

			    _testCaseFolderPath = value;
			    OnPropertyChanged();
		    }
		}

	    public string Description
	    {
		    get => _description;
		    set
		    {
			    if (_description == value)
				    return;

			    _description = value;
			    OnPropertyChanged();
		    }
		}

	    public int ObjWidth
	    {
		    get => _objWidth;
		    set
		    {
			    if (value <= 0)
				    throw new ArgumentOutOfRangeException();

				if (_objWidth == value)
				    return;

			    _objWidth = value;
			    OnPropertyChanged();
		    }
		}

	    public int ObjHeight
	    {
		    get => _objHeight;
		    set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				if (_objHeight == value)
				    return;

				_objHeight = value;
			    OnPropertyChanged();
		    }
		}

	    public int ObjDepth
	    {
		    get => _objDepth;
		    set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				if (_objDepth == value)
				    return;

				_objDepth = value;
			    OnPropertyChanged();
		    }
		}

	    public byte TimesToSave
	    {
		    get => _timesToSave;
		    set
		    {
			    if (value <= 0)
				    throw new ArgumentOutOfRangeException();

			    if (_timesToSave == value)
				    return;

			    _timesToSave = value;
			    OnPropertyChanged();
		    }
		}

	    public bool ShowControl
	    {
		    get => _showControl;
		    set
		    {
			    if (_showControl == value)
				    return;

			    _showControl = value;
				OnPropertyChanged();
		    }
	    }

	    public TestDataGenerationControlVm(ApplicationSettings settings, DeviceParams deviceParams)
	    {
		    _applicationSettings = settings;
		    _deviceParams = deviceParams;

			TestCaseName = "obj1";
		    Description = "";
		    TestCaseFolderPath = "C:/3D/";
		    ObjWidth = 1;
		    ObjHeight = 1;
		    ObjDepth = 1;
		    TimesToSave = 30;
	    }

	    public void ColorFrameUpdated(ImageData image)
	    {
		    _latestColorFrame = image;
	    }

		public void DepthFrameUpdated(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			if (_testDataGenerator != null && _testDataGenerator.IsActive)
				_testDataGenerator.AdvanceDataSaving(depthMap);
		}

	    public void ApplicationSettingsUpdated(ApplicationSettings settings)
	    {
		    _applicationSettings = settings;
	    }

	    public void DeviceParamsUpdated(DeviceParams deviceParams)
	    {
		    _deviceParams = deviceParams;
	    }

	    private void RunTestDataGeneration()
	    {
		    var basicTestInfo = new TestCaseBasicInfo(TestCaseName, Description, TestCaseFolderPath,
			    ObjWidth, ObjHeight, ObjDepth, TimesToSave);

		    var testCaseData = new TestCaseData(basicTestInfo, _latestColorFrame, _latestDepthMap, _deviceParams,
			    _applicationSettings);

		    _testDataGenerator = new TestDataGenerator(testCaseData);
	    }
	}
}