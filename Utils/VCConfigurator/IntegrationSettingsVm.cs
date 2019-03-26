using System.Collections.ObjectModel;
using System.Linq;
using Primitives.Settings.Integration;

namespace VCConfigurator
{
	internal class IntegrationSettingsVm : BaseViewModel
	{
		private bool _enableHttpApi;
		private int _httpApiPort;
		private string _httpApiLogin;
		private string _httpApiPassword;

		private bool _enableWebClientHandler;
		private string _webClientHandlerAddress;
		private int _webClientHandlerPort;

		private bool _enableHttpRequests;
		private string _httpRequestAddress;
		private int _httpRequestPort;
		private string _httpRequestUrl;
		private string _httpRequestLogin;
		private string _httpRequestPassword;

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
		private bool _ftpRequestIncludeObjectPhotos;

		public bool EnableHttpApi
		{
			get => _enableHttpApi;
			set => SetField(ref _enableHttpApi, value, nameof(EnableHttpApi));
		}

		public int HttpApiPort
		{
			get => _httpApiPort;
			set => SetField(ref _httpApiPort, value, nameof(HttpApiPort));
		}

		public string HttpApiLogin
		{
			get => _httpApiLogin;
			set => SetField(ref _httpApiLogin, value, nameof(HttpApiLogin));
		}

		public string HttpApiPassword
		{
			get => _httpApiPassword;
			set => SetField(ref _httpApiPassword, value, nameof(HttpApiPassword));
		}

		public bool EnableWebClientHandler
		{
			get => _enableWebClientHandler;
			set => SetField(ref _enableWebClientHandler, value, nameof(EnableWebClientHandler));
		}

		public string WebClientHandlerAddress
		{
			get => _webClientHandlerAddress;
			set => SetField(ref _webClientHandlerAddress, value, nameof(WebClientHandlerAddress));
		}

		public int WebClientHandlerPort
		{
			get => _webClientHandlerPort;
			set => SetField(ref _webClientHandlerPort, value, nameof(WebClientHandlerPort));
		}

		public bool EnableHttpRequests
		{
			get => _enableHttpRequests;
			set => SetField(ref _enableHttpRequests, value, nameof(EnableHttpRequests));
		}

		public string HttpRequestAddress
		{
			get => _httpRequestAddress;
			set => SetField(ref _httpRequestAddress, value, nameof(HttpRequestAddress));
		}

		public int HttpRequestPort
		{
			get => _httpRequestPort;
			set => SetField(ref _httpRequestPort, value, nameof(HttpRequestPort));
		}

		public string HttpRequestUrl
		{
			get => _httpRequestUrl;
			set => SetField(ref _httpRequestUrl, value, nameof(HttpRequestUrl));
		}

		public string HttpRequestLogin
		{
			get => _httpRequestLogin;
			set => SetField(ref _httpRequestLogin, value, nameof(HttpRequestLogin));
		}

		public string HttpRequestPassword
		{
			get => _httpRequestPassword;
			set => SetField(ref _httpRequestPassword, value, nameof(_httpRequestPassword));
		}

		public bool EnableSqlRequests
		{
			get => _enableSqlRequests;
			set => SetField(ref _enableSqlRequests, value, nameof(EnableSqlRequests));
		}

		public string SqlRequestHostName
		{
			get => _sqlRequestHostName;
			set => SetField(ref _sqlRequestHostName, value, nameof(SqlRequestHostName));
		}

		public string SqlRequestUsername
		{
			get => _sqlRequestUsername;
			set => SetField(ref _sqlRequestUsername, value, nameof(SqlRequestUsername));
		}

		public string SqlRequestPassword
		{
			get => _sqlRequestPassword;
			set => SetField(ref _sqlRequestPassword, value, nameof(SqlRequestPassword));
		}

		public string SqlRequestDbName
		{
			get => _sqlRequestDbName;
			set => SetField(ref _sqlRequestDbName, value, nameof(SqlRequestDbName));
		}

		public string SqlRequestTableName
		{
			get => _sqlRequestTableName;
			set => SetField(ref _sqlRequestTableName, value, nameof(SqlRequestTableName));
		}

		public bool EnableFtpRequests
		{
			get => _enableFtpRequests;
			set => SetField(ref _enableFtpRequests, value, nameof(EnableFtpRequests));
		}

		public string FtpRequestHostName
		{
			get => _ftpRequestHostName;
			set => SetField(ref _ftpRequestHostName, value, nameof(FtpRequestHostName));
		}

		public int FtpRequestPort
		{
			get => _ftpRequestPort;
			set => SetField(ref _ftpRequestPort, value, nameof(FtpRequestPort));
		}

		public string FtpRequestLogin
		{
			get => _ftpRequestLogin;
			set => SetField(ref _ftpRequestLogin, value, nameof(FtpRequestLogin));
		}

