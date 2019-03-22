using System.Collections.ObjectModel;
using System.Linq;
using Primitives.Settings.Integration;

namespace VCConfigurator
{
	internal class IntegrationSettingsVm : BaseViewModel
	{
		private bool _enableHttpApi;
		private int _httpApiPort;

		private bool _enableHttpRequests;
		private ObservableCollection<string> _httpDestinationIps;
		private int _httpRequestPort;
		private string _httpRequestUrl;

		private bool _enableSqlRequests;
		private string _sqlRequestHostName;
		private string _sqlRequestUsername;
		private string _sqlRequestPassword;
		private string _sqlRequestDbName;
		private string _sqlRequestTableName;

		private bool _enableFtpRequests;
		private string _ftpRequestHostName;
		private int _ftpRequestPort;
		private string _ftpRequestLogin;
		private string _ftpRequestPassword;
		private bool _ftpRequestIsSecure;
		private string _ftpRequestHostCertificateFingerPrint;
		private string _ftpRequestBaseFolderName;

		public bool EnableHttpApi
		{
			get => _enableHttpApi;
			set
			{
				if (_enableHttpApi == value)
					return;

				_enableHttpApi = value;
				OnPropertyChanged();
			}
		}

		public int HttpApiPort
		{
			get => _httpApiPort;
			set
			{
				if (_httpApiPort == value)
					return;

				_httpApiPort = value;
				OnPropertyChanged();
			}
		}

