using DeviceIntegration.Scales;
using Primitives.Calculation;
using Primitives.Logging;
using Primitives.Settings;
using VCClient.Utils;
using VCClient.ViewModels;
using VCServer;

namespace VolumeCalculatorTests.GUI
{
	[TestFixture]
	internal class DashboardControlVmTest
	{
		ILogger _logger;

		[SetUp]
		public void SetUp()
		{
			_logger = new DummyLogger();
		}

		[TearDown]
		public void TearDown()
		{
			_logger?.Dispose();
		}

		[Test]
		public void UpdateDataUponCalculationFinish_WhenRanOnFreshInstance_SetsCorrectAttributes()
		{
			var gtObjectCode = "";
			var gtUnitCount = 0;
			var gtComment = "";
			var gtresultData = TestUtils.GetSuccessfulResult();
			var gtResult = gtresultData.Result;

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateDataUponCalculationFinish(gtresultData);

			Assert.Multiple(() =>
			{
				Assert.That(vm.ObjectCode, Is.EqualTo(gtObjectCode));
				Assert.That(vm.UnitCount, Is.EqualTo(gtUnitCount));
				Assert.That(vm.Comment, Is.EqualTo(gtComment));
				Assert.That(vm.ObjectLength, Is.EqualTo(gtResult.ObjectLengthMm));
				Assert.That(vm.ObjectWidth, Is.EqualTo(gtResult.ObjectWidthMm));
				Assert.That(vm.ObjectHeight, Is.EqualTo(gtResult.ObjectHeightMm));
				Assert.That(vm.ObjectVolume * 1000.0, Is.EqualTo(gtResult.ObjectVolumeMm));
			});
		}

		[Test]
		public void UpdateBarcode_WhenGivenValidBarcode_SetsBarcode()
		{
			var gtBarcode = "TESTBARCODE1234";

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateBarcode(gtBarcode);

			Assert.That(vm.ObjectCode, Is.EqualTo(gtBarcode));
		}

		[Test]
		public void UpdateBarcode_WhenCalculationInProgress_DoesntSetBarcode()
		{
			var gtBarcode = "TESTBARCODE1234";

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.CalculationInProgress = true;
			vm.UpdateBarcode(gtBarcode);

			Assert.That(vm.ObjectCode, Is.Not.EqualTo(gtBarcode));
		}

		[Test]
		public void UpdateBarcode_WhenGivenEmptyBarcode_DoesntSetBarcode()
		{
			var gtBarcodeBefore = "TESTBARCODE1234";
			var gtBarcodeAfter = string.Empty;

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateBarcode(gtBarcodeBefore);
			vm.UpdateBarcode(gtBarcodeAfter);

			Assert.That(vm.ObjectCode, Is.EqualTo(gtBarcodeBefore));
		}

		[TestCase(nameof(DashboardControlVm.CodeBoxFocused))]
		[TestCase(nameof(DashboardControlVm.UnitCountBoxFocused))]
		[TestCase(nameof(DashboardControlVm.CommentBoxFocused))]
		public void UpdateBarcode_WhenABoxIsFocused_DoesntSetBarcode(string propertyName)
		{
			var gtBarcode = "TESTBARCODE1234";

			var vm = new DashboardControlVm();
			vm.GetType().GetProperty(propertyName).SetValue(vm, true);
			vm.UpdateBarcode(gtBarcode);

			Assert.That(vm.ObjectCode, Is.Not.EqualTo(gtBarcode));
		}

		[Test]
		public void UpdateWeight_WhenGivenValidWeight_SetsWeight()
		{
			var gtWeightGr = 725;
			var gtScaleDate = new ScaleMeasurementData(MeasurementStatus.Measured, gtWeightGr);

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateWeight(gtScaleDate);

			Assert.That(vm.ObjectWeight, Is.EqualTo(gtWeightGr));
		}

		[Test]
		public void UpdateWeight_WhenGivenNullWeightData_DoesntSetWeight()
		{
			var gtWeightGr = 725;
			ScaleMeasurementData gtScaleDate = null;

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateWeight(gtScaleDate);

			Assert.That(vm.ObjectWeight, Is.Not.EqualTo(gtWeightGr));
		}

		[Test]
		public void UpdateWeight_WhenCalculationInGrogress_DoesntSetWeight()
		{
			var gtWeightGr = 725;
			var gtScaleDate = new ScaleMeasurementData(MeasurementStatus.Measured, gtWeightGr);

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.CalculationInProgress = true;
			vm.UpdateWeight(gtScaleDate);

			Assert.That(vm.ObjectWeight, Is.Not.EqualTo(gtWeightGr));
		}

