using System;
using System.Windows.Input;
using FrameProviders;
using Primitives;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Logic;

namespace VolumeCalculatorGUI.GUI
{
    internal class TestDataGenerationControlVm : BaseViewModel
    {
	    private TestDataGenerator _testDataGenerator;
	    private ColorCameraParams _colorCameraParams;
	    private DepthCameraParams _depthCameraParams;
	    private ApplicationSettings _applicationSettings;

		private string _testCaseName;
	    private string _testCaseFolderPath;
	    private string _description;
	    private int _objWidth;
	    private int _objHeight;
	    private int _objLength;
	    private byte _timesToSave;
	    private bool _showControl;
	    private bool _generationInProgress;

	    private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

	    public ICommand RunTestDataGenerationCommand { get; }

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

	    public int ObjLength
	    {
		    get => _objLength;
		    set
		    {
			    if (value <= 0)
				    throw new ArgumentOutOfRangeException();

			    if (_objLength == value)
				    return;

			    _objLength = value;
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

	    public bool GenerationInProgress
	    {
		    get => _generationInProgress;
		    set
		    {
			    if (_generationInProgress == value)
				    return;

			    _generationInProgress = value;
				OnPropertyChanged();
		    }
	    }

	    public TestDataGenerationControlVm(ApplicationSettings settings, DepthCameraParams deptCameraParams)
	    {
		    _applicationSettings = settings;
		    _depthCameraParams = deptCameraParams;

			TestCaseName = "obj1";
		    Description = "";
		    TestCaseFolderPath = "C:/3D/";
		    ObjLength = 1;
		    ObjWidth = 1;
		    ObjHeight = 1;
		    TimesToSave = 30;

		    RunTestDataGenerationCommand = new CommandHandler(RunTestDataGeneration, !GenerationInProgress);
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
			else
				GenerationInProgress = false;
		}

	    public void ApplicationSettingsUpdated(ApplicationSettings settings)
	    {
		    _applicationSettings = settings;
	    }

	    public void DeviceParamsUpdated(ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
	    {
		    _colorCameraParams = colorCameraParams;
		    _depthCameraParams = depthCameraParams;
	    }

	    private void RunTestDataGeneration()
	    {
		    GenerationInProgress = true;

			var basicTestInfo = new TestCaseBasicInfo(TestCaseName, Description, TestCaseFolderPath, ObjLength,
			    ObjWidth, ObjHeight, TimesToSave);

		    var testCaseData = new TestCaseData(basicTestInfo, _latestColorFrame, _latestDepthMap, _depthCameraParams, _applicationSettings);

		    _testDataGenerator = new TestDataGenerator(testCaseData);
	    }
	}
}