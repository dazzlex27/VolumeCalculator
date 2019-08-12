﻿using System;
using System.Windows.Input;
using FrameProcessor;
using FrameProviders;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculatorGUI.GUI.Utils;

namespace VolumeCalculatorGUI
{
	internal class TestDataGenerationControlVm : BaseViewModel
	{
		private readonly ILogger _logger;
		private readonly IFrameProvider _frameProvider;

		private TestDataGenerator _testDataGenerator;
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
			set => SetField(ref _description, value, nameof(Description));
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
			set => SetField(ref _showControl, value, nameof(ShowControl));
		}

		public bool GenerationInProgress
		{
			get => _generationInProgress;
			set => SetField(ref _generationInProgress, value, nameof(GenerationInProgress));
		}

		public TestDataGenerationControlVm(ILogger logger, ApplicationSettings settings, IFrameProvider frameProvider)
		{
			_logger = logger;
			_applicationSettings = settings;
			_frameProvider = frameProvider;

			TestCaseName = "obj1";
			Description = "";
			TestCaseFolderPath = "C:/3D/";
			ObjLength = 1;
			ObjWidth = 1;
			ObjHeight = 1;
			TimesToSave = 30;

			RunTestDataGenerationCommand = new CommandHandler(RunTestDataGeneration, !GenerationInProgress);
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_applicationSettings = settings;
		}

		private void RunTestDataGeneration()
		{
			try
			{
				GenerationInProgress = true;

				_logger.LogInfo("Saving test case data...");

				var basicTestInfo = new TestCaseInfo(TestCaseName, Description, TestCaseFolderPath, ObjLength,
					ObjWidth, ObjHeight, TimesToSave);

				_testDataGenerator = new TestDataGenerator(_logger, basicTestInfo, _frameProvider, _applicationSettings.AlgorithmSettings);
				_testDataGenerator.FinishedSaving += OnSavingFinished;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to start saving test case data", ex);
			}
		}

		private void OnSavingFinished(bool success)
		{
			try
			{
				_logger.LogInfo($"Finished saving test case data, success={success}");

				_testDataGenerator.FinishedSaving -= OnSavingFinished;
				_testDataGenerator = null;
			}
			finally
			{
				GenerationInProgress = false;
			}
		}
	}
}