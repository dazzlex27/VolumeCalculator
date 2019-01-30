using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DeviceIntegration;
using DeviceIntegration.Scales;
using DeviceIntegrations.IoCircuits;
using DeviceIntegrations.Scanners;
using FrameProviders;
using Microsoft.Win32;
using Primitives.Logging;
using Primitives.Settings;

namespace VolumeCalculatorGUI.Utils
{
	internal class DeviceSetFactory
	{
		public DeviceSet CreateDeviceSet(ILogger logger, IoSettings settings)
		{
			IScales scales = null;
			var barcodeScanners = new List<IBarcodeScanner>();
			IIoCircuit ioCircuit = null;

			var frameProviderName = settings.ActiveCameraName;
			logger.LogInfo($"Creating frame provider \"{frameProviderName}\"...");
			var frameProvider = FrameProvidersCommon.CreateRequestedFrameProvider(frameProviderName, logger);
			frameProvider.ColorCameraFps = 5;
			frameProvider.DepthCameraFps = 5;
			frameProvider.Start();

			foreach (var entry in settings.ActiveScanners)
			{
				logger.LogInfo($"Creating scanner \"{entry.Name}\"...");

				if (entry.Name == "keyboard")
				{
					var keyboardListener = new GenericKeyboardBarcodeScanner(logger);
					barcodeScanners.Add(keyboardListener);
					var keyEventHandler = new KeyEventHandler(keyboardListener.AddKey);
					EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, keyEventHandler, true);
				}
				else
				{
					if (entry.Port == string.Empty)
						continue;

					var scanner = DeviceIntegrationCommon.CreateRequestedScanner(entry.Name, logger, entry.Port);
					barcodeScanners.Add(scanner);
				}
			}

			var scalesName = settings.ActiveScalesName;
			var scalesPort = settings.ScalesPort;
			if (scalesPort != string.Empty)
			{
				logger.LogInfo($"Creating scales \"{scalesName}\"...");
				scales = DeviceIntegrationCommon.CreateRequestedScales(scalesName, logger, scalesPort);
			}

			var ioCircuitName = settings.ActiveIoCircuitName;
			var ioCircuitPort = settings.IoCircuitPort;
			if (ioCircuitPort != string.Empty)
			{
				logger.LogInfo($"Creating IO circuit \"{ioCircuitName}\"...");
				ioCircuit = DeviceIntegrationCommon.CreateRequestedIoCircuit(ioCircuitName, logger, ioCircuitPort);
			}

			return new DeviceSet(frameProvider, scales, barcodeScanners, ioCircuit);
		}

		public static byte[] GetAddr()
		{
			var serial = "";
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion"))
				{
					var obj = key?.GetValue("ProductId");
					if (obj != null)
						serial = obj.ToString();
				}
			}
			catch (Exception ex)
			{
				Directory.CreateDirectory("c:/temp");
				using (var f = File.AppendText("c:/temp"))
				{
					f.WriteLine($"s1 f{ex}");
				}

				return new byte[0];
			}

			return string.IsNullOrEmpty(serial) ? new byte[] {0} : Encoding.ASCII.GetBytes(serial);
		}
	}
}