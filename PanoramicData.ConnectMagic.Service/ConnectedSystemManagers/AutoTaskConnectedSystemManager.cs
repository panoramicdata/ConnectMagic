using AutoTask.Api;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System.Threading;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class AutoTaskConnectedSystemManager : IConnectedSystemManager
	{
		private readonly ConnectedSystem connectedSystem;
		private readonly Client autoTaskClient;

		public AutoTaskConnectedSystemManager(ConnectedSystem connectedSystem)
		{
			this.connectedSystem = connectedSystem;
			autoTaskClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
		}

		public async System.Threading.Tasks.Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in connectedSystem.Datasets)
			{
				var query = new SubstitutionString(new JObject(dataSet.QueryConfig)["Query"]?.ToString() ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'"));

				var items = await autoTaskClient
					.ExecuteQueryAsync(query.ToString())
					.ConfigureAwait(false);
			}
		}
	}
}