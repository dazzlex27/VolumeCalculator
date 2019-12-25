using System;
using System.Net.Http.Headers;
using System.Text;

namespace ProcessingUtils
{
	public static class NetworkUtils
	{
		public static AuthenticationHeaderValue GetBasicAuthenticationHeaderData(string login, string password)
		{
			var asciiCredentialsData = Encoding.ASCII.GetBytes($"{login}:{password}");
			var base64CredentialsData = Convert.ToBase64String(asciiCredentialsData);

			return new AuthenticationHeaderValue("Basic", base64CredentialsData);
		}
	}
}