		[Test]
		public void UpdateWeight_WhenWeightIsTooSmall_DoesntSetWeight()
		{
			var gtWeightGr = 0.000001;
			var gtScaleDate = new ScaleMeasurementData(MeasurementStatus.Measured, gtWeightGr);

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateWeight(gtScaleDate);

			Assert.That(vm.ObjectWeight, Is.Not.EqualTo(gtWeightGr));
		}

		[TestCase(CalculationStatus.AbortedByUser, false, false)]
		[TestCase(CalculationStatus.BarcodeNotEntered, false, false)]
		[TestCase(CalculationStatus.CalculationError, false, false)]
		[TestCase(CalculationStatus.FailedToCloseFiles, false, false)]
		[TestCase(CalculationStatus.FailedToSelectAlgorithm, false, false)]
		[TestCase(CalculationStatus.FailedToStart, false, false)]
		[TestCase(CalculationStatus.InProgress, true, false)]
		[TestCase(CalculationStatus.ObjectNotFound, false, false)]
		[TestCase(CalculationStatus.Pending, false, true)]
		[TestCase(CalculationStatus.Successful, false, false)]
		[TestCase(CalculationStatus.TimedOut, false, false)]
		[TestCase(CalculationStatus.WeightNotStable, false, false)]
		public void UpdateCalculationStatus_WhenSetToSuccessful_SetsCorrectValues(
			CalculationStatus gtStatus, bool gtCalculationInProgress, bool gtCalculationPending)
		{
			var dashboardStatus = StatusUtils.GetDashboardStatus(gtStatus);
			var gtStatusBrush = GuiUtils.GetBrushFromDashboardStatus(dashboardStatus);
			var gtStatusText = GuiUtils.GetMessageFromCalculationStatus(gtStatus);

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			vm.UpdateCalculationStatus(gtStatus);
			
			Assert.Multiple(() =>
			{
				Assert.That(vm.CalculationInProgress, Is.EqualTo(gtCalculationInProgress));
				Assert.That(vm.StatusBrush.Color, Is.EqualTo(gtStatusBrush.Color));
				Assert.That(vm.StatusText, Is.EqualTo(gtStatusText));
				Assert.That(vm.CalculationPending, Is.EqualTo(gtCalculationPending));
			});
		}

		[Test]
		public void UpdateLastAlgorithm_WhenGivenValues_UpdatesField()
		{
			var gtAlgorithm = "dm1";
			var gtWasRangeMeterUsed = "+";
			var gtMessage = $"LastAlgorithm={gtAlgorithm}, RM={gtWasRangeMeterUsed}";

			var vm = new DashboardControlVm();
			vm.UpdateLastAlgorithm(gtAlgorithm, gtWasRangeMeterUsed);

			Assert.That(gtMessage, Is.EqualTo(vm.LastAlgorithmUsed));
		}

		[Test]
		public void UpdateSettings_WhenGivenValidSettings_UpdatesFields()
		{
			string weightLabelTextBefore;
			var weightLabelTextAfter = "кг";

			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);
			weightLabelTextBefore = vm.WeightLabelText;

			var algSettings = new AlgorithmSettings(null, 1, false, 3000, true, Primitives.WeightUnits.Kg,
				false, 0.0, 0);
			vm.UpdateSettings(algSettings);

			Assert.That(vm.WeightLabelText, Is.EqualTo(weightLabelTextAfter));
			Assert.That(vm.WeightLabelText, Is.Not.EqualTo(weightLabelTextBefore));
		}

		[Test]
		public void Dispose_WhenGivenValidSettings_DoesntThrow()
		{
			var vm = new DashboardControlVm();

			vm.Dispose();
		}

		[Test]
		public void ObjectCode_WhenAssignedNewValue_SetsFields()
		{
			var gtObjectLength = 0;
			var gtObjectWidth = 0;
			var gtObjectHeight = 0;
			var gtObjectVolume = 0;
			var gtUnitCount = 0;
			var gtComment = "";
			var settings = ApplicationSettings.GetDefaultDebugSettings();
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);

			vm.ObjectLength = 3;
			vm.ObjectWidth = 4;
			vm.ObjectHeight = 5;
			vm.ObjectVolume = 60;
			vm.UnitCount = 45;
			vm.Comment = "comment";
			vm.ObjectCode = "newCode";