		public bool EnableHttpRequests
		{
			get => _enableHttpRequests;
			set
			{
				if (_enableHttpRequests == value)
					return;

				_enableHttpRequests = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> HttpDestinationIps
		{
			get => _httpDestinationIps;
			set
			{
				if (_httpDestinationIps == value)
					return;

				_httpDestinationIps = value;
				OnPropertyChanged();
			}
		}

		public int HttpRequestPort
		{
			get => _httpRequestPort;
			set
			{
				if (_httpRequestPort == value)
					return;

				_httpRequestPort = value;
				OnPropertyChanged();
			}
		}

		public string HttpRequestUrl
		{
			get => _httpRequestUrl;
			set
			{
				if (_httpRequestUrl == value)
					return;

				_httpRequestUrl = value;
				OnPropertyChanged();
			}
		}

		public bool EnableSqlRequests
		{
			get => _enableSqlRequests;
			set
			{
				if (_enableSqlRequests == value)
					return;

				_enableSqlRequests = value;
				OnPropertyChanged();
			}
		}

		public string SqlRequestHostName
		{
			get => _sqlRequestHostName;
			set
			{
				if (_sqlRequestHostName == value)
					return;

				_sqlRequestHostName = value;
				OnPropertyChanged();
			}
		}

		public string SqlRequestUsername
		{
			get => _sqlRequestUsername;
			set
			{
				if (_sqlRequestUsername == value)
					return;

				_sqlRequestUsername = value;
				OnPropertyChanged();
			}
		}

		public string SqlRequestPassword
		{
			get => _sqlRequestPassword;
			set
			{
				if (_sqlRequestPassword == value)
					return;

				_sqlRequestPassword = value;
				OnPropertyChanged();
			}
		}

		public string SqlRequestDbName
		{
			get => _sqlRequestDbName;
			set
			{
				if (_sqlRequestDbName == value)
					return;

				_sqlRequestDbName = value;
				OnPropertyChanged();
			}
		}

		public string SqlRequestTableName
		{
			get => _sqlRequestTableName;
			set
			{
				if (_sqlRequestTableName == value)
					return;

				_sqlRequestTableName = value;
				OnPropertyChanged();
			}
		}

		public bool EnableFtpRequests
		{
			get => _enableFtpRequests;
			set
			{
				if (_enableFtpRequests == value)
					return;

				_enableFtpRequests = value;
				OnPropertyChanged();
			}
		}

		public string FtpRequestHostName
		{
			get => _ftpRequestHostName;
			set
			{
				if (_ftpRequestHostName == value)
					return;

				_ftpRequestHostName = value;
				OnPropertyChanged();
			}
		}

		public int FtpRequestPort
		{
			get => _ftpRequestPort;
			set
			{
				if (_ftpRequestPort == value)
					return;

				_ftpRequestPort = value;
				OnPropertyChanged();
			}
		}

		public string FtpRequestLogin
		{
			get => _ftpRequestLogin;
			set
			{
				if (_ftpRequestLogin == value)
					return;

				_ftpRequestLogin = value;
				OnPropertyChanged();
			}
		}

		public string FtpRequestPassword
		{
			get => _ftpRequestPassword;
			set
			{
				if (_ftpRequestPassword == value)
					return;

				_ftpRequestPassword = value;
				OnPropertyChanged();
			}
		}

		public bool FtpRequestIsSecure
		{
			get => _ftpRequestIsSecure;
			set
			{
				if (_ftpRequestIsSecure == value)
					return;

				_ftpRequestIsSecure = value;
				OnPropertyChanged();
			}
		}

		public string FtpRequestHostCertificateFingerPrint
		{
			get => _ftpRequestHostCertificateFingerPrint;
			set
			{
				if (_ftpRequestHostCertificateFingerPrint == value)
					return;

				_ftpRequestHostCertificateFingerPrint = value;
				OnPropertyChanged();
			}
		}

		public string FtpRequestBaseFolderName
		{
			get => _ftpRequestBaseFolderName;
			set
			{
				if (_ftpRequestBaseFolderName == value)
					return;

				_ftpRequestBaseFolderName = value;
				OnPropertyChanged();
			}
		}

		public void FillValuesFromSettings(IntegrationSettings settings)
		{
			var httpApiSettings = settings.HttpApiSettings;
			EnableHttpApi = httpApiSettings.EnableRequests;
			HttpApiPort = httpApiSettings.Port;

			var httpRequestSettings = settings.HttpRequestSettings;
			EnableHttpRequests = httpRequestSettings.EnableRequests;
			HttpDestinationIps = new ObservableCollection<string>(httpRequestSettings.DestinationIps);
			HttpRequestPort = httpRequestSettings.Port;
			HttpRequestUrl = httpRequestSettings.Url;

			var sqlRequestSettings = settings.SqlRequestSettings;
			EnableSqlRequests = sqlRequestSettings.EnableRequests;
			SqlRequestHostName = sqlRequestSettings.HostName;
			SqlRequestUsername = sqlRequestSettings.Username;
			SqlRequestPassword = sqlRequestSettings.Password;
			SqlRequestDbName = sqlRequestSettings.DbName;
			SqlRequestTableName = sqlRequestSettings.TableName;

			var ftpRequestSettings = settings.FtpRequestSettings;
			EnableFtpRequests = ftpRequestSettings.EnableRequests;
			FtpRequestHostName = ftpRequestSettings.Host;
			FtpRequestLogin = ftpRequestSettings.Login;
			FtpRequestPassword = ftpRequestSettings.Password;
			FtpRequestIsSecure = ftpRequestSettings.IsSecure;
			FtpRequestHostCertificateFingerPrint = ftpRequestSettings.HostCertificateFingerprint;
			FtpRequestBaseFolderName = ftpRequestSettings.BaseDirectory;
		}

		public void FillSettingsFromValues(IntegrationSettings settings)
		{
			var httpApiSettings = settings.HttpApiSettings;
			httpApiSettings.EnableRequests = EnableHttpApi;
			httpApiSettings.Port = HttpApiPort;

			var httpRequestSettings = settings.HttpRequestSettings;
			httpRequestSettings.EnableRequests = EnableHttpRequests;
			httpRequestSettings.DestinationIps = HttpDestinationIps.ToArray();
			httpRequestSettings.Port = HttpRequestPort;
			httpRequestSettings.Url = HttpRequestUrl;

			var sqlRequestSettings = settings.SqlRequestSettings;
			sqlRequestSettings.EnableRequests = EnableSqlRequests;
			sqlRequestSettings.HostName = SqlRequestHostName;
			sqlRequestSettings.Username = SqlRequestUsername;
			sqlRequestSettings.Password = SqlRequestPassword;
			sqlRequestSettings.DbName = SqlRequestDbName;
			sqlRequestSettings.TableName = SqlRequestTableName;

			var ftpRequestSettings = settings.FtpRequestSettings;
			ftpRequestSettings.EnableRequests = EnableFtpRequests;
			ftpRequestSettings.Host = FtpRequestHostName;
			ftpRequestSettings.Login = FtpRequestLogin;
			ftpRequestSettings.Password = FtpRequestPassword;
			ftpRequestSettings.IsSecure = FtpRequestIsSecure;
			ftpRequestSettings.HostCertificateFingerprint = FtpRequestHostCertificateFingerPrint;
			ftpRequestSettings.BaseDirectory = FtpRequestBaseFolderName;
		}
	}
}