﻿using System;
using System.Linq;
using System.Net.Http;
using DeviceIntegration.Cameras;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using FrameProviders;
using Primitives.Logging;
using Primitives.Settings;

namespace VCServer
{
    public class IoDeviceManager : IDisposable
    {
        public event Action<ScaleMeasurementData> WeightMeasurementReady;
        public event Action<string> BarcodeReady;

        private readonly ILogger _logger;
        private readonly DeviceSet _deviceSet;

        private bool _subtractPalletWeight;
        private double _palletWeightGr;

        public IFrameProvider FrameProvider => _deviceSet.FrameProvider;

        public IIoCircuit IoCircuit => _deviceSet.IoCircuit;
        
        public IRangeMeter RangeMeter => _deviceSet.RangeMeter;

        public IIpCamera IpCamera => _deviceSet.IpCamera;
        
        public IoDeviceManager(ILogger logger, HttpClient httpClient, IoSettings settings)
        {
            _logger = logger;
            _logger.LogInfo("Createing device manager...");
            _deviceSet = _deviceSet = DeviceSetFactory.CreateDeviceSet(logger, httpClient, settings);
            
            if (_deviceSet.Scanners != null)
            {
                foreach (var scanner in _deviceSet.Scanners.Where(s => s != null))
                    scanner.CharSequenceFormed += OnBarcodeReady;
            }

            if (_deviceSet.Scales != null)
                _deviceSet.Scales.MeasurementReady += OnScalesWeightReady;

            _logger.LogInfo("Device manager created");
        }

        public void Dispose()
        {
            _logger.LogInfo("Disposing device manager...");
            
            var scanners = _deviceSet.Scanners;
            if (scanners != null)
            {
                foreach (var scanner in scanners.Where(s => s != null))
                    scanner.CharSequenceFormed -= OnBarcodeReady;
            }

            var scales = _deviceSet.Scales;
            if (scales != null)
                scales.MeasurementReady -= OnScalesWeightReady;

            _logger.LogInfo("Device manager disposed");
        }

        public void TogglePause(bool pause)
        {
            _deviceSet.TogglePause(pause);
        }
        
        public void ResetWeight()
        {
            _deviceSet.Scales?.ResetWeight();
        }

        public void UpdateSettings(ApplicationSettings settings)
        {
            _subtractPalletWeight = settings.AlgorithmSettings.EnablePalletSubtraction;
            _palletWeightGr = settings.AlgorithmSettings.PalletWeightGr;
        }

        private void OnScalesWeightReady(ScaleMeasurementData data)
        {
            OnWeightMeasurementReady(data);
        }

        private void OnWeightMeasurementReady(ScaleMeasurementData data)
        {
            if (!_subtractPalletWeight)
                WeightMeasurementReady?.Invoke(data);
            else
            {
                var newData = new ScaleMeasurementData(data.Status, data.WeightGr - _palletWeightGr);
                WeightMeasurementReady?.Invoke(newData);
            }
        }

        private void OnBarcodeReady(string barcode)
        {
            BarcodeReady?.Invoke(barcode);
        }
    }
}