		public string FtpRequestPassword
		{
			get => _ftpRequestPassword;
			set => SetField(ref _ftpRequestPassword, value, nameof(FtpRequestPassword));
		}

		public bool FtpRequestIsSecure
		{
			get => _ftpRequestIsSecure;
			set => SetField(ref _ftpRequestIsSecure, value, nameof(FtpRequestIsSecure));
		}

		public string FtpRequestHostCertificateFingerPrint
		{
			get => _ftpRequestHostCertificateFingerPrint;
			set => SetField(ref _ftpRequestHostCertificateFingerPrint, value, nameof(FtpRequestHostCertificateFingerPrint));
		}

		public string FtpRequestBaseFolderName
		{
			get => _ftpRequestBaseFolderName;
			set => SetField(ref _ftpRequestBaseFolderName, value, nameof(FtpRequestBaseFolderName));
		}

		public bool FtpRequestIncludeObjectPhotos
		{
			get => _ftpRequestIncludeObjectPhotos;
			set => SetField(ref _ftpRequestIncludeObjectPhotos, value, nameof(FtpRequestIncludeObjectPhotos));
		}

		public void FillValuesFromSettings(IntegrationSettings settings)
		{
			var httpApiSettings = settings.HttpApiSettings;
			EnableHttpApi = httpApiSettings.EnableRequests;
			HttpApiPort = httpApiSettings.Port;
			HttpApiLogin = httpApiSettings.Login;
			HttpApiPassword = httpApiSettings.Password;

			var webClientHandlerSettings = settings.WebClientHandlerSettings;
			EnableWebClientHandler = webClientHandlerSettings.EnableRequests;
			WebClientHandlerAddress = webClientHandlerSettings.Address;
			WebClientHandlerPort = webClientHandlerSettings.Port;

			var httpRequestSettings = settings.HttpRequestSettings;
			EnableHttpRequests = httpRequestSettings.EnableRequests;
			HttpRequestAddress = httpRequestSettings.DestinationIps.Length > 0
				? httpRequestSettings.DestinationIps[0]
				: "127.0.0.1";
			HttpRequestPort = httpRequestSettings.Port;
			HttpRequestUrl = httpRequestSettings.Url;
			HttpRequestLogin = httpRequestSettings.Login;
			HttpRequestPassword = httpRequestSettings.Password;

			var sqlRequestSettings = settings.SqlRequestSettings;
			EnableSqlRequests = sqlRequestSettings.EnableRequests;
			SqlRequestHostName = sqlRequestSettings.HostName;
			SqlRequestUsername = sqlRequestSettings.Username;
			SqlRequestPassword = sqlRequestSettings.Password;
			SqlRequestDbName = sqlRequestSettings.DbName;
			SqlRequestTableName = sqlRequestSettings.TableName;

			var ftpRequestSettings = settings.FtpRequestSettings;
			EnableFtpRequests = ftpRequestSettings.EnableRequests;
			FtpRequestPort = ftpRequestSettings.Port;
			FtpRequestHostName = ftpRequestSettings.Host;
			FtpRequestLogin = ftpRequestSettings.Login;
			FtpRequestPassword = ftpRequestSettings.Password;
			FtpRequestIsSecure = ftpRequestSettings.IsSecure;
			FtpRequestHostCertificateFingerPrint = ftpRequestSettings.HostCertificateFingerprint;
			FtpRequestBaseFolderName = ftpRequestSettings.BaseDirectory;
			FtpRequestIncludeObjectPhotos = ftpRequestSettings.IncludeObjectPhotos;
		}

		public void FillSettingsFromValues(IntegrationSettings settings)
		{
			var httpApiSettings = settings.HttpApiSettings;
			httpApiSettings.EnableRequests = EnableHttpApi;
			httpApiSettings.Port = HttpApiPort;
			httpApiSettings.Login = HttpRequestLogin;
			httpApiSettings.Password = HttpRequestPassword;

			var webClientHandlerSettings = settings.WebClientHandlerSettings;
			webClientHandlerSettings.EnableRequests = EnableWebClientHandler;
			webClientHandlerSettings.Address = WebClientHandlerAddress;
			webClientHandlerSettings.Port = WebClientHandlerPort;

			var httpRequestSettings = settings.HttpRequestSettings;
			httpRequestSettings.EnableRequests = EnableHttpRequests;
			httpRequestSettings.DestinationIps = new[] {HttpRequestAddress};
			httpRequestSettings.Port = HttpRequestPort;
			httpRequestSettings.Url = HttpRequestUrl;
			httpRequestSettings.Login = HttpRequestLogin;
			httpRequestSettings.Password = HttpRequestPassword;

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
			ftpRequestSettings.IncludeObjectPhotos = FtpRequestIncludeObjectPhotos;
		}
	}
}