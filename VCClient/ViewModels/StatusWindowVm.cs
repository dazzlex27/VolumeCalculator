﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using CommonUtils;

namespace VCClient.ViewModels
{
	internal class StatusWindowVm : BaseViewModel
	{
		private string _appVersion;
		private string _hostName;
		private string _currentIp;
		private bool _licenseIsOk;
		private bool _webServerIsRunning;

		public string AppVersion
		{
			get => _appVersion;
			set => SetField(ref _appVersion, value, nameof(AppVersion));
		}

		public string HostName
		{
			get => _hostName;
			set => SetField(ref _hostName, value, nameof(HostName));
		}

		public string CurrentIp
		{
			get => _currentIp;
			set => SetField(ref _currentIp, value, nameof(CurrentIp));
		}

		public bool LicenseIsOk
		{
			get => _licenseIsOk;
			set => SetField(ref _licenseIsOk, value, nameof(LicenseIsOk));
		}

		public bool WebServerIsRunning
		{
			get => _webServerIsRunning;
			set => SetField(ref _webServerIsRunning, value, nameof(WebServerIsRunning));
		}

		public StatusWindowVm(ILogger logger, HttpClient httpClient)
		{
			try
			{
				HostName = IoUtils.GetHostName();

				// TODO: verify that this is ok
				var ipAddresses = Task.Run(IoUtils.GetLocalIpAddressesAsync);

				AppVersion = GlobalConstants.AppVersion;

				CurrentIp = string.Join(Environment.NewLine, ipAddresses);
				LicenseIsOk = true;
				
				WebServerIsRunning = false;

				Task.Run(async () =>
				{
					try
					{
						var response = await httpClient.GetAsync(@"http://127.0.0.1");
						if (response.StatusCode == HttpStatusCode.OK)
						{
							Dispatcher.Invoke(() =>
							{
								WebServerIsRunning = true;
							});
						}
					}
					catch (Exception ex)
					{
						logger.LogException("Web server request for info failed", ex);
					}
				});

				logger.LogInfo($"Status has been requested: hostname={HostName}, currentIps={string.Join(";", ipAddresses)}, license={LicenseIsOk}, webserverrunning={WebServerIsRunning}");
			}
			catch (Exception ex)
			{
				logger.LogException("Failed to fetch system info", ex);
			}
		}
	}
}