			Assert.Multiple(() =>
			{
				Assert.That(vm.ObjectLength, Is.EqualTo(gtObjectLength));
				Assert.That(vm.ObjectWidth, Is.EqualTo(gtObjectWidth));
				Assert.That(vm.ObjectHeight, Is.EqualTo(gtObjectHeight));
				Assert.That(vm.ObjectVolume, Is.EqualTo(gtObjectVolume));
				Assert.That(vm.UnitCount, Is.EqualTo(gtUnitCount));
				Assert.That(vm.Comment, Is.EqualTo(gtComment));
			});
		}

		[Test]
		public void CalculationRequested_WhenTriggeredCalculation_RaisesEvent()
		{
			var vm = new DashboardControlVm();

			var isCalled = false;
			vm.CalculationRequested += (o) => isCalled = true;
			vm.RunVolumeCalculationCommand.Execute(null);

			Assert.That(isCalled, Is.True);
		}

		[Test]
		public void WeightResetRequested_WhenTriggeredWeightReset_RaisesEvent()
		{
			var vm = new DashboardControlVm();

			var isCalled = false;
			vm.WeightResetRequested += () => isCalled = true;
			vm.ResetWeightCommand.Execute(null);

			Assert.That(isCalled, Is.True);
		}

		[Test]
		public void ResultFileOpeningRequested_WhenTriggeredFileOpening_RaisesEvent()
		{
			var vm = new DashboardControlVm();

			var isCalled = false;
			vm.ResultFileOpeningRequested += () => isCalled = true;
			vm.OpenResultsFileCommand.Execute(null);

			Assert.That(isCalled, Is.True);
		}

		[Test]
		public void CalculationCancellationRequested_WhenTriggeredCalculationCancellation_RaisesEvent()
		{
			var vm = new DashboardControlVm();

			var isCalled = false;
			vm.CalculationCancellationRequested += () => isCalled = true;
			vm.CancelPendingCalculationCommand.Execute(null);

			Assert.That(isCalled, Is.True);
		}

		[TestCase(nameof(DashboardControlVm.CodeBoxFocused), false)]
		[TestCase(nameof(DashboardControlVm.UnitCountBoxFocused), false)]
		[TestCase(nameof(DashboardControlVm.CommentBoxFocused), false)]
		[TestCase(nameof(DashboardControlVm.CodeBoxFocused), true)]
		[TestCase(nameof(DashboardControlVm.UnitCountBoxFocused), true)]
		[TestCase(nameof(DashboardControlVm.CommentBoxFocused), true)]
		public void LockingStatusChanged_WhenTriggeredStatusChange_RaisesEvent(string propertyName, bool value)
		{
			var vm = new DashboardControlVm();

			var isCalled = false;
			vm.LockingStatusChanged += (o) => isCalled = true;
			vm.GetType().GetProperty(propertyName).SetValue(vm, value);

			Assert.That(isCalled, Is.True);
		}

		[TestCase(false, false, false, true)]
		[TestCase(true, false, false, false)]
		[TestCase(false, true, false, false)]
		[TestCase(false, false, true, false)]
		[TestCase(true, true, false, false)]
		[TestCase(false, true, true, false)]
		[TestCase(true, false, true, false)]
		[TestCase(true, true, true, false)]
		public void CanAcceptBarcodes_WhenGivenDifferentStatesOfBoxes_ReturnsApproppriateValue(
			bool codeBoxFocused, bool unitCountBoxFocused, bool commentBoxFocused, bool returnValue)
		{
			var vm = new DashboardControlVm();

			vm.CodeBoxFocused = codeBoxFocused;
			vm.UnitCountBoxFocused = unitCountBoxFocused;
			vm.CommentBoxFocused = commentBoxFocused;

			Assert.That(vm.CanAcceptBarcodes, Is.EqualTo(returnValue));
		}

		[TestCase("code", true, true)]
		[TestCase("", true, false)]
		[TestCase(null, true, false)]
		[TestCase("", false, true)]
		[TestCase("code", false, true)]
		public void CodeReady_WhenNoCodeWithDefaultSettings_ReturnsFalse(
			string code, bool requireBarcode, bool resultValue)
		{
			var settings = ApplicationSettings.GetDefaultDebugSettings();
			settings.AlgorithmSettings.RequireBarcode = requireBarcode;
			var vm = new DashboardControlVm();
			vm.UpdateSettings(settings.AlgorithmSettings);

			vm.ObjectCode = code;

			Assert.That(vm.CodeReady, Is.EqualTo(resultValue));
		}
	}
}
