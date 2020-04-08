using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using Salesforce.Common;
using Salesforce.Common.Models.Json;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public class SalesforceClient : IDisposable
	{
		private readonly string? _url;
		private readonly string _clientId;
		private readonly string _clientSecret;
		private readonly string _userName;
		private readonly string _password;

		private AuthenticationClient? _authenticationClient;

		public SalesforceClient(
			string? url,
			string clientId,
			string clientSecret,
			string userName,
			string password)
		{
			_url = url;
			_clientId = clientId;
			_clientSecret = clientSecret;
			_userName = userName;
			_password = password;
		}

		private ForceClient? _client;

		public ForceClient Client
		{
			get
			{
				if (_client != null)
				{
					return _client;
				}
				_authenticationClient = new AuthenticationClient();
				if (_url is null)
				{
					_authenticationClient
					 .UsernamePasswordAsync(_clientId, _clientSecret, _userName, _password)
					 .GetAwaiter().GetResult();
				}
				else
				{
					_authenticationClient
					 .UsernamePasswordAsync(_clientId, _clientSecret, _userName, _password, _url)
					 .GetAwaiter().GetResult();
				}
				return _client = new ForceClient(
					_authenticationClient.InstanceUrl,
					_authenticationClient.AccessToken,
					_authenticationClient.ApiVersion);
			}
		}

		/// <summary>
		/// Handles paging using createdate
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public async Task<List<JObject>> GetAllJObjectsAsync(string query)
		{
			var list = new List<JObject>();
			QueryResult<JObject> queryResult;
			DateTimeOffset maxCreatedDateTimeOffset = default;

			// For paging
			if (!query.Contains("CreatedDate,"))
			{
				query = query.Replace("SELECT ", "SELECT CreatedDate, ");
			}

			var skipCount = 0;
			do
			{
				var pageQuery = query;
				if (maxCreatedDateTimeOffset != default)
				{
					// TODO - use >= and dedupe
					if (pageQuery.Contains(" WHERE "))
					{
						pageQuery = pageQuery.Replace(" WHERE ", $" WHERE CreatedDate >= {maxCreatedDateTimeOffset.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ} AND ");
					}
					else
					{
						pageQuery = $"{query} WHERE CreatedDate >= {maxCreatedDateTimeOffset.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";
					}
				}

				if (pageQuery.Contains(" ORDER BY"))
				{
					throw new ConfigurationException("Query should not contain ' ORDER BY'");
				}
				pageQuery += " ORDER BY CreatedDate ASC";

				queryResult = await Client.QueryAsync<JObject>(pageQuery).ConfigureAwait(false);

				list.AddRange(queryResult.Records.Skip(skipCount));

				var maxCreatedDateTime = queryResult.Records.Last().Value<DateTime>("CreatedDate");
				skipCount = queryResult.Records.Count(r => r.Value<DateTime>("CreatedDate") == maxCreatedDateTime);
				maxCreatedDateTimeOffset = new DateTimeOffset(maxCreatedDateTime);
			} while (!queryResult.Done);

			return list;
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_authenticationClient?.Dispose();
					_client?.Dispose();
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
#pragma warning disable IDE0022 // Use expression body for methods
			Dispose(true);
#pragma warning restore IDE0022 // Use expression body for methods
		}
		#endregion
	}